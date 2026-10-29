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
        var closed = LocalTime.IsClosed(DateTime.UtcNow);
        var staleSessions = await context.EmployeeSessions
        .Where(s => s.BreakStartedAt == null && s.LogoutAt == null && (closed || s.LoginAt < today))
        .ToListAsync();
        var staleBreaks = await context.EmployeeSessions.Where(s => s.BreakStartedAt != null && s.BreakEndedAt == null && (closed || s.BreakStartedAt < today)).ToListAsync();
        if (staleSessions.Count == 0 && staleBreaks.Count == 0) return;
        foreach (var s in staleSessions)
            s.LogoutAt = LocalTime.SessionEndUtc(s.LoginAt);
        foreach (var b in staleBreaks)
            b.BreakEndedAt = LocalTime.SessionEndUtc(b.BreakStartedAt!.Value);

        var userIds = staleSessions.Select(s => s.UserId)
        .Concat(staleBreaks.Select(b => b.UserId))
        .Distinct().ToList();

        var onBreakUsers = await context.Users.Where(u => userIds.Contains(u.Id) && u.IsOnBreak).ToListAsync();
        foreach (var u in onBreakUsers) 
            u.IsOnBreak = false;
        await context.SaveChangesAsync();
        foreach (var s in staleSessions)
            await logger.LogAsync(LogActions.EmployeeLogout,"Employee",s.UserId,
            $"counter: {s.CounterName ?? "none"}; auto logout at {LocalTime.CloseTimeText}",actorId:s.UserId,actorName:"system");
        if(onBreakUsers.Count>0)
            await hub.Clients.All.SendAsync("CountersUpdated",await counterRepository.GetActiveCountersAsync());


    }

}