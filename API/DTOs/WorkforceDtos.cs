using API.Entities.Enums;

namespace API.DTOs;

public class CounterOvertimeDto
{
    public string CounterName { get; set; } = "";
    public int Employees { get; set; }
    public double OvertimeHours { get; set; }
}

public class EmployeeWorkforceRowDto
{
    public int Id { get; set; }
    public string Username { get; set; } = "";
    public DateTime? FirstLoginAt { get; set; }
    public int TenureDays { get; set; }
    public int ExpectedDays { get; set; }
    public int WorkedDays { get; set; }
    public int AbsentDays { get; set; }
    public double AbsenteeismRate { get; set; }
    public int VacationDays { get; set; }
    public int SickDays { get; set; }
    public int LateArrivals { get; set; }
    public double AvgLateMinutes { get; set; }
    public int EarlyDepartures { get; set; }
    public double OvertimeHours { get; set; }
}

public class WorkforceReportDto
{
    public int TotalEmployees { get; set; }
    public int ActiveInRange { get; set; }
    public double AbsenteeismRate { get; set; }
    public int TotalLateArrivals { get; set; }
    public int TotalEarlyDepartures { get; set; }
    public double TotalOvertimeHours { get; set; }
    public List<CounterOvertimeDto> OvertimeByCounter { get; set; } = [];
    public List<EmployeeWorkforceRowDto> Employees { get; set; } = [];
}

public class PtoRowDto
{
    public int Id { get; set; }
    public string Username { get; set; } = "";
    public int VacationAllowance { get; set; }
    public int VacationUsed { get; set; }
    public int VacationScheduled { get; set; }
    public int VacationRemaining { get; set; }
    public int SickAllowance { get; set; }
    public int SickUsed { get; set; }
    public int SickRemaining { get; set; }
}

public class LeaveDto
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public string Username { get; set; } = "";
    public string Type { get; set; } = "";
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Status { get; set; } = "";
    public string? Note { get; set; }
}

public class CreateLeaveDto
{
    public int EmployeeId { get; set; }
    public LeaveType Type { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? Note { get; set; }
}

public class RequestLeaveDto
{
    public LeaveType Type { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? Note { get; set; }
}

public class LeaveBalanceDto
{
    public int Year { get; set; }
    public int VacationAllowance { get; set; }
    public int VacationUsed { get; set; }      
    public int VacationRemaining { get; set; }
    public int SickAllowance { get; set; }
    public int SickUsed { get; set; }
    public int SickRemaining { get; set; }
}
