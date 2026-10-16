using API.Extensions;

namespace API.DTOs;

public class AdminTicketDto
{
    public int Id { get; set; }
    public int Number { get; set; }
    public string ServiceName { get; set; } = "";
    public string Status { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public string? CounterName { get; set; }
    public string? ClientUsername { get; set; }
    public string ServiceCode { get; set; } = "";
    public string Code => TicketCode.Format(ServiceCode, Number);
}