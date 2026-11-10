using API.Data;
using API.DTOs;
using API.Entities;
using API.Entities.Enums;
using API.Extensions;
using API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers;

[Authorize(Roles = "Admin")]
public class ReportsController(AppDbContext context) : BaseApiController
{
    private sealed record SessionSpan(EmployeeSession S, DateTime End, double BreakHours)
    {
        public double Hours => Math.Max(0, (End - S.LoginAt).TotalHours - BreakHours);
    }

    private static double BreakHoursOf(EmployeeSession s, DateTime end, List<EmployeeSession> breaks)
    {
        var minutes = breaks
            .Where(b => b.UserId == s.UserId && b.LoginAt == s.LoginAt)
            .Sum(b =>
            {
                var start = b.BreakStartedAt!.Value;
                var stop = b.BreakEndedAt ?? end;
                if (stop > end) stop = end;
                return stop > start ? (stop - start).TotalMinutes : 0;
            });
        return minutes / 60;
    }

    [HttpGet("heatmap")]
    public async Task<ActionResult<List<HeatmapCellDto>>> GetHeatmap([FromQuery] DateTime from, [FromQuery] DateTime to)
    {
        if (to.Date < from.Date) return BadRequest("End date is before start date");
        var (f, t) = Range(from, to);
        var created = await context.Tickets
            .AsNoTracking()
            .Where(x => x.CreatedAt >= f && x.CreatedAt < t)
            .Select(x => x.CreatedAt)
            .ToListAsync();

        return created
        .Select(LocalTime.ToLocal)
        .GroupBy(d => new { Weekday = ((int)d.DayOfWeek + 6) % 7, d.Hour })
        .Select(g => new HeatmapCellDto { Weekday = g.Key.Weekday, Hour = g.Key.Hour, Tickets = g.Count() })
        .ToList();
    }
    [HttpGet("employees")]
    public async Task<ActionResult<List<EmployeeReportRowDto>>> GetEmployees(
        [FromQuery] DateTime from, [FromQuery] DateTime to)
    {
        if (to.Date < from.Date) return BadRequest("End date is before start date");
        var (f, t) = Range(from, to);
        var counters = await context.Counters.AsNoTracking().ToDictionaryAsync(c => c.Id, c => c.Name);
        var employees = await context.Users
            .Where(u => u.Role == Role.Employee)
            .Select(u => new { u.Id, u.Username, u.CounterId })
            .ToListAsync();
        var spans = await LoadSpansAsync(f, t, null);
        var tickets = await LoadTicketsAsync(f, t);
        var now = DateTime.UtcNow;
        var breakRows = await context.EmployeeSessions.AsNoTracking().Where(b => b.BreakStartedAt != null && b.BreakStartedAt >= f && b.BreakStartedAt < t).ToListAsync();
        var holidayDates = (await context.NonWorkingDays.Where(x => x.Date >= f && x.Date < t).Select(x => x.Date).ToListAsync()).ToHashSet();
        bool IsNonWorking(DateTime d) => holidayDates.Contains(d.Date) || d.DayOfWeek == DayOfWeek.Saturday || d.DayOfWeek == DayOfWeek.Sunday;


        return employees.Select(e =>
        {
            var mine = spans.Where(x => x.S.UserId == e.Id).ToList();
            var inRange = mine.Where(x => x.S.LoginAt >= f).ToList();
            var handled = TicketsOf(mine, tickets, e.Id);
            return new EmployeeReportRowDto
            {
                Id = e.Id,
                Username = e.Username,
                CounterName = e.CounterId != null && counters.TryGetValue(e.CounterId.Value, out var name) ? name : null,
                Sessions = inRange.Count,
                HoursWorked = Math.Round(inRange.Sum(x => x.Hours), 2),
                TicketsServed = handled.Count(x => x.Status == TicketStatus.Finished),
                NoShows = handled.Count(x => x.Status == TicketStatus.NoShow),
                AvgServiceMinutes = AvgMinutes(handled),
                LastLoginAt = mine.Count == 0 ? null : mine.Max(x => x.S.LoginAt),
                NonWorkingHours = Math.Round(inRange.Where(x => IsNonWorking(x.S.LoginAt)).Sum(x => x.Hours), 2),
                BreakHours = Math.Round(inRange.Sum(x => x.BreakHours), 2),
            };
        }).OrderBy(r => r.Username).ToList();
    }

