namespace API.DTOs;

public class ServiceQueueDto
{
    public int ServiceId { get; set; }
    public string ServiceName { get; set; } = "";
    public List<ServingDto> Serving { get; set; } = [];
    public List<string> NextCodes { get; set; } = [];
    public int Waiting { get; set; }                 // all tickets in queue for this service
    public DateTime? OldestWaitingSince { get; set; } // CreatedAt of the longest waiting ticket
}

public class ServingDto
{
    public string Code { get; set; } = "";
    public string CounterName { get; set; } = "";
}