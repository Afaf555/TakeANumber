using API.Entities.Enums;
namespace API.Entities;
public class Leave
{
    public int Id  { get; set; }
    public int UserId { get; set; }
    public User User {get; set; }=null!;
    public LeaveType Type { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public LeaveStatus Status { get; set; } = LeaveStatus.Approved;
    public string? Note { get; set; }

}