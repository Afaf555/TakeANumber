namespace API.Entities;

public class SessionBreak
{
    public int Id { get; set; }
    public int SessionId { get; set; }
    public EmployeeSession Session { get; set; } = null!;
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? EndedAt { get; set; }
}