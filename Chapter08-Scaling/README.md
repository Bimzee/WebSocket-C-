# 📈 Chapter 08 — Scaling WebSockets with Redis

> **Goal**: Scale WebSocket servers horizontally using Redis as a message backplane  
> **Concepts**: Horizontal scaling, Redis pub/sub, SignalR Redis backplane, load balancing

---

## 🎯 What You'll Learn

- Why WebSocket servers can't scale with a simple load balancer
- How Redis pub/sub solves the cross-server messaging problem
- Configuring SignalR with `AddStackExchangeRedis()`
- Running multiple server instances
- The "sticky sessions" concept

---

## 📊 The Scaling Problem

```
WITHOUT Redis Backplane:
─────────────────────────────────────────────────
Client A ──→ Server 1 (knows about A only)
Client B ──→ Server 2 (knows about B only)

Client A sends "Hello!" → Server 1 broadcasts...
→ Only Client A receives it! ❌ Client B misses it!

WITH Redis Backplane:
─────────────────────────────────────────────────
Client A ──→ Server 1 ──→ Redis ──→ Server 2 ──→ Client B ✅
                                ──→ Server 1 ──→ Client A ✅
```

## 🔑 How Redis Backplane Works

```
┌──────────┐         ┌──────────────┐         ┌──────────┐
│ Server 1 │───pub──►│              │◄──sub────│ Server 2 │
│ (Client  │         │    REDIS     │         │ (Client  │
│  A, C)   │◄──sub──│   Pub/Sub    │───pub──►│  B, D)   │
└──────────┘         └──────────────┘         └──────────┘
                            │
                      ┌─────┴────┐
                      │ Server 3 │
                      │ (Client  │
                      │  E, F)   │
                      └──────────┘
```

1. Client A sends message to Server 1
2. Server 1 **publishes** message to Redis
3. Redis **distributes** to all subscribed servers
4. Each server delivers to its local clients

---

## 📁 Files in This Chapter

| File | Purpose |
|------|---------|
| `Program.cs` | Server with SignalR + Redis backplane |
| `Hubs/ScalingHub.cs` | Hub that tags messages with server port |
| `docker-compose.yml` | Redis container |
| `wwwroot/index.html` | Multi-server client with server selector |

---

## 🚀 How to Run

### 1. Start Redis

```bash
cd Chapter08-Scaling
docker-compose up -d
```

### 2. Start Multiple Server Instances (in separate terminals)

```bash
# Terminal 1
dotnet run -- --port=5000

# Terminal 2
dotnet run -- --port=5001

# Terminal 3 (optional)
dotnet run -- --port=5002
```

### 3. Open the Client

Open **<http://localhost:5000>** in your browser. Click different server cards to connect to different instances.

### 4. Test Cross-Server Messaging

1. Open **Tab 1** → connect to Server :5000
2. Open **Tab 2** → connect to Server :5001
3. Send a message from Tab 1 → It appears in Tab 2! 🎉 (via Redis)
4. Notice the server tag showing which server processed each message

---

## 📝 The Magic Line

```csharp
builder.Services.AddSignalR()
    .AddStackExchangeRedis("localhost:6379");
```

That's it! One line to enable cross-server messaging. SignalR's Redis backplane automatically:

- Publishes all `Clients.All.SendAsync()` calls to Redis
- Subscribes to Redis for messages from other servers
- Handles groups across servers

---

## 🧠 Key Takeaways

1. **A single WebSocket server only knows its own clients** — scaling requires a message bus
2. **Redis pub/sub** is a lightweight, fast message broker perfect for this
3. **`AddStackExchangeRedis()`** — one line to enable cross-server SignalR
4. **Sticky sessions** may be needed for WebSocket transport (load balancer must route reconnections to the same server)
5. **Groups work across servers** — Redis handles the distributed membership

---

## ⏭ Capstone Project

**[Capstone — Real-Time Financial Dashboard →](../Capstone-LiveDashboard/)**

Apply everything you've learned in a real-world project!
