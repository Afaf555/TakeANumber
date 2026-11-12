namespace API.DTOs;

public class EmployeePhasesDto
{
    public DateTime Date { get; set; }
    public int EmployeeId { get; set; }
    public string Username { get; set; } = "";
    public double ServingMinutes { get; set; }   // phase 1
    public double IdleMinutes { get; set; }      // phase 2
    public double NeglectMinutes { get; set; }   // phase 3
    public double BreakMinutes { get; set; }     // phase 4
    public double AfterHoursMinutes { get; set; }
}
public class PhaseTimelineDto
{
    public double OpenMinute { get; set; }     // minutes from local midnight
    public double CloseMinute { get; set; }
    public List<PhaseTimelineDayDto> Days { get; set; } = [];
}

public class PhaseTimelineDayDto
{
    public DateTime Date { get; set; }
    public List<PhaseSegmentDto> Sessions { get; set; } = [];   // login -> logout
    public List<PhaseSegmentDto> Segments { get; set; } = [];   // phases inside the sessions
}

public class PhaseSegmentDto
{
    public string Phase { get; set; } = "";     // Session / Serving / Idle / Neglect / Break
    public string? Ticket { get; set; }
    public string From { get; set; } = "";      // local time HH:mm
    public string To { get; set; } = "";
    public double StartMinute { get; set; }     // minutes from local midnight
    public double EndMinute { get; set; }
}
