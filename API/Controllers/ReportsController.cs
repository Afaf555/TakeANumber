using API.Data;
using API.DTOs;
using API.Entities;
using API.Entities.Enums;
using API.Extensions;
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

    [HttpGet("employees")]
    public async Task<ActionResult<List<EmployeeReportRowDto>>> GetEmployees(
        [FromQuery] DateTime from, [FromQuery] DateTime to)
    {
        var (f, t) = Range(from, to);
        var counters = await context.Counters.ToDictionaryAsync(c => c.Id, c => c.Name);
        var employees = await context.Users
            .Where(u => u.Role == Role.Employee)
            .Select(u => new { u.Id, u.Username, u.CounterId })
            .ToListAsync();
        var spans = await LoadSpansAsync(f, t, null);
        var tickets = await LoadTicketsAsync(f, t);
        var now=DateTime.UtcNow;
        var breakRows=await context.EmployeeSessions.Where(b=>b.BreakStartedAt!=null && b.BreakStartedAt>=f&& b.BreakStartedAt<t).ToListAsync();
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

        return new EmployeeActivityDto
        {
            Sessions = spans
                .Where(x => x.S.LoginAt >= f)
                .OrderByDescending(x => x.S.LoginAt)
                .Select(x => new SessionReportDto
                {
                    Id = x.S.Id,
                    LoginAt = x.S.LoginAt,
                    LogoutAt = x.S.LogoutAt,
                    IsOpen = x.S.LogoutAt==null,
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
        var (f, t) = Range(from, to);

        var holidays = await context.NonWorkingDays
            .Where(x => x.Date >= f && x.Date < t)
            .ToDictionaryAsync(x => x.Date, x => x.Name);
        var users = await context.Users
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
            .Where(s => s.BreakStartedAt == null && s.LoginAt < toExcl && (s.LogoutAt == null || s.LogoutAt >= from));
        if (userId != null) query = query.Where(s => s.UserId == userId);

        var sessions = await query.OrderBy(s => s.LoginAt).ToListAsync();

        var userIds = sessions.Select(s => s.UserId).Distinct().ToList();
        var breaks = await context.EmployeeSessions
            .Where(b => b.BreakStartedAt != null && userIds.Contains(b.UserId))
            .ToListAsync();

        return sessions
            .Select(s =>
            {
                var endOfDay = LocalTime.SessionEndUtc(s.LoginAt);
                var end = s.LogoutAt ?? (now < endOfDay ? now : endOfDay);
                return new SessionSpan(s, end, BreakHoursOf(s, end, breaks));
            })
            .Where(x => x.End >= from)
            .ToList();
    }

    private Task<List<Ticket>> LoadTicketsAsync(DateTime from, DateTime toExcl) =>
        context.Tickets
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

    private static double BreakLengthHours(EmployeeSession b,DateTime now)
    {
        var start=b.BreakStartedAt!.Value;
        var endOfDay=start.Date.AddDays(1);
        var cap=now<endOfDay?now:endOfDay;
        var stop=b.BreakEndedAt??cap;
        if(stop>cap) stop=cap;
        return stop>start?(stop-start).TotalHours:0;
    }
}