    [HttpGet("employees/{id}/activity")]
    public async Task<ActionResult<EmployeeActivityDto>> GetActivity(
        int id, [FromQuery] DateTime from, [FromQuery] DateTime to)
    {
        if (to.Date < from.Date) return BadRequest("End date is before start date");
        if (!await context.Users.AnyAsync(u => u.Id == id && u.Role == Role.Employee)) return NotFound();

        var (f, t) = Range(from, to);
        var spans = await LoadSpansAsync(f, t, id);
        var tickets = TicketsOf(spans, await LoadTicketsAsync(f, t), id);

        var log = await context.ActivityLogs
            .Where(l => l.ActorId == id && l.CreatedAt >= f && l.CreatedAt < t)
            .OrderByDescending(l => l.CreatedAt)
            .Take(200)
            .Select(l => new ActivityEntryDto { CreatedAt = l.CreatedAt, Action = l.Action, Details = l.Details })
            .ToListAsync();


        var todayLocal = LocalTime.Today();
        var holidays = (await context.NonWorkingDays.AsNoTracking()
                .Where(x => x.Date >= f && x.Date < t)
                .Select(x => x.Date).ToListAsync())
            .Select(d => d.Date).ToHashSet();
        var leaves = await context.Leaves.AsNoTracking()
            .Where(l => l.UserId == id && l.Status == LeaveStatus.Approved && l.EndDate >= f && l.StartDate < t)
            .ToListAsync();
        var leaveDays = leaves
    .SelectMany(l => EachDay(l.StartDate, l.EndDate).Select(d => new { Day = d, l.Type }))
    .Where(x => x.Day >= f.Date && x.Day < t && x.Day <= todayLocal
                && x.Day.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday)
                && !holidays.Contains(x.Day))
    .OrderByDescending(x => x.Day)
    .ToList();
        var leaveDaySet = leaveDays.Select(x => x.Day).ToHashSet();
        var absentDays = leaveDays
            .Select(x => new AbsentDayDto { Date = x.Day, Reason = x.Type.ToString() })
            .ToList();

