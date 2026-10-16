using Microsoft.AspNetCore.SignalR;

namespace API.SignalR;

public class QueueHub() : Hub
{
    public override async Task OnConnectedAsync()
    {
        Console.WriteLine($"SignalR client connected: {Context.ConnectionId}");
        await base.OnConnectedAsync();
    }
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        Console.WriteLine($"SignalR client disconnected: {Context.ConnectionId}");

        await base.OnDisconnectedAsync(exception);

    }
}