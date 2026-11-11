namespace API.DTOs;

public class EmployeePhasesDto
{
    public DateTime Date { get; set; }
    public int EmployeeId { get; set; }
    public string Username { get; set; } = "";
    public double ServingMinutes { get; set; }   // phase 1
    public double IdleMinutes { get; set; }      // phase 2
    public double NeglectMinutes { get; set; }   // phase 3
    public double BreakMinutes { get; set; }     // phase 4
    public double AfterHoursMinutes { get; set; }
}