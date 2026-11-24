namespace API.DTOs;

public class EmployeeOverviewDto
{
    public string? CounterName { get; set; }
    public List<WaitingTicketDto> Waiting { get; set; } = [];
    public int ServedToday { get; set; }
    public int NoShowsToday { get; set; }
    public double AvgServiceMinutes { get; set; }
}

public class WaitingTicketDto
{
    public string Code { get; set; } = "";
    public string ServiceName { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}
