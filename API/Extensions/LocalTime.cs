namespace API.Extensions;

public static class LocalTime
{
    private static readonly TimeZoneInfo Zone = FindZone();

    // The system works until this local (Skopje) time
    public static readonly TimeSpan CloseTime = new(17, 0, 0);
    public static string CloseTimeText => $"{CloseTime.Hours:00}:{CloseTime.Minutes:00}";

    private static TimeZoneInfo FindZone()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("Europe/Skopje"); }
        catch { return TimeZoneInfo.FindSystemTimeZoneById("Central Europe Standard Time"); }
    }

    private static DateTime ToUtc(DateTime local) =>
        TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(local, DateTimeKind.Unspecified), Zone);

    public static DateTime ToLocal(DateTime utc) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), Zone);

    // UTC moment when the given local date starts
    public static DateTime LocalDayStartUtc(DateTime localDate) => ToUtc(localDate.Date);

    // Start of "today" in Skopje, expressed in UTC (use for CreatedAt >= today)
    public static DateTime TodayStartUtc() => LocalDayStartUtc(ToLocal(DateTime.UtcNow));

    // Closing moment (UTC) of the local day that contains the given UTC moment
    public static DateTime CloseOfDayUtc(DateTime utc) => ToUtc(ToLocal(utc).Date + CloseTime);

    // True when the given UTC moment is at or after that day's closing time
    public static bool IsClosed(DateTime utc) => utc >= CloseOfDayUtc(utc);

    // Where a session that started at loginUtc ends if nobody logs out: that day's closing time
    public static DateTime SessionEndUtc(DateTime loginUtc)
    {
        var close = CloseOfDayUtc(loginUtc);
        return close > loginUtc ? close : loginUtc;
    }
}
