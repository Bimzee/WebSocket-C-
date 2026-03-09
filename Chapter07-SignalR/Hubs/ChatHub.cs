// =============================================================================
// Chapter 07 — SignalR Hub
// =============================================================================
// The Hub is the core abstraction in SignalR. It's a high-level class that
// handles connections, groups, and method invocation automatically.
//
// Compare this to Chapter 04's WebSocketHandler.cs:
//   - No manual byte[] buffers
//   - No JSON serialization code
//   - No connection tracking dictionaries
//   - No message routing switch statements
//   - SignalR handles ALL of this for you!
// =============================================================================

using Microsoft.AspNetCore.SignalR;

namespace Chapter07_SignalR.Hubs;

public class ChatHub : Hub
{
    // Static dictionary to track user names (in production, use a service)
    private static readonly Dictionary<string, string> _userNames = new();

    // =========================================================================
    // LIFECYCLE: Called when a client connects
    // =========================================================================
    public override async Task OnConnectedAsync()
    {
        var connectionId = Context.ConnectionId;
        var shortId = connectionId[..8];
        _userNames[connectionId] = $"User-{shortId}";

        Console.WriteLine($"✅ SignalR client connected: {shortId}");

        // Send welcome to the connecting client
        await Clients.Caller.SendAsync(
            "SystemMessage",
            new
            {
                Event = "welcome",
                ConnectionId = shortId,
                Name = _userNames[connectionId],
                Message = $"Welcome to SignalR! Your ID: {shortId}",
            }
        );

        // Notify all OTHER clients
        await Clients.Others.SendAsync(
            "SystemMessage",
            new
            {
                Event = "user_joined",
                Name = _userNames[connectionId],
                Message = $"{_userNames[connectionId]} joined the chat",
            }
        );

        await base.OnConnectedAsync();
    }

    // =========================================================================
    // LIFECYCLE: Called when a client disconnects
    // =========================================================================
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var name = _userNames.GetValueOrDefault(Context.ConnectionId, "Unknown");
        _userNames.Remove(Context.ConnectionId);

        Console.WriteLine($"👋 SignalR client disconnected: {name}");

        await Clients.All.SendAsync(
            "SystemMessage",
            new
            {
                Event = "user_left",
                Name = name,
                Message = $"{name} left the chat",
            }
        );

        await base.OnDisconnectedAsync(exception);
    }

    // =========================================================================
    // HUB METHOD: Send a chat message to ALL clients
    // =========================================================================
    // In SignalR, the client calls: connection.invoke("SendMessage", "Hello!")
    // The hub method receives it and forwards to all clients.
    public async Task SendMessage(string message)
    {
        var name = _userNames.GetValueOrDefault(Context.ConnectionId, "Unknown");
        Console.WriteLine($"💬 [{name}] {message}");

        // Clients.All sends to EVERY connected client (including sender)
        await Clients.All.SendAsync(
            "ReceiveMessage",
            new
            {
                From = name,
                Text = message,
                Timestamp = DateTime.UtcNow,
            }
        );
    }

    // =========================================================================
    // HUB METHOD: Set display name
    // =========================================================================
    public async Task SetName(string newName)
    {
        var oldName = _userNames.GetValueOrDefault(Context.ConnectionId, "Unknown");
        _userNames[Context.ConnectionId] = newName;

        await Clients.All.SendAsync(
            "SystemMessage",
            new
            {
                Event = "name_changed",
                OldName = oldName,
                NewName = newName,
                Message = $"{oldName} is now {newName}",
            }
        );
    }

    // =========================================================================
    // HUB METHOD: Join a group
    // =========================================================================
    // SignalR groups are built-in! No ConcurrentDictionary needed.
    public async Task JoinGroup(string groupName)
    {
        // One line! SignalR manages the group membership for you.
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

        var name = _userNames.GetValueOrDefault(Context.ConnectionId, "Unknown");

        await Clients.Caller.SendAsync(
            "SystemMessage",
            new
            {
                Event = "joined_group",
                Group = groupName,
                Message = $"You joined '{groupName}'",
            }
        );

        // Notify the group
        await Clients
            .Group(groupName)
            .SendAsync(
                "SystemMessage",
                new
                {
                    Event = "user_joined_group",
                    Name = name,
                    Group = groupName,
                    Message = $"{name} joined '{groupName}'",
                }
            );
    }

    // =========================================================================
    // HUB METHOD: Leave a group
    // =========================================================================
    public async Task LeaveGroup(string groupName)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);

        var name = _userNames.GetValueOrDefault(Context.ConnectionId, "Unknown");

        await Clients.Caller.SendAsync(
            "SystemMessage",
            new
            {
                Event = "left_group",
                Group = groupName,
                Message = $"You left '{groupName}'",
            }
        );

        await Clients
            .Group(groupName)
            .SendAsync(
                "SystemMessage",
                new
                {
                    Event = "user_left_group",
                    Name = name,
                    Group = groupName,
                    Message = $"{name} left '{groupName}'",
                }
            );
    }

    // =========================================================================
    // HUB METHOD: Send message to a group
    // =========================================================================
    public async Task SendGroupMessage(string groupName, string message)
    {
        var name = _userNames.GetValueOrDefault(Context.ConnectionId, "Unknown");

        await Clients
            .Group(groupName)
            .SendAsync(
                "ReceiveGroupMessage",
                new
                {
                    From = name,
                    Group = groupName,
                    Text = message,
                    Timestamp = DateTime.UtcNow,
                }
            );
    }

    // =========================================================================
    // HUB METHOD: Send a direct message to a specific connection
    // =========================================================================
    public async Task DirectMessage(string targetConnectionId, string message)
    {
        var senderName = _userNames.GetValueOrDefault(Context.ConnectionId, "Unknown");

        // Find full connection ID by prefix
        var target = _userNames.Keys.FirstOrDefault(k => k.StartsWith(targetConnectionId));
        if (target == null)
        {
            await Clients.Caller.SendAsync(
                "SystemMessage",
                new { Event = "error", Message = $"User '{targetConnectionId}' not found" }
            );
            return;
        }

        var dm = new
        {
            From = senderName,
            To = _userNames.GetValueOrDefault(target, "Unknown"),
            Text = message,
            Timestamp = DateTime.UtcNow,
        };

        await Clients.Client(target).SendAsync("ReceiveDirectMessage", dm);
        await Clients.Caller.SendAsync("ReceiveDirectMessage", dm);
    }
}
