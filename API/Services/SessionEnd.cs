using API.Data;
using API.Entities;
using API.Extensions;
using Microsoft.EntityFrameworkCore;

namespace API.Services;

public static class SessionEnd
{
    public static readonly TimeSpan Grace = TimeSpan.FromMinutes(5);

 
    public static async Task<DateTime> ForOpenSessionAsync(AppDbContext context, EmployeeSession s, DateTime now)
    {
        if (now < LocalTime.CloseOfDayUtc(s.LoginAt)) return now;
        if (s.CounterId == null) return s.LoginAt;

   
        var nextLogin = await context.EmployeeSessions
            .Where(x => x.BreakStartedAt == null && x.CounterId == s.CounterId && x.LoginAt > s.LoginAt)
            .OrderBy(x => x.LoginAt)
            .Select(x => (DateTime?)x.LoginAt)
            .FirstOrDefaultAsync();
        var limit = nextLogin ?? LocalTime.NextMidnightUtc(s.LoginAt);

        var lastCall = await context.Tickets
            .Where(t => t.CounterId == s.CounterId && t.StartedAt != null
                        && t.StartedAt >= s.LoginAt && t.StartedAt < limit)
            .MaxAsync(t => t.StartedAt);

        var end = lastCall == null ? s.LoginAt : lastCall.Value + Grace;
        return end > now ? now : end;
    }
}
