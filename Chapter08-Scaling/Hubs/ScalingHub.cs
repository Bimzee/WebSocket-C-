using Microsoft.AspNetCore.SignalR;

namespace Chapter08_Scaling.Hubs;

public class ScalingHub : Hub
{
    private static int _connectionCount = 0;

    public override async Task OnConnectedAsync()
    {
        Interlocked.Increment(ref _connectionCount);
        var port =
            Environment
                .GetCommandLineArgs()
                .FirstOrDefault(a => a.StartsWith("--port="))
                ?.Split("=")[1]
            ?? "5000";

        Console.WriteLine(
            $"✅ Client connected on port {port}. Local connections: {_connectionCount}"
        );

        await Clients.Caller.SendAsync(
            "ServerInfo",
            new
            {
                ServerPort = port,
                ServerId = Environment.MachineName,
                ProcessId = Environment.ProcessId,
                Message = $"Connected to server on port {port}",
            }
        );

        await Clients.All.SendAsync(
            "SystemMessage",
            new { Message = $"A user connected via server :{port}", ServerPort = port }
        );

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        Interlocked.Decrement(ref _connectionCount);
        await Clients.All.SendAsync("SystemMessage", new { Message = "A user disconnected" });
        await base.OnDisconnectedAsync(exception);
    }

    public async Task SendMessage(string message)
    {
        var port =
            Environment
                .GetCommandLineArgs()
                .FirstOrDefault(a => a.StartsWith("--port="))
                ?.Split("=")[1]
            ?? "5000";

        // This message will be delivered to ALL clients across ALL servers
        // thanks to the Redis backplane!
        await Clients.All.SendAsync(
            "ReceiveMessage",
            new
            {
                Text = message,
                ServerPort = port,
                Timestamp = DateTime.UtcNow,
            }
        );
    }

    public async Task SendToGroup(string groupName, string message)
    {
        var port =
            Environment
                .GetCommandLineArgs()
                .FirstOrDefault(a => a.StartsWith("--port="))
                ?.Split("=")[1]
            ?? "5000";

        await Clients
            .Group(groupName)
            .SendAsync(
                "ReceiveGroupMessage",
                new
                {
                    Group = groupName,
                    Text = message,
                    ServerPort = port,
                    Timestamp = DateTime.UtcNow,
                }
            );
    }

    public async Task JoinGroup(string groupName)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        await Clients.Caller.SendAsync(
            "SystemMessage",
            new { Message = $"Joined group '{groupName}'" }
        );
    }

    public async Task LeaveGroup(string groupName)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        await Clients.Caller.SendAsync(
            "SystemMessage",
            new { Message = $"Left group '{groupName}'" }
        );
    }
}
