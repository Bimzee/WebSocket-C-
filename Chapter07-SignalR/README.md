# 🔮 Chapter 07 — SignalR: The .NET Way

> **Goal**: Learn ASP.NET Core SignalR and compare it to raw WebSocket  
> **Concepts**: Hubs, typed methods, built-in groups, auto-reconnect, fallback transports

---

## 🎯 What You'll Learn

- What SignalR is and why it exists
- The Hub pattern — typed RPC-style communication
- Built-in groups, reconnection, and transport fallback
- SignalR JavaScript client library
- When to use SignalR vs raw WebSocket

---

## 📊 Raw WebSocket vs SignalR

```
┌──────────────────────────────┐    ┌──────────────────────────────┐
│      RAW WEBSOCKET           │    │         SIGNALR              │
│      (Chapters 2-6)          │    │      (This Chapter)          │
│                              │    │                              │
│  ❌ Manual byte[] buffers    │    │  ✅ Automatic serialization  │
│  ❌ Manual JSON parsing      │    │  ✅ Typed method invocation  │
│  ❌ Manual connection track  │    │  ✅ Built-in tracking        │
│  ❌ Manual group management  │    │  ✅ Built-in Groups API      │
│  ❌ Manual reconnection      │    │  ✅ withAutomaticReconnect() │
│  ❌ WebSocket only           │    │  ✅ WS → SSE → Long Polling │
│  ❌ Manual message routing   │    │  ✅ Hub method dispatch      │
│                              │    │                              │
│  ✅ Full protocol control    │    │  ❌ Less low-level control   │
│  ✅ Minimal overhead         │    │  ❌ SignalR protocol overhead│
│  ✅ Any language/platform    │    │  ❌ .NET-specific patterns   │
└──────────────────────────────┘    └──────────────────────────────┘
```

---

## 🔑 Key Concepts

### What is a Hub?

A **Hub** is a class that handles client connections and messages. Think of it as a **controller for WebSocket** — clients call methods on the hub, and the hub calls methods on clients.

```csharp
// Server: Define a method clients can call
public async Task SendMessage(string message)
{
    await Clients.All.SendAsync("ReceiveMessage", message);
}

// Client: Call the server method
connection.invoke("SendMessage", "Hello!");

// Client: Handle server calling a client method
connection.on("ReceiveMessage", (data) => { ... });
```

### SignalR Transport Fallback

SignalR automatically negotiates the best transport:

| Priority | Transport | Description |
|----------|----------|-------------|
| 1️⃣ | **WebSocket** | Best: full-duplex, low overhead |
| 2️⃣ | **Server-Sent Events** | Fallback: server → client only |
| 3️⃣ | **Long Polling** | Last resort: periodic HTTP requests |

### Built-in Auto-Reconnect

```javascript
// Raw WebSocket: 30+ lines of exponential backoff code (Chapter 5)
// SignalR: One line!
.withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
```

### Built-in Groups

```csharp
// Raw WebSocket: ConcurrentDictionary + HashSet + locks (Chapter 4)
// SignalR: One line!
await Groups.AddToGroupAsync(Context.ConnectionId, "myGroup");
await Clients.Group("myGroup").SendAsync("Message", data);
```

---

## 📁 Files in This Chapter

| File | Purpose |
|------|---------|
| `Program.cs` | Server setup — just `AddSignalR()` + `MapHub()` |
| `Hubs/ChatHub.cs` | Hub class with chat, groups, and DM methods |
| `wwwroot/index.html` | SignalR JS client with comparison banner |

---

## 🚀 How to Run

```bash
cd Chapter07-SignalR
dotnet run
```

Open multiple tabs to **<http://localhost:5000>** and chat!

### What to Try

1. **Send messages** — see typed method invocation (no JSON parsing!)
2. **Check transport** — click "Show Transport" to see WebSocket/SSE/Polling
3. **Compare code** — see the side-by-side comparison at the top
4. **Join groups** — same feature as Chapter 4, but with one line of code
5. **Kill the server** — watch automatic reconnection with retry delays

---

## 🧠 Key Takeaways

1. **SignalR abstracts WebSocket** — typed methods instead of raw bytes
2. **Transport fallback** — works even when WebSocket isn't available
3. **Built-in features** — groups, reconnection, serialization, all free
4. **Hub pattern** — clean separation of concerns, just like controllers
5. **Use SignalR for most .NET real-time apps** — use raw WebSocket only when you need full protocol control

---

## ⏭ Next Chapter

**[Chapter 08 — Scaling WebSockets with Redis →](../Chapter08-Scaling/)**

Next, we'll learn how to scale WebSocket connections across multiple server instances using Redis!
