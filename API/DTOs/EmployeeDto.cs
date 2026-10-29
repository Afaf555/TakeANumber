namespace API.DTOs;

public class EmployeeDto
{
    public int Id { get; set; }
    public string Username { get; set; } = "";
    public int? CounterId { get; set; }
    public string? CounterName { get; set; }
    public bool HasActiveTickets { get; set; }
}