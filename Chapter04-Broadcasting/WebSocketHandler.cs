// =============================================================================
// Chapter 04 — WebSocket Handler
// =============================================================================
// Manages WebSocket connections, groups, and message routing.
// Demonstrates:
//   1. Thread-safe connection tracking with ConcurrentDictionary
//   2. Broadcasting to all connected clients
//   3. Group (room) management — join, leave, message
//   4. Targeted (direct) messaging between clients
// =============================================================================

using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace Chapter04_Broadcasting;

public class WebSocketHandler
{
    // =========================================================================
    // Connection Storage
    // =========================================================================
    // ConcurrentDictionary is thread-safe — multiple WebSocket handlers
    // can add/remove connections simultaneously without locks.

    private readonly ConcurrentDictionary<string, WebSocket> _connections = new();
    private readonly ConcurrentDictionary<string, HashSet<string>> _groups = new();
    private readonly ConcurrentDictionary<string, string> _clientGroups = new(); // clientId -> current group
    private readonly ConcurrentDictionary<string, string> _clientNames = new(); // clientId -> display name

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    // =========================================================================
    // Connection Management
    // =========================================================================

    public async Task HandleConnection(WebSocket webSocket, string clientId)
    {
        // Register the connection
        _connections.TryAdd(clientId, webSocket);
        _clientNames.TryAdd(clientId, $"User-{clientId}");

        Console.WriteLine($"✅ Client {clientId} connected. Total: {_connections.Count}");

        // Send welcome + client list
        await SendToClient(
            clientId,
            new
            {
                Type = "system",
                Event = "welcome",
                ClientId = clientId,
                Name = _clientNames[clientId],
                Message = $"Welcome! You are {_clientNames[clientId]}. {_connections.Count} user(s) online.",
                OnlineUsers = _connections.Keys.Select(id => new
                {
                    Id = id,
                    Name = _clientNames.GetValueOrDefault(id, id),
                }),
                AvailableGroups = _groups.Keys.ToList(),
            }
        );

        // Notify everyone else
        await BroadcastExcept(
            clientId,
            new
            {
                Type = "system",
                Event = "user_joined",
                ClientId = clientId,
                Name = _clientNames[clientId],
                Message = $"{_clientNames[clientId]} joined the server",
                OnlineCount = _connections.Count,
            }
        );

        // Receive loop
        var buffer = new byte[1024 * 4];
        try
        {
            var result = await webSocket.ReceiveAsync(
                new ArraySegment<byte>(buffer),
                CancellationToken.None
            );
            while (!result.CloseStatus.HasValue)
            {
                var raw = Encoding.UTF8.GetString(buffer, 0, result.Count);
                await ProcessMessage(clientId, raw);
                result = await webSocket.ReceiveAsync(
                    new ArraySegment<byte>(buffer),
                    CancellationToken.None
                );
            }
            await webSocket.CloseAsync(
                result.CloseStatus.Value,
                result.CloseStatusDescription,
                CancellationToken.None
            );
        }
        catch (WebSocketException)
        { /* Client disconnected abruptly */
        }
        finally
        {
            await RemoveClient(clientId);
        }
    }

    private async Task RemoveClient(string clientId)
    {
        _connections.TryRemove(clientId, out _);

        // Remove from current group
        if (_clientGroups.TryRemove(clientId, out var group))
        {
            if (_groups.TryGetValue(group, out var members))
            {
                lock (members)
                {
                    members.Remove(clientId);
                }
                await SendToGroup(
                    group,
                    new
                    {
                        Type = "system",
                        Event = "user_left_group",
                        ClientId = clientId,
                        Name = _clientNames.GetValueOrDefault(clientId, clientId),
                        Group = group,
                    }
                );
                // Clean up empty groups
                if (members.Count == 0)
                    _groups.TryRemove(group, out _);
            }
        }

        _clientNames.TryRemove(clientId, out _);
        Console.WriteLine($"👋 Client {clientId} disconnected. Total: {_connections.Count}");

        await BroadcastAll(
            new
            {
                Type = "system",
                Event = "user_left",
                ClientId = clientId,
                OnlineCount = _connections.Count,
            }
        );
    }

    // =========================================================================
    // Message Processing
    // =========================================================================

    private async Task ProcessMessage(string clientId, string raw)
    {
        try
        {
            using var doc = JsonDocument.Parse(raw);
            var root = doc.RootElement;
            var type = root.GetProperty("type").GetString() ?? "";

            switch (type)
            {
                case "set_name":
                    var newName = root.GetProperty("name").GetString() ?? clientId;
                    var oldName = _clientNames.GetValueOrDefault(clientId, clientId);
                    _clientNames[clientId] = newName;
                    await BroadcastAll(
                        new
                        {
                            Type = "system",
                            Event = "name_changed",
                            ClientId = clientId,
                            OldName = oldName,
                            NewName = newName,
                        }
                    );
                    break;

                case "broadcast":
                    var broadcastText = root.GetProperty("text").GetString() ?? "";
                    await BroadcastAll(
                        new
                        {
                            Type = "broadcast",
                            From = clientId,
                            Name = _clientNames.GetValueOrDefault(clientId, clientId),
                            Text = broadcastText,
                            Timestamp = DateTime.UtcNow,
                        }
                    );
                    break;

                case "join_group":
                    var groupName = root.GetProperty("group").GetString() ?? "";
                    await JoinGroup(clientId, groupName);
                    break;

                case "leave_group":
                    await LeaveGroup(clientId);
                    break;

                case "group_message":
                    var msgText = root.GetProperty("text").GetString() ?? "";
                    if (_clientGroups.TryGetValue(clientId, out var currentGroup))
                    {
                        await SendToGroup(
                            currentGroup,
                            new
                            {
                                Type = "group_message",
                                From = clientId,
                                Name = _clientNames.GetValueOrDefault(clientId, clientId),
                                Group = currentGroup,
                                Text = msgText,
                                Timestamp = DateTime.UtcNow,
                            }
                        );
                    }
                    else
                    {
                        await SendToClient(
                            clientId,
                            new
                            {
                                Type = "error",
                                Message = "You're not in a group. Join one first!",
                            }
                        );
                    }
                    break;

                case "direct_message":
                    var targetId = root.GetProperty("to").GetString() ?? "";
                    var dmText = root.GetProperty("text").GetString() ?? "";
                    await SendDirectMessage(clientId, targetId, dmText);
                    break;

                case "list_users":
                    await SendToClient(
                        clientId,
                        new
                        {
                            Type = "user_list",
                            Users = _connections.Keys.Select(id => new
                            {
                                Id = id,
                                Name = _clientNames.GetValueOrDefault(id, id),
                                Group = _clientGroups.GetValueOrDefault(id, "none"),
                            }),
                        }
                    );
                    break;

                case "list_groups":
                    await SendToClient(
                        clientId,
                        new
                        {
                            Type = "group_list",
                            Groups = _groups.Select(g => new
                            {
                                Name = g.Key,
                                MemberCount = g.Value.Count,
                            }),
                        }
                    );
                    break;
            }
        }
        catch (Exception ex)
        {
            await SendToClient(
                clientId,
                new { Type = "error", Message = $"Invalid message: {ex.Message}" }
            );
        }
    }

