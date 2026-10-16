namespace API.DTOs;

public class ServiceQueueDto
{
    public int ServiceId { get; set; }
    public string ServiceName { get; set; } = "";
    public List<ServingDto> Serving { get; set; } = [];
    public List<string> NextCodes { get; set; } = [];
}

public class ServingDto
{
    public string Code { get; set; } = "";
    public string CounterName { get; set; } = "";
}