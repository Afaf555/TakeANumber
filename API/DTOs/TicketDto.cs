using System;
using API.Extensions;

namespace API.DTOs;

public class TicketDto
{
    public int Id { get; set; }
    public int Number { get; set; }
    public int ServiceId { get; set; }
    public string ServiceName { get; set; } = "";
    public string Status { get; set; } = "c";
    public DateTime CreatedAt { get; set; }
    public int PeopleAhead { get; set; }
    public string ServiceCode { get; set; } = "";
    public string Code => TicketCode.Format(ServiceCode, Number); public string? CounterName { get; set; }
}
