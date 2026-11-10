namespace API.DTOs;

public class WorkSettingsDto
{
    public string OpenTime { get; set; } = "08:00";
    public string CloseTime { get; set; } = "17:00";
    public int LateGraceMinutes { get; set; }
    public int EarlyGraceMinutes { get; set; }
    public double StandardDayHours { get; set; }
    public int QueueAlertWaiting { get; set; }
    public int QueueAlertMinutes { get; set; }
    public int AnnualLeaveDays { get; set; }
    public int AnnualSickDays { get; set; }
}