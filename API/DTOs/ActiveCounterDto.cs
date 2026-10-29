using System;

namespace API.DTOs;

public class ActiveCounterDto
{
    public int CounterId { get; set; }
    public string CounterName { get; set; } = "";
    public int? CurrentTicketNumber { get; set; }
    public string? CurrentTicketCode { get; set; }
    public bool IsOnBreak { get; set; }
}