    // =========================================================================
    // Group Management
    // =========================================================================

    private async Task JoinGroup(string clientId, string groupName)
    {
        // Leave current group first
        await LeaveGroup(clientId, silent: true);

        // Create group if it doesn't exist
        var members = _groups.GetOrAdd(groupName, _ => new HashSet<string>());
        lock (members)
        {
            members.Add(clientId);
        }
        _clientGroups[clientId] = groupName;

        // Notify the client
        await SendToClient(
            clientId,
            new
            {
                Type = "system",
                Event = "joined_group",
                Group = groupName,
                Members = members.Select(id => new
                {
                    Id = id,
                    Name = _clientNames.GetValueOrDefault(id, id),
                }),
                Message = $"You joined group '{groupName}'",
            }
        );

        // Notify group members
        await SendToGroupExcept(
            groupName,
            clientId,
            new
            {
                Type = "system",
                Event = "user_joined_group",
                ClientId = clientId,
                Name = _clientNames.GetValueOrDefault(clientId, clientId),
                Group = groupName,
            }
        );
    }

    private async Task LeaveGroup(string clientId, bool silent = false)
    {
        if (_clientGroups.TryRemove(clientId, out var group))
        {
            if (_groups.TryGetValue(group, out var members))
            {
                lock (members)
                {
                    members.Remove(clientId);
                }
                if (members.Count == 0)
                    _groups.TryRemove(group, out _);

                if (!silent)
                {
                    await SendToClient(
                        clientId,
                        new
                        {
                            Type = "system",
                            Event = "left_group",
                            Group = group,
                            Message = $"You left group '{group}'",
                        }
                    );
                    await SendToGroup(
                        group,
                        new
                        {
                            Type = "system",
                            Event = "user_left_group",
                            ClientId = clientId,
                            Name = _clientNames.GetValueOrDefault(clientId, clientId),
                            Group = group,
                        }
                    );
                }
            }
        }
    }

    // =========================================================================
    // Sending Messages
    // =========================================================================

    private async Task SendToClient(string clientId, object message)
    {
        if (_connections.TryGetValue(clientId, out var ws) && ws.State == WebSocketState.Open)
        {
            var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message, _jsonOptions));
            await ws.SendAsync(
                new ArraySegment<byte>(bytes),
                WebSocketMessageType.Text,
                true,
                CancellationToken.None
            );
        }
    }

    private async Task BroadcastAll(object message)
    {
        var tasks = _connections
            .Where(c => c.Value.State == WebSocketState.Open)
            .Select(c => SendToClient(c.Key, message));
        await Task.WhenAll(tasks);
    }

    private async Task BroadcastExcept(string excludeClientId, object message)
    {
        var tasks = _connections
            .Where(c => c.Key != excludeClientId && c.Value.State == WebSocketState.Open)
            .Select(c => SendToClient(c.Key, message));
        await Task.WhenAll(tasks);
    }

    private async Task SendToGroup(string groupName, object message)
    {
        if (_groups.TryGetValue(groupName, out var members))
        {
            List<string> memberList;
            lock (members)
            {
                memberList = members.ToList();
            }
            var tasks = memberList.Select(id => SendToClient(id, message));
            await Task.WhenAll(tasks);
        }
    }

    private async Task SendToGroupExcept(string groupName, string excludeClientId, object message)
    {
        if (_groups.TryGetValue(groupName, out var members))
        {
            List<string> memberList;
            lock (members)
            {
                memberList = members.Where(id => id != excludeClientId).ToList();
            }
            var tasks = memberList.Select(id => SendToClient(id, message));
            await Task.WhenAll(tasks);
        }
    }

    private async Task SendDirectMessage(string fromId, string toId, string text)
    {
        if (!_connections.ContainsKey(toId))
        {
            await SendToClient(
                fromId,
                new { Type = "error", Message = $"User '{toId}' not found" }
            );
            return;
        }

        var dm = new
        {
            Type = "direct_message",
            From = fromId,
            Name = _clientNames.GetValueOrDefault(fromId, fromId),
            To = toId,
            Text = text,
            Timestamp = DateTime.UtcNow,
        };

        await SendToClient(toId, dm);
        await SendToClient(fromId, dm); // Echo back to sender
    }
}
