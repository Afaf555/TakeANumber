namespace API.Entities;
public class ActivityLog
{
    public int Id {get; set;}
    public DateTime CreatedAt { get; set; }=DateTime.UtcNow;
    public string Action { get; set; }="";
    public string EntityType { get; set; }="";
    public int? EntityId { get; set; }
    public string Details { get; set; }="";

    public int? ActorId { get; set; }
    public string? ActorName { get; set; }
}