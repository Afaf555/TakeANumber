namespace API.Extensions;

public static class LocalTime
{
    private static readonly TimeZoneInfo Zone = FindZone();



    private static TimeZoneInfo FindZone()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("Europe/Skopje"); }
        catch { return TimeZoneInfo.FindSystemTimeZoneById("Central Europe Standard Time"); }
    }

    private static DateTime ToUtc(DateTime local) =>
        TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(local, DateTimeKind.Unspecified), Zone);

    public static DateTime ToLocal(DateTime utc) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), Zone);

   
    public static DateTime LocalDayStartUtc(DateTime localDate) => ToUtc(localDate.Date);


    public static DateTime Today() => ToLocal(DateTime.UtcNow).Date;

    public static DateTime TodayStartUtc() => LocalDayStartUtc(ToLocal(DateTime.UtcNow));

  
    public static DateTime CloseOfDayUtc(DateTime utc, TimeSpan closeTime) => ToUtc(ToLocal(utc).Date + closeTime);

   
    public static DateTime NextMidnightUtc(DateTime utc) => LocalDayStartUtc(ToLocal(utc).AddDays(1));
}
