using System.Security.Claims;
using API.Data;
using API.Entities;
using API.SignalR;
using Microsoft.AspNetCore.SignalR;

namespace API.Services;

public class ActivityLogger(AppDbContext context, IHttpContextAccessor accessor, IHubContext<QueueHub> hub)
{
    public async Task LogAsync(string action, string entityType, int? entityId = null,
        string? details = null, int? actorId = null, string? actorName = null)
    {
        var user = accessor.HttpContext?.User;
        if (actorId == null && user?.Identity?.IsAuthenticated == true)
        {
            if (int.TryParse(user.FindFirstValue("nameid"), out var id)) actorId = id;
            actorName ??= user.FindFirstValue("unique_name");
        }

        context.ActivityLogs.Add(new ActivityLog
        {
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Details = details,
            ActorId = actorId,
            ActorName = actorName
        });
        await context.SaveChangesAsync();
        await hub.Clients.All.SendAsync("DataChanged", action);
    }
}