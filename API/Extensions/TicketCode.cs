namespace API.Extensions;

public static class TicketCode
{
    public static string Format(string serviceCode, int number) => $"{serviceCode}-{number:000}";
}