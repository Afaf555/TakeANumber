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

[Authorize]
public class LeaveController(AppDbContext context, ActivityLogger logger) : BaseApiController
{
    private static string? CleanNote(string? note)
    {
        var n = note?.Trim();
        if (string.IsNullOrEmpty(n)) return null;
        return n.Length > 200 ? n[..200] : n;
    }

    private static string Describe(string username, Leave l) =>
        $"{username}: {l.Type} {l.StartDate:yyyy-MM-dd} - {l.EndDate:yyyy-MM-dd}"
        + (string.IsNullOrEmpty(l.Note) ? "" : $" ({l.Note})");

    private static LeaveDto ToDto(Leave l, string username) => new()
    {
        Id = l.Id,
        EmployeeId = l.UserId,
        Username = username,
        Type = l.Type.ToString(),
        StartDate = l.StartDate,
        EndDate = l.EndDate,
        Status = l.Status.ToString(),
        Note = l.Note
    };

    private Task<bool> OverlapsAsync(int userId, DateTime start, DateTime end) =>
        context.Leaves.AnyAsync(l => l.UserId == userId
                                     && l.Status != LeaveStatus.Rejected
                                     && l.StartDate <= end && l.EndDate >= start);

   
    private async Task<HashSet<DateTime>> HolidaysAsync(DateTime from, DateTime to) =>
        (await context.NonWorkingDays.AsNoTracking()
            .Where(x => x.Date >= from && x.Date <= to)
            .Select(x => x.Date).ToListAsync())
        .Select(d => d.Date).ToHashSet();

