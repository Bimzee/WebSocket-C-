# 💓 Chapter 05 — Heartbeat & Reconnection

> **Goal**: Keep connections alive and recover gracefully from failures  
> **Concepts**: Ping/pong, IHostedService, exponential backoff, connection state machine

---

## 🎯 What You'll Learn

- Server-initiated heartbeat (ping/pong) to detect dead connections
- Using `BackgroundService` (IHostedService) for periodic tasks
- Dead connection cleanup based on missed pings
- Client-side auto-reconnection with exponential backoff and jitter
- Connection state machine (Connected → Disconnected → Reconnecting)

---

## 📊 Heartbeat & Reconnection Flow

![Heartbeat & Reconnection](./images/heartbeat-reconnection.png)

---

## 🔑 Key Concepts

### Why Heartbeat?

WebSocket connections can silently die due to:

- Network interruptions (Wi-Fi drops, mobile network switches)
- Firewalls/proxies closing idle connections
- Server crashes
- Client browser tabs going to sleep

Without heartbeat, the server holds references to **dead connections** forever, wasting memory and causing send failures.

### Two Levels of Ping/Pong

| Level | Mechanism | Who Controls It |
|-------|----------|----------------|
| **WebSocket Protocol** | Built-in ping/pong frames (opcode 0x9/0xA) | Framework (`KeepAliveInterval`) |
| **Application Level** | Custom JSON `{"type":"ping"}` / `{"type":"pong"}` | Your code |

Our chapter implements **both**: the ASP.NET Core `KeepAliveInterval` for protocol-level pings, and custom application-level pings for visibility and tracking.

### Exponential Backoff Formula

```
delay = min(baseDelay × 2^attempt + random_jitter, maxDelay)
```

| Attempt | Base Delay | Calculated | With Max Cap |
|---------|-----------|-----------|-------------|
| 1 | 1s × 2¹ | 2s + jitter | 2.3s |
| 2 | 1s × 2² | 4s + jitter | 4.7s |
| 3 | 1s × 2³ | 8s + jitter | 8.1s |
| 4 | 1s × 2⁴ | 16s + jitter | 16.5s |
| 5 | 1s × 2⁵ | 32s + jitter | **30s** (capped) |

**Jitter** adds randomness to prevent all clients from reconnecting at the exact same time (**thundering herd** problem).

---

## 📁 Files in This Chapter

| File | Purpose |
|------|---------|
| `Program.cs` | Server with heartbeat service, connection manager, and health tracking |
| `wwwroot/index.html` | Client with auto-reconnect, state machine UI, and health dashboard |

---

## 🚀 How to Run

```bash
cd Chapter05-HeartbeatReconnection
dotnet run
```

Open **<http://localhost:5000>** and watch the heartbeat in action!

### What to Try

1. **Watch pings** — Server sends ping every 10s, client auto-responds with pong
2. **Simulate network drop** — Click "💥 Simulate Drop" to test reconnection
3. **Observe backoff** — Each reconnect attempt waits longer
4. **Check server status** — Click "📊 Server Status" to see all connection health
5. **Visit health endpoint** — Open `http://localhost:5000/api/health` in a new tab

---

## 📝 Code Walkthrough

### BackgroundService for Heartbeat

```csharp
public class HeartbeatService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            await _connManager.SendPingToAll();
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            await _connManager.CleanupDeadConnections(maxMissedPings: 3);
        }
    }
}
```

`BackgroundService` runs a loop in the background alongside your web server — perfect for periodic tasks.

### Dead Connection Cleanup

```csharp
public async Task CleanupDeadConnections(int maxMissedPings = 3)
{
    var dead = _connections
        .Where(c => c.Value.MissedPings >= maxMissedPings)
        .ToList();
    // Close and remove dead connections
}
```

After 3 missed pings (30 seconds without response), the connection is presumed dead and removed.

---

## 🧠 Key Takeaways

1. **Heartbeat detects dead connections** that TCP can't detect immediately
2. **`BackgroundService`** is the .NET way to run periodic background tasks
3. **Exponential backoff** prevents reconnection storms
4. **Jitter** prevents the thundering herd problem
5. **Application-level ping** gives you visibility; protocol-level ping is transparent

---

## ⏭ Next Chapter

**[Chapter 06 — Authentication & Security →](../Chapter06-AuthSecurity/)**

Next, we'll secure our WebSocket connections with JWT authentication!
