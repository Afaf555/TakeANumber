namespace API.Services;

public record TicketSlice(DateTime CreatedAt, DateTime StartedAt, DateTime? FinishedAt);

public record PhaseResult(double Serving, double Idle, double Neglect, double Break, double AfterHours)
{
    public PhaseResult Plus(PhaseResult o) =>
        new(Serving + o.Serving, Idle + o.Idle, Neglect + o.Neglect, Break + o.Break, AfterHours + o.AfterHours);
}

// Splits one login session into 4 phases (all values in minutes):
//   1 Serving  - ticket started -> finished
//   2 Idle     - free time with nobody waiting, plus the first `threshold` of reaction time
//                after a ticket was available
//   3 Neglect  - reaction time beyond `threshold` while a ticket was already waiting
//   4 Break    - on break
// AfterHours = time present (not on break) inside [afterStart, afterEnd)
public static class PhaseCalculator
{
    public static PhaseResult Compute(
        DateTime login, DateTime end,
        IEnumerable<(DateTime Start, DateTime End)> breaks,
        IEnumerable<TicketSlice> tickets,
        TimeSpan threshold,
        DateTime afterStart, DateTime afterEnd)
    {
        if (end <= login) return new PhaseResult(0, 0, 0, 0, 0);

        var ticketList = tickets.OrderBy(t => t.StartedAt).ToList();
        var serving = ticketList
            .Select(t => (Start: Max(t.StartedAt, login), End: Min(t.FinishedAt ?? end, end)))
            .Where(i => i.End > i.Start)
            .ToList();
        var brk = breaks
            .Select(b => (Start: Max(b.Start, login), End: Min(b.End, end)))
            .Where(i => i.End > i.Start)
            .ToList();

        var busy = Merge(serving.Concat(brk));

        double servingMin = Minutes(Merge(serving));
        double breakMin = Minutes(Merge(brk));
        double idle = 0, neglect = 0;

        var cursor = login;
        foreach (var b in busy.Append((Start: end, End: end)))
        {
            if (b.Start > cursor)
            {
                var a = cursor;
                var stop = b.Start;
                var next = ticketList.FirstOrDefault(t => t.StartedAt == stop);
                if (next != null)
                {
                    var available = Min(Max(next.CreatedAt, a), stop);
                    idle += (available - a).TotalMinutes;
                    var reaction = stop - available;
                    var ok = reaction < threshold ? reaction : threshold;
                    idle += ok.TotalMinutes;
                    neglect += (reaction - ok).TotalMinutes;
                }
                else idle += (stop - a).TotalMinutes;
            }
            cursor = Max(cursor, b.End);
        }

        var afterMin = Minutes(Intersect(new[] { (login, end) }, afterStart, afterEnd))
                       - Minutes(Intersect(Merge(brk), afterStart, afterEnd));

        return new PhaseResult(
            Math.Round(servingMin, 2), Math.Round(idle, 2), Math.Round(neglect, 2),
            Math.Round(breakMin, 2), Math.Round(Math.Max(0, afterMin), 2));
    }

    private static IEnumerable<(DateTime Start, DateTime End)> Intersect(
        IEnumerable<(DateTime Start, DateTime End)> items, DateTime a, DateTime b) =>
        items.Select(i => (Start: Max(i.Start, a), End: Min(i.End, b))).Where(i => i.End > i.Start);

    private static List<(DateTime Start, DateTime End)> Merge(IEnumerable<(DateTime Start, DateTime End)> items)
    {
        var result = new List<(DateTime Start, DateTime End)>();
        foreach (var i in items.OrderBy(x => x.Start))
        {
            if (result.Count > 0 && i.Start <= result[^1].End)
                result[^1] = (result[^1].Start, Max(result[^1].End, i.End));
            else result.Add(i);
        }
        return result;
    }

    private static double Minutes(IEnumerable<(DateTime Start, DateTime End)> items) =>
        items.Sum(i => (i.End - i.Start).TotalMinutes);

    private static DateTime Max(DateTime a, DateTime b) => a > b ? a : b;
    private static DateTime Min(DateTime a, DateTime b) => a < b ? a : b;
}