    private static int CountWorkingDays(DateTime start, DateTime end, HashSet<DateTime> holidays)
    {
        var n = 0;
        for (var d = start.Date; d <= end.Date; d = d.AddDays(1))
            if (d.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday) && !holidays.Contains(d)) n++;
        return n;
    }

   
    private async Task<int> UsedDaysAsync(int userId, LeaveType type, int year, HashSet<DateTime> holidays)
    {
        var yStart = new DateTime(year, 1, 1);
        var yEnd = new DateTime(year, 12, 31);
        var leaves = await context.Leaves.AsNoTracking()
            .Where(l => l.UserId == userId && l.Type == type
                        && l.Status != LeaveStatus.Rejected
                        && l.EndDate >= yStart && l.StartDate <= yEnd)
            .Select(l => new { l.StartDate, l.EndDate })
            .ToListAsync();

        return leaves.Sum(l => CountWorkingDays(
            l.StartDate < yStart ? yStart : l.StartDate,
            l.EndDate > yEnd ? yEnd : l.EndDate,
            holidays));
    }


    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<ActionResult<List<LeaveDto>>> GetLeaves(
        [FromQuery] int? employeeId, [FromQuery] int? year)
    {
        var query = context.Leaves.AsNoTracking().AsQueryable();
        if (employeeId != null) query = query.Where(l => l.UserId == employeeId);
        if (year != null) query = query.Where(l => l.StartDate.Year <= year && l.EndDate.Year >= year);

        return await query
           
            .OrderBy(l => l.Status == LeaveStatus.Pending ? 0 : 1)
            .ThenByDescending(l => l.StartDate)
            .Select(l => new LeaveDto
            {
                Id = l.Id,
                EmployeeId = l.UserId,
                Username = l.User.Username,
                Type = l.Type.ToString(),
                StartDate = l.StartDate,
                EndDate = l.EndDate,
                Status = l.Status.ToString(),
                Note = l.Note
            })
            .ToListAsync();
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<LeaveDto>> CreateLeave(CreateLeaveDto dto)
    {
        if (!Enum.IsDefined(dto.Type)) return BadRequest("Invalid leave type");
        var today=DateTime.Now.Date;
        var employee = await context.Users
            .SingleOrDefaultAsync(u => u.Id == dto.EmployeeId && u.Role == Role.Employee);
        if (employee == null) return BadRequest("Employee does not exist");

        var start = dto.StartDate.Date;
        var end = dto.EndDate.Date;
        if (end < start) return BadRequest("End date is before start date");
        if(start<today) return BadRequest("Date can't be before today");

        if (await OverlapsAsync(employee.Id, start, end))
            return BadRequest("Leave overlaps with an existing one");

        var leave = new Leave
        {
            UserId = employee.Id,
            Type = dto.Type,
            StartDate = start,
            EndDate = end,
            Status = LeaveStatus.Approved,
            Note = CleanNote(dto.Note)
        };
        context.Leaves.Add(leave);
        await context.SaveChangesAsync();

        await logger.LogAsync(LogActions.LeaveAdded, "Employee", employee.Id, Describe(employee.Username, leave));
        return ToDto(leave, employee.Username);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("{id}/approve")]
    public async Task<ActionResult<LeaveDto>> Approve(int id)
    {
        var leave = await context.Leaves.Include(l => l.User).SingleOrDefaultAsync(l => l.Id == id);
        if (leave == null) return NotFound();
        if (leave.Status != LeaveStatus.Pending) return BadRequest("Request is not pending");

        leave.Status = LeaveStatus.Approved;
        await context.SaveChangesAsync();

        await logger.LogAsync(LogActions.LeaveApproved, "Employee", leave.UserId, Describe(leave.User.Username, leave));
        return ToDto(leave, leave.User.Username);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("{id}/reject")]
    public async Task<ActionResult<LeaveDto>> Reject(int id)
    {
        var leave = await context.Leaves.Include(l => l.User).SingleOrDefaultAsync(l => l.Id == id);
        if (leave == null) return NotFound();
        if (leave.Status != LeaveStatus.Pending) return BadRequest("Request is not pending");

        leave.Status = LeaveStatus.Rejected;
        await context.SaveChangesAsync();

        await logger.LogAsync(LogActions.LeaveRejected, "Employee", leave.UserId, Describe(leave.User.Username, leave));
        return ToDto(leave, leave.User.Username);
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteLeave(int id)
    {
        var leave = await context.Leaves.Include(l => l.User).SingleOrDefaultAsync(l => l.Id == id);
        if (leave == null) return NotFound();

        context.Leaves.Remove(leave);
        await context.SaveChangesAsync();

        await logger.LogAsync(LogActions.LeaveRemoved, "Employee", leave.UserId, Describe(leave.User.Username, leave));
        return NoContent();
    }

   

    [Authorize(Roles = "Employee")]
    [HttpGet("mine")]
    public async Task<ActionResult<List<LeaveDto>>> GetMine([FromQuery] int? year)
    {
        var userId = User.GetUserId();
        var query = context.Leaves.AsNoTracking().Where(l => l.UserId == userId);
        if (year != null) query = query.Where(l => l.StartDate.Year <= year && l.EndDate.Year >= year);

        return await query
            .OrderByDescending(l => l.StartDate)
            .Select(l => new LeaveDto
            {
                Id = l.Id,
                EmployeeId = l.UserId,
                Username = l.User.Username,
                Type = l.Type.ToString(),
                StartDate = l.StartDate,
                EndDate = l.EndDate,
                Status = l.Status.ToString(),
                Note = l.Note
            })
            .ToListAsync();
    }

    [Authorize(Roles = "Employee")]
    [HttpGet("mine/balance")]
    public async Task<ActionResult<LeaveBalanceDto>> GetMyBalance([FromQuery] int? year)
    {
        var userId = User.GetUserId();
        var y = year ?? DateTime.UtcNow.Year;
        var employee = await context.Users.AsNoTracking()
            .SingleOrDefaultAsync(u => u.Id == userId && u.Role == Role.Employee);
        if (employee == null) return BadRequest("Employee does not exist");

        var holidays = await HolidaysAsync(new DateTime(y, 1, 1), new DateTime(y, 12, 31));
        var vacationUsed = await UsedDaysAsync(userId, LeaveType.Vacation, y, holidays);
        var sickUsed = await UsedDaysAsync(userId, LeaveType.Sick, y, holidays);

        return new LeaveBalanceDto
        {
            Year = y,
            VacationAllowance = employee.AnnualLeaveDays,
            VacationUsed = vacationUsed,
            VacationRemaining = employee.AnnualLeaveDays - vacationUsed,
            SickAllowance = employee.AnnualSickDays,
            SickUsed = sickUsed,
            SickRemaining = employee.AnnualSickDays - sickUsed
        };
    }

    [Authorize(Roles = "Employee")]
    [HttpPost("request")]
    public async Task<ActionResult<LeaveDto>> RequestLeave(RequestLeaveDto dto)
    {
        if (!Enum.IsDefined(dto.Type)) return BadRequest("Invalid leave reason");

        var userId = User.GetUserId();
        var employee = await context.Users
            .SingleOrDefaultAsync(u => u.Id == userId && u.Role == Role.Employee);
        if (employee == null) return BadRequest("Employee does not exist");
        var today=DateTime.Now.Date;
        var start = dto.StartDate.Date;
        var end = dto.EndDate.Date;
        if (end < start) return BadRequest("End date is before start date");
        if(start<today) return BadRequest("Can't make a request for previous days");
        
        if (dto.Type == LeaveType.Vacation && start < DateTime.UtcNow.Date)
            return BadRequest("Vacation cannot start in the past");

        if (await OverlapsAsync(employee.Id, start, end))
            return BadRequest("You already have a leave request for these dates");

       
        var allowance = dto.Type == LeaveType.Vacation ? employee.AnnualLeaveDays : employee.AnnualSickDays;
        var label = dto.Type == LeaveType.Vacation ? "vacation" : "sick";
        var totalRequested = 0;
        for (var year = start.Year; year <= end.Year; year++)
        {
            var yStart = new DateTime(year, 1, 1);
            var yEnd = new DateTime(year, 12, 31);
            var holidays = await HolidaysAsync(yStart, yEnd);

            var requested = CountWorkingDays(start > yStart ? start : yStart, end < yEnd ? end : yEnd, holidays);
            if (requested == 0) continue;
            totalRequested += requested;

            var remaining = allowance - await UsedDaysAsync(employee.Id, dto.Type, year, holidays);
            if (requested > remaining)
                return BadRequest($"Not enough {label} days left in {year}: {Math.Max(0, remaining)} remaining, {requested} requested.");
        }
        if (totalRequested == 0)
            return BadRequest("The selected dates contain no working days (weekends and holidays are not counted).");

        var leave = new Leave
        {
            UserId = employee.Id,
            Type = dto.Type,
            StartDate = start,
            EndDate = end,
            Status = LeaveStatus.Pending,
            Note = CleanNote(dto.Note)
        };
        context.Leaves.Add(leave);
        await context.SaveChangesAsync();

        await logger.LogAsync(LogActions.LeaveRequested, "Employee", employee.Id, Describe(employee.Username, leave));
        return ToDto(leave, employee.Username);
    }

    [Authorize(Roles = "Employee")]
    [HttpDelete("mine/{id}")]
    public async Task<ActionResult> CancelMine(int id)
    {
        var userId = User.GetUserId();
        var leave = await context.Leaves.Include(l => l.User)
            .SingleOrDefaultAsync(l => l.Id == id && l.UserId == userId);
        if (leave == null) return NotFound();
        if (leave.Status != LeaveStatus.Pending)
            return BadRequest("Only pending requests can be cancelled");

        context.Leaves.Remove(leave);
        await context.SaveChangesAsync();

        await logger.LogAsync(LogActions.LeaveCancelled, "Employee", leave.UserId, Describe(leave.User.Username, leave));
        return NoContent();
    }
}
