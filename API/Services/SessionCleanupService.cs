using API.Extensions;
using API.Data;
using API.Entities;
using API.Interfaces;
using API.SignalR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace API.Services;

public class SessionCleanupService(IServiceScopeFactory scopeFactory, ILogger<SessionCleanupService> log) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try { await CloseStaleSessionsAsync(); }
            catch (Exception ex) { log.LogError(ex, "Session cleanup failed"); }
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
    private async Task CloseStaleSessionsAsync()
    {
        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ActivityLogger>();
        var hub = scope.ServiceProvider.GetRequiredService<IHubContext<QueueHub>>();
        var counterRepository = scope.ServiceProvider.GetRequiredService<ICounterRepository>();
        var today = LocalTime.TodayStartUtc();
        var now = DateTime.UtcNow;
        var staleSessions = await context.EmployeeSessions
        .Where(s => s.BreakStartedAt == null && s.LogoutAt == null && s.LoginAt < today)
        .ToListAsync();
        var staleBreaks = await context.EmployeeSessions.Where(s => s.BreakStartedAt != null && s.BreakEndedAt == null && s.BreakStartedAt < today).ToListAsync();
        if (staleSessions.Count == 0 && staleBreaks.Count == 0) return;
        foreach (var s in staleSessions)
            s.LogoutAt = await SessionEnd.ForOpenSessionAsync(context, s, now);
        await context.SaveChangesAsync();
        foreach (var b in staleBreaks)
        {
            var sessionEnd = await context.EmployeeSessions
                .Where(w => w.UserId == b.UserId && w.LoginAt == b.LoginAt && w.BreakStartedAt == null)
                .Select(w => w.LogoutAt)
                .FirstOrDefaultAsync();
            b.BreakEndedAt = sessionEnd != null && sessionEnd > b.BreakStartedAt ? sessionEnd : b.BreakStartedAt;
        }

        var userIds = staleSessions.Select(s => s.UserId)
        .Concat(staleBreaks.Select(b => b.UserId))
        .Distinct().ToList();

        var onBreakUsers = await context.Users.Where(u => userIds.Contains(u.Id) && u.IsOnBreak).ToListAsync();
        foreach (var u in onBreakUsers) 
            u.IsOnBreak = false;
        await context.SaveChangesAsync();
        foreach (var s in staleSessions)
            await logger.LogAsync(LogActions.EmployeeLogout,"Employee",s.UserId,
            $"counter: {s.CounterName ?? "none"}; auto logout (last ticket call + 5 min)",actorId:s.UserId,actorName:"system");
        if(onBreakUsers.Count>0)
            await hub.Clients.All.SendAsync("CountersUpdated",await counterRepository.GetActiveCountersAsync());


    }

}