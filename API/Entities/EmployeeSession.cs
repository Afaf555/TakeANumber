namespace API.Entities;

public class EmployeeSession
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; }=null!;
    public DateTime LoginAt { get; set; }=DateTime.UtcNow;
    public DateTime? LogoutAt { get; set; }
    public int? CounterId { get; set; }
    public string? CounterName { get; set; }
    public string? Services { get; set; }
    
}