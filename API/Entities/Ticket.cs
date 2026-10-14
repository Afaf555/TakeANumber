using API.Entities.Enums;

namespace API.Entities;

public class Ticket
{
    public int Id { get; set; }
    public int Number { get; set; }
    public TicketStatus Status { get; set; } = TicketStatus.InQueue;

    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }

    public int ServiceId { get; set; }
    public Service Service { get; set; } = null!;

    public int? ClientId { get; set; }
    public User? Client { get; set; }

    public int? CounterId { get; set; }
    public Counter? Counter { get; set; }
}