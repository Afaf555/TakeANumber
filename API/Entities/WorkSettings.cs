using System.Globalization;

namespace API.Entities;

public static class SettingKeys
{
    public const string Work = "WorkSettings";

    public const string OpenTime = "OpenTime";
    public const string CloseTime = "CloseTime";
    public const string LateGraceMinutes = "LateGraceMinutes";
    public const string EarlyGraceMinutes = "EarlyGraceMinutes";
    public const string StandardDayHours = "StandardDayHours";
    public const string QueueAlertWaiting = "QueueAlertWaiting";
    public const string QueueAlertMinutes = "QueueAlertMinutes";
    public const string AnnualLeaveDays = "AnnualLeaveDays";
    public const string AnnualSickDays = "AnnualSickDays";
}


public class WorkSettings
{
    public TimeSpan OpenTime { get; set; } = new(8, 0, 0);
    public TimeSpan CloseTime { get; set; } = new(17, 0, 0);
    public int LateGraceMinutes { get; set; } = 10;
    public int EarlyGraceMinutes { get; set; } = 10;
    public double StandardDayHours { get; set; } = 8;
    public int QueueAlertWaiting { get; set; } = 10;   
    public int QueueAlertMinutes { get; set; } = 15;   
    public int AnnualLeaveDays { get; set; } = 20;
    public int AnnualSickDays { get; set; } = 10;

  
    public static WorkSettings From(IReadOnlyDictionary<string, string> map)
    {
        var d = new WorkSettings();
        return new WorkSettings
        {
            OpenTime = Time(map, SettingKeys.OpenTime, d.OpenTime),
            CloseTime = Time(map, SettingKeys.CloseTime, d.CloseTime),
            LateGraceMinutes = Int(map, SettingKeys.LateGraceMinutes, d.LateGraceMinutes),
            EarlyGraceMinutes = Int(map, SettingKeys.EarlyGraceMinutes, d.EarlyGraceMinutes),
            StandardDayHours = Dbl(map, SettingKeys.StandardDayHours, d.StandardDayHours),
            QueueAlertWaiting = Int(map, SettingKeys.QueueAlertWaiting, d.QueueAlertWaiting),
            QueueAlertMinutes = Int(map, SettingKeys.QueueAlertMinutes, d.QueueAlertMinutes),
            AnnualLeaveDays = Int(map, SettingKeys.AnnualLeaveDays, d.AnnualLeaveDays),
            AnnualSickDays = Int(map, SettingKeys.AnnualSickDays, d.AnnualSickDays)
        };
    }

  
    public string Serialize()
    {
        var inv = CultureInfo.InvariantCulture;
        var pairs = new[]
        {
            (SettingKeys.OpenTime, OpenTime.ToString(@"hh\:mm")),
            (SettingKeys.CloseTime, CloseTime.ToString(@"hh\:mm")),
            (SettingKeys.LateGraceMinutes, LateGraceMinutes.ToString(inv)),
            (SettingKeys.EarlyGraceMinutes, EarlyGraceMinutes.ToString(inv)),
            (SettingKeys.StandardDayHours, StandardDayHours.ToString(inv)),
            (SettingKeys.QueueAlertWaiting, QueueAlertWaiting.ToString(inv)),
            (SettingKeys.QueueAlertMinutes, QueueAlertMinutes.ToString(inv)),
            (SettingKeys.AnnualLeaveDays, AnnualLeaveDays.ToString(inv)),
            (SettingKeys.AnnualSickDays, AnnualSickDays.ToString(inv))
        };
        return string.Join(", ", pairs.Select(p => $"{p.Item1}={p.Item2}"));
    }

    public static WorkSettings Parse(string? text)
    {
        var map = new Dictionary<string, string>();
        var parts = (text ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var part in parts)
        {
            var i = part.IndexOf('=');
            if (i > 0) map[part[..i].Trim()] = part[(i + 1)..].Trim();
        }
        return From(map);
    }

    private static TimeSpan Time(IReadOnlyDictionary<string, string> m, string key, TimeSpan fallback) =>
        m.TryGetValue(key, out var v) && TimeSpan.TryParseExact(v, @"hh\:mm", CultureInfo.InvariantCulture, out var t)
            ? t : fallback;

    private static int Int(IReadOnlyDictionary<string, string> m, string key, int fallback) =>
        m.TryGetValue(key, out var v) && int.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i)
            ? i : fallback;

    private static double Dbl(IReadOnlyDictionary<string, string> m, string key, double fallback) =>
        m.TryGetValue(key, out var v) && double.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out var x)
            ? x : fallback;
}