        return new EmployeeActivityDto
        {
            AbsentDays = absentDays,

            Sessions = spans
                .Where(x => x.S.LoginAt >= f && !leaveDaySet.Contains(LocalTime.ToLocal(x.S.LoginAt).Date))
                .OrderByDescending(x => x.S.LoginAt)
                .Select(x => new SessionReportDto
                {
                    Id = x.S.Id,
                    LoginAt = x.S.LoginAt,
                    LogoutAt = x.S.LogoutAt,
                    IsOpen = x.S.LogoutAt == null,
                    DurationHours = Math.Round(x.Hours, 2),
                    CounterName = x.S.CounterName,
                    Services = x.S.Services,
                    BreakMinutes = Math.Round(x.BreakHours * 60, 0),
                }).ToList(),
            Tickets = tickets
                        .OrderByDescending(x => x.StartedAt)
                        .Select(x => new HandledTicketDto
                        {
                            Code = TicketCode.Format(x.Service.Code, x.Number),
                            ServiceName = x.Service.Name,
                            StartedAt = x.StartedAt!.Value,
                            FinishedAt = x.FinishedAt,
                            Status = x.Status.ToString(),
                            DurationMinutes = x.FinishedAt == null ? null
                                : Math.Round((x.FinishedAt.Value - x.StartedAt!.Value).TotalMinutes, 1)
                        }).ToList(),
            Log = log
        };
    }

    [HttpGet("hours")]
    public async Task<ActionResult<HoursReportDto>> GetHours(
        [FromQuery] DateTime from, [FromQuery] DateTime to,
        [FromQuery] string groupBy = "week", [FromQuery] int? employeeId = null)
    {
        if (to.Date < from.Date) return BadRequest("End date is before start date");
        var (f, t) = Range(from, to);
        var spans = await LoadSpansAsync(f, t, employeeId);
        var inRange = spans.Where(x => x.S.LoginAt >= f).ToList();
        var tickets = TicketsOf(spans, await LoadTicketsAsync(f, t), employeeId);

        var points = new List<HoursPointDto>();
        for (var p = PeriodStart(f, groupBy); p < t; p = NextPeriod(p, groupBy))
        {
            var ses = inRange.Where(x => PeriodStart(x.S.LoginAt, groupBy) == p).ToList();
            var tk = tickets.Where(x => PeriodStart(x.StartedAt!.Value, groupBy) == p).ToList();
            points.Add(new HoursPointDto
            {
                PeriodStart = p,
                Label = Label(p, groupBy),
                Hours = Math.Round(ses.Sum(x => x.Hours), 2),
                Sessions = ses.Count,
                Tickets = tk.Count(x => x.Status == TicketStatus.Finished),
                AvgServiceMinutes = AvgMinutes(tk)
            });
        }

        var total = inRange.Sum(x => x.Hours);
        var workedDays = inRange.Select(x => x.S.LoginAt.Date).Distinct().Count();

        return new HoursReportDto
        {
            Points = points,
            TotalHours = Math.Round(total, 2),
            AvgHoursPerPeriod = points.Count == 0 ? 0 : Math.Round(total / points.Count, 2),
            WorkedDays = workedDays,
            AvgHoursPerWorkedDay = workedDays == 0 ? 0 : Math.Round(total / workedDays, 2),
            AvgSessionHours = inRange.Count == 0 ? 0 : Math.Round(total / inRange.Count, 2),
            TotalTickets = tickets.Count(x => x.Status == TicketStatus.Finished),
            AvgServiceMinutes = AvgMinutes(tickets)
        };
    }

    [HttpGet("non-working-days")]
    public async Task<ActionResult<List<NonWorkingDayWorkDto>>> GetNonWorkingDayWork(
        [FromQuery] DateTime from, [FromQuery] DateTime to)
    {
        if (to.Date < from.Date) return BadRequest("End date is before start date");
        var (f, t) = Range(from, to);

        var holidays = await context.NonWorkingDays
            .AsNoTracking()
            .Where(x => x.Date >= f && x.Date < t)
            .ToDictionaryAsync(x => x.Date, x => x.Name);
        var users = await context.Users
            .AsNoTracking()
            .Where(u => u.Role == Role.Employee)
            .ToDictionaryAsync(u => u.Id, u => u.Username);

        var spans = (await LoadSpansAsync(f, t, null)).Where(x => x.S.LoginAt >= f).ToList();
        var tickets = await LoadTicketsAsync(f, t);

        var result = new List<NonWorkingDayWorkDto>();
        for (var day = f; day < t; day = day.AddDays(1))
        {
            var isWeekend = day.DayOfWeek == DayOfWeek.Saturday || day.DayOfWeek == DayOfWeek.Sunday;
            var isHoliday = holidays.TryGetValue(day, out var holidayName);
            if (!isWeekend && !isHoliday) continue;

            var daySpans = spans.Where(x => x.S.LoginAt.Date == day).ToList();
            var employees = daySpans
                .GroupBy(x => x.S.UserId)
                .Select(g => new EmployeeWorkDto
                {
                    EmployeeId = g.Key,
                    Username = users.TryGetValue(g.Key, out var username) ? username : "(deleted)",
                    Sessions = g.Count(),
                    Hours = Math.Round(g.Sum(x => x.Hours), 2),
                    TicketsServed = TicketsOf(g.ToList(), tickets, g.Key)
                        .Count(x => x.Status == TicketStatus.Finished)
                })
                .OrderBy(e => e.Username)
                .ToList();

            result.Add(new NonWorkingDayWorkDto
            {
                Date = day,
                Reason = isHoliday ? holidayName! : day.DayOfWeek.ToString(),
                IsHoliday = isHoliday,
                Employees = employees
            });
        }

        return result.OrderByDescending(x => x.Date).ToList();
    }
    private static IEnumerable<DateTime> EachDay(DateTime from, DateTime to)
    {
        for (var d = from.Date; d <= to.Date; d = d.AddDays(1)) yield return d;
    }

    [HttpGet("workforce")]
    public async Task<ActionResult<WorkforceReportDto>> GetWorkforce(
        [FromQuery] DateTime from, [FromQuery] DateTime to)
    {
        if (to.Date < from.Date) return BadRequest("End date is before start date");
        var (f, t) = Range(from, to);
        var todayLocal = LocalTime.ToLocal(DateTime.UtcNow).Date;
        var cfg = await context.GetWorkSettingsAsync();
        var lateGrace = TimeSpan.FromMinutes(cfg.LateGraceMinutes);
        var earlyGrace = TimeSpan.FromMinutes(cfg.EarlyGraceMinutes);

        var employees = await context.Users.AsNoTracking()
            .Where(u => u.Role == Role.Employee)
            .Select(u => new { u.Id, u.Username, u.CounterId })
            .ToListAsync();
        var counters = await context.Counters.AsNoTracking()
            .Select(c => new { c.Id, c.Name })
            .ToListAsync();
        var firstLogins = await context.EmployeeSessions.AsNoTracking()
            .Where(s => s.BreakStartedAt == null)
            .GroupBy(s => s.UserId)
            .Select(g => new { UserId = g.Key, First = g.Min(s => s.LoginAt) })
            .ToDictionaryAsync(x => x.UserId, x => x.First);
        var holidays = (await context.NonWorkingDays.AsNoTracking()
                .Where(x => x.Date >= f && x.Date < t)
                .Select(x => x.Date).ToListAsync())
            .Select(d => d.Date).ToHashSet();
        var autoLogouts = await context.ActivityLogs.AsNoTracking()
            .Where(l => l.Action == LogActions.EmployeeLogout
                        && l.Details.Contains("auto logout") && l.CreatedAt >= f)
            .Select(l => new { l.ActorId, l.CreatedAt })
            .ToListAsync();
        var leaves = await context.Leaves.AsNoTracking()
            .Where(l => l.EndDate >= f && l.StartDate < t)
            .ToListAsync();
        var spans = (await LoadSpansAsync(f, t, null)).Where(x => x.S.LoginAt >= f).ToList();

        bool IsNonWorking(DateTime d) =>
            holidays.Contains(d) || d.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;

        bool IsAutoLogout(EmployeeSession s)
        {
            var dayEnd = LocalTime.NextMidnightUtc(s.LoginAt);
            return autoLogouts.Any(l => l.ActorId == s.UserId
                                        && l.CreatedAt >= dayEnd && l.CreatedAt < dayEnd.AddDays(1));
        }

        HashSet<DateTime> LeaveDays(int userId, LeaveType type) => leaves
            .Where(l => l.UserId == userId && l.Type == type)
            .SelectMany(l => EachDay(l.StartDate, l.EndDate))
            .ToHashSet();

        var workDays = new List<DateTime>();
        for (var d = f.Date; d < t && d < todayLocal; d = d.AddDays(1))
            if (!IsNonWorking(d)) workDays.Add(d);

        var rows = employees.Select(e =>
        {
            var byDay = spans.Where(x => x.S.UserId == e.Id)
                .GroupBy(x => LocalTime.ToLocal(x.S.LoginAt).Date)
                .ToDictionary(g => g.Key, g => g.OrderBy(x => x.S.LoginAt).ToList());

            DateTime? first = firstLogins.TryGetValue(e.Id, out var fl) ? fl : null;
            var startDay = first == null ? DateTime.MaxValue.Date : LocalTime.ToLocal(first.Value).Date;

            var vacation = LeaveDays(e.Id, LeaveType.Vacation);
            var sick = LeaveDays(e.Id, LeaveType.Sick);
            var expected = workDays.Count(d => d >= startDay && !vacation.Contains(d));
            var absent = workDays.Count(d => d >= startDay && !vacation.Contains(d) && !byDay.ContainsKey(d));

            int late = 0, early = 0;
            double lateMinutes = 0, overtime = 0;
            foreach (var (day, list) in byDay.Where(kv => !IsNonWorking(kv.Key)))
            {
                var login = LocalTime.ToLocal(list[0].S.LoginAt).TimeOfDay;
                if (login > cfg.OpenTime + lateGrace)
                {
                    late++;
                    lateMinutes += (login - cfg.OpenTime).TotalMinutes;
                }

                var last = list[^1].S;
                if (day < todayLocal && last.LogoutAt != null && !IsAutoLogout(last))
                {
                    var logout = LocalTime.ToLocal(last.LogoutAt.Value);
                    if (logout.Date == day && logout.TimeOfDay < cfg.CloseTime - earlyGrace) early++;
                }

                overtime += Math.Max(0, list.Sum(x => x.Hours) - cfg.StandardDayHours);
            }

            return new EmployeeWorkforceRowDto
            {
                Id = e.Id,
                Username = e.Username,
                FirstLoginAt = first,
                TenureDays = first == null ? 0 : (int)(todayLocal - LocalTime.ToLocal(first.Value).Date).TotalDays,
                ExpectedDays = expected,
                WorkedDays = byDay.Count,
                AbsentDays = absent,
                AbsenteeismRate = expected == 0 ? 0 : Math.Round(absent * 100.0 / expected, 1),
                VacationDays = workDays.Count(d => d >= startDay && vacation.Contains(d)),
                SickDays = workDays.Count(d => d >= startDay && sick.Contains(d) && !byDay.ContainsKey(d)),
                LateArrivals = late,
                AvgLateMinutes = late == 0 ? 0 : Math.Round(lateMinutes / late, 1),
                EarlyDepartures = early,
                OvertimeHours = Math.Round(overtime, 2)
            };
        }).OrderBy(r => r.Username).ToList();

        var totalExpected = rows.Sum(r => r.ExpectedDays);

        return new WorkforceReportDto
        {
            TotalEmployees = employees.Count,
            ActiveInRange = rows.Count(r => r.WorkedDays > 0),
            AbsenteeismRate = totalExpected == 0 ? 0 : Math.Round(rows.Sum(r => r.AbsentDays) * 100.0 / totalExpected, 1),
            TotalLateArrivals = rows.Sum(r => r.LateArrivals),
            TotalEarlyDepartures = rows.Sum(r => r.EarlyDepartures),
            TotalOvertimeHours = Math.Round(rows.Sum(r => r.OvertimeHours), 2),
            OvertimeByCounter = counters.Select(c =>
            {
                var ids = employees.Where(e => e.CounterId == c.Id).Select(e => e.Id).ToHashSet();
                return new CounterOvertimeDto
                {
                    CounterName = c.Name,
                    Employees = ids.Count,
                    OvertimeHours = Math.Round(rows.Where(r => ids.Contains(r.Id)).Sum(r => r.OvertimeHours), 2)
                };
            }).OrderBy(c => c.CounterName).ToList(),
            Employees = rows
        };
    }

    [HttpGet("pto")]
    public async Task<ActionResult<List<PtoRowDto>>> GetPto([FromQuery] int year)
    {
        var start = new DateTime(year, 1, 1);
        var end = start.AddYears(1);
        var today = LocalTime.ToLocal(DateTime.UtcNow).Date;

        var daysInYear = (end - start).TotalDays;
        var elapsed = Math.Clamp((today - start).TotalDays + 1, 0, daysInYear);


        var holidays = (await context.NonWorkingDays.AsNoTracking()
                .Where(x => x.Date >= start && x.Date < end)
                .Select(x => x.Date).ToListAsync())
            .Select(d => d.Date).ToHashSet();
        bool IsWorking(DateTime d) =>
            !holidays.Contains(d) && d.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday);

        var employees = await context.Users.AsNoTracking()
    .Where(u => u.Role == Role.Employee)
    .Select(u => new { u.Id, u.Username })
    .ToListAsync();
        var cfg = await context.GetWorkSettingsAsync();
        var leaves = await context.Leaves.AsNoTracking()
            .Where(l => l.EndDate >= start && l.StartDate < end)
            .ToListAsync();

        return employees.Select(e =>
        {
            var days = leaves.Where(l => l.UserId == e.Id)
                .SelectMany(l => EachDay(l.StartDate, l.EndDate)
                    .Where(d => d >= start && d < end && IsWorking(d))
                    .Select(d => new { l.Type, Day = d }))
                .ToList();
            var used = days.Count(x => x.Type == LeaveType.Vacation && x.Day <= today);
            var scheduled = days.Count(x => x.Type == LeaveType.Vacation && x.Day > today);
            var sickUsed = days.Count(x => x.Type == LeaveType.Sick && x.Day <= today);

            return new PtoRowDto
            {
                Id = e.Id,
                Username = e.Username,
                VacationAllowance = cfg.AnnualLeaveDays,

                VacationUsed = used,
                VacationScheduled = scheduled,
                VacationRemaining = cfg.AnnualLeaveDays - used - scheduled,
                SickAllowance = cfg.AnnualSickDays,
                SickUsed = sickUsed,
                SickRemaining = cfg.AnnualSickDays - sickUsed
            };
        }).OrderBy(r => r.Username).ToList();
    }

    private static (DateTime From, DateTime ToExcl) Range(DateTime from, DateTime to)
    {
        var today = DateTime.UtcNow.Date;
        var last = to.Date > today ? today : to.Date;
        return (from.Date, last.AddDays(1));
    }

    private async Task<List<SessionSpan>> LoadSpansAsync(DateTime from, DateTime toExcl, int? userId)
    {
        var now = DateTime.UtcNow;
        var query = context.EmployeeSessions
            .AsNoTracking()
            .Where(s => s.BreakStartedAt == null && s.LoginAt < toExcl && (s.LogoutAt == null || s.LogoutAt >= from));
        if (userId != null) query = query.Where(s => s.UserId == userId);

        var sessions = await query.OrderBy(s => s.LoginAt).ToListAsync();

        var userIds = sessions.Select(s => s.UserId).Distinct().ToList();
        var breaks = await context.EmployeeSessions
            .AsNoTracking()
            .Where(b => b.BreakStartedAt != null && userIds.Contains(b.UserId))
            .ToListAsync();

        var spans = new List<SessionSpan>();
        foreach (var s in sessions)
        {
            var end = s.LogoutAt ?? await SessionEnd.ForOpenSessionAsync(context, s, now);
            spans.Add(new SessionSpan(s, end, BreakHoursOf(s, end, breaks)));
        }
        return spans.Where(x => x.End >= from).ToList();
    }

    private Task<List<Ticket>> LoadTicketsAsync(DateTime from, DateTime toExcl) =>
        context.Tickets
            .AsNoTracking()
            .Include(t => t.Service)
            .Where(t => t.CounterId != null && t.StartedAt != null &&
                        t.StartedAt >= from && t.StartedAt < toExcl)
            .ToListAsync();


    private static List<Ticket> TicketsOf(List<SessionSpan> spans, List<Ticket> tickets, int? userId = null) =>
        tickets.Where(t => spans.Any(x =>
            (userId == null || x.S.UserId == userId) &&
            x.S.CounterId == t.CounterId &&
            t.StartedAt >= x.S.LoginAt && t.StartedAt <= x.End)).ToList();

    private static double AvgMinutes(IEnumerable<Ticket> tickets)
    {
        var durations = tickets
            .Where(t => t.Status == TicketStatus.Finished && t.StartedAt != null && t.FinishedAt != null)
            .Select(t => (t.FinishedAt!.Value - t.StartedAt!.Value).TotalMinutes)
            .ToList();
        return durations.Count == 0 ? 0 : Math.Round(durations.Average(), 1);
    }

    private static DateTime PeriodStart(DateTime d, string g) => g switch
    {
        "week" => d.Date.AddDays(-(((int)d.DayOfWeek + 6) % 7)),
        "month" => new DateTime(d.Year, d.Month, 1),
        "year" => new DateTime(d.Year, 1, 1),
        _ => d.Date
    };

    private static DateTime NextPeriod(DateTime p, string g) => g switch
    {
        "week" => p.AddDays(7),
        "month" => p.AddMonths(1),
        "year" => p.AddYears(1),
        _ => p.AddDays(1)
    };

    private static string Label(DateTime p, string g) => g switch
    {
        "week" => "Week of " + p.ToString("yyyy-MM-dd"),
        "month" => p.ToString("yyyy-MM"),
        "year" => p.ToString("yyyy"),
        _ => p.ToString("yyyy-MM-dd")
    };

    private static double BreakLengthHours(EmployeeSession b, DateTime now)
    {
        var start = b.BreakStartedAt!.Value;
        var endOfDay = start.Date.AddDays(1);
        var cap = now < endOfDay ? now : endOfDay;
        var stop = b.BreakEndedAt ?? cap;
        if (stop > cap) stop = cap;
        return stop > start ? (stop - start).TotalHours : 0;
    }
}