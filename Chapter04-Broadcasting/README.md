# 📢 Chapter 04 — Broadcasting & Groups

> **Goal**: Manage multiple WebSocket clients with broadcasting, group messaging, and direct messaging  
> **Concepts**: ConcurrentDictionary, broadcast patterns, room/group management, targeted messaging

---

## 🎯 What You'll Learn

- Thread-safe connection tracking with `ConcurrentDictionary`
- Broadcasting messages to all connected clients
- Group (room) based messaging — join, leave, send
- Direct (private) messaging between specific clients
- Separating WebSocket logic into a handler class

---

## 📊 Broadcast vs Group Messaging

![Broadcast Topology](./images/broadcast-topology.png)

---

## 🏗 Architecture

```
                    ┌───────────────────────────────┐
                    │       WebSocketHandler         │
                    │                               │
                    │  _connections: ConcurrentDict  │──── All connected WebSockets
                    │  _groups: ConcurrentDict       │──── Group → Set<clientId>
                    │  _clientGroups: ConcurrentDict │──── clientId → current group
                    │  _clientNames: ConcurrentDict  │──── clientId → display name
                    │                               │
                    │  BroadcastAll()               │──── Send to everyone
                    │  SendToGroup()                │──── Send to group members
                    │  SendToClient()               │──── Send to one client
                    │  SendDirectMessage()          │──── Private messaging
                    └───────────────────────────────┘
```

---

## 📁 Files in This Chapter

| File | Purpose |
|------|---------|
| `Program.cs` | Server entry point — creates handler, accepts connections |
| `WebSocketHandler.cs` | Core logic — connection tracking, messaging, groups |
| `wwwroot/index.html` | Multi-tab client — broadcast, group, and DM interfaces |

---

## 🚀 How to Run

```bash
cd Chapter04-Broadcasting
dotnet run
```

Open **multiple browser tabs** to **<http://localhost:5000>** to see broadcasting in action!

### What to Try

1. **Open 3+ tabs** — each gets a unique user ID
2. **Set names** — give each tab a display name
3. **Broadcast** — send a message from one tab, see it appear in all tabs
4. **Join a group** — type a group name like "general" or "sports"
5. **Group message** — switch to Group tab, messages only go to group members
6. **Direct message** — click a user in the sidebar, switch to Direct tab, send a private message

---

## 📝 Code Walkthrough

### Thread-Safe Connection Tracking

```csharp
// ConcurrentDictionary is essential for WebSocket servers!
// Multiple connections are handled on different threads simultaneously.
private readonly ConcurrentDictionary<string, WebSocket> _connections = new();
```

> ⚠️ **Never use `Dictionary<>` for WebSocket connections!** Multiple threads add/remove connections concurrently. `ConcurrentDictionary` handles thread safety automatically.

### Broadcasting Pattern

```csharp
private async Task BroadcastAll(object message)
{
    // Use LINQ + Task.WhenAll for parallel sending
    var tasks = _connections
        .Where(c => c.Value.State == WebSocketState.Open)
        .Select(c => SendToClient(c.Key, message));
    await Task.WhenAll(tasks);
}
```

This sends the message to **all clients in parallel** using `Task.WhenAll`. It also filters out closed connections to avoid errors.

### Group Management

```csharp
// Groups are stored as a dictionary of group name → set of client IDs
private readonly ConcurrentDictionary<string, HashSet<string>> _groups = new();

// Join a group
var members = _groups.GetOrAdd(groupName, _ => new HashSet<string>());
lock (members) { members.Add(clientId); }
```

> 💡 While `ConcurrentDictionary` is thread-safe for its operations, the `HashSet<string>` values need explicit locking since `HashSet` itself is not thread-safe.

---

## 🧠 Key Takeaways

1. **`ConcurrentDictionary`** is essential for tracking WebSocket connections
2. **Broadcast** sends to everyone — use `Task.WhenAll` for parallel delivery
3. **Groups** partition clients — messages only go to group members
4. **Direct messaging** targets a specific client by ID
5. **Separate your WebSocket logic** into a handler class for maintainability

---

## ⏭ Next Chapter

**[Chapter 05 — Heartbeat & Reconnection →](../Chapter05-HeartbeatReconnection/)**

Next, we'll implement keep-alive mechanisms and automatic reconnection!
