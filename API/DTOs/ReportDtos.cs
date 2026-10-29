using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace API.DTOs;

public class EmployeeReportRowDto
{
    public int Id { get; set; }
    public string Username { get; set; } = "";
    public string? CounterName { get; set; }
    public int Sessions { get; set; }
    public double HoursWorked { get; set; }
    public int TicketsServed { get; set; }
    public int NoShows { get; set; }
    public double AvgServiceMinutes { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public double NonWorkingHours { get; set; }
    public double BreakHours { get; set; }

}
public class SessionReportDto
{
    public int Id { get; set; }
    public DateTime LoginAt { get; set; }
    public DateTime? LogoutAt { get; set; }
    public double DurationHours { get; set; }
    public bool IsOpen { get; set; }
    public string? CounterName { get; set; }
    public string? Services { get; set; }
    public double BreakMinutes {get; set;}
}

public class HandledTicketDto
{
    public string Code { get; set; } = "";
    public string ServiceName { get; set; } = "";
    public DateTime StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
    public string Status { get; set; } = "";
    public double? DurationMinutes { get; set; }
}

public class ActivityEntryDto
{
    public DateTime CreatedAt { get; set; }
    public string Action { get; set; } = "";
    public string? Details { get; set; }
}
public class EmployeeActivityDto
{
    public List<SessionReportDto> Sessions { get; set; } = [];
    public List<HandledTicketDto> Tickets { get; set; } = [];
    public List<ActivityEntryDto> Log { get; set; } = [];
}
public class HoursPointDto
{
    public DateTime PeriodStart { get; set; }
    public string Label { get; set; } = "";
    public double Hours { get; set; }
    public int Sessions { get; set; }
    public int Tickets { get; set; }
    public double AvgServiceMinutes { get; set; }
}
public class HoursReportDto
{
    public List<HoursPointDto> Points { get; set; } = [];
    public double TotalHours { get; set; }
    public double AvgHoursPerPeriod { get; set; }
    public int WorkedDays { get; set; }
    public double AvgHoursPerWorkedDay { get; set; }
    public double AvgSessionHours { get; set; }
    public int TotalTickets { get; set; }
    public double AvgServiceMinutes { get; set; }
}