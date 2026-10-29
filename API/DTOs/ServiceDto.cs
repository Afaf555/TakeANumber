using System;

namespace API.DTOs;

public class ServiceDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Code { get; set; } = "";
    public bool HasActiveTickets { get; set; }
}
