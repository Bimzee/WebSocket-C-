# 🥋 Chapter 08 — Code Katas: Scaling with Redis

> **Type**: Hands-on coding exercises  
> **Goal**: Master horizontal scaling of WebSocket servers using Redis as a message backplane

---

## 🟢 Kata 1: Redis Pub/Sub Basics (Easy)

**Objective**: Understand Redis pub/sub before applying it to SignalR.

**Requirements:**

1. Start Redis using Docker: `docker run -d -p 6379:6379 redis`
2. Create a simple .NET console app with two modes:
   - **Publisher**: Reads from console and publishes to a Redis channel
   - **Subscriber**: Subscribes to the channel and prints received messages
3. Use `StackExchange.Redis` NuGet package
4. Run two instances — publish from one, subscribe from the other

**Code skeleton:**

```csharp
// Publisher
var redis = ConnectionMultiplexer.Connect("localhost:6379");
var sub = redis.GetSubscriber();
while (true)
{
    var message = Console.ReadLine();
    await sub.PublishAsync("chat", message);
}

// Subscriber
var redis = ConnectionMultiplexer.Connect("localhost:6379");
var sub = redis.GetSubscriber();
await sub.SubscribeAsync("chat", (channel, message) =>
{
    Console.WriteLine($"[{channel}] {message}");
});
```

---

## 🟢 Kata 2: Two-Server SignalR Setup (Easy)

**Objective**: Run two SignalR servers sharing messages through Redis.

**Requirements:**

1. Set up a single SignalR project with Redis backplane:

   ```csharp
   builder.Services.AddSignalR()
       .AddStackExchangeRedis("localhost:6379");
   ```

2. Run **two instances** on different ports: `--port=5000` and `--port=5001`
3. Open a browser tab connected to each server
4. Send a message from one — verify it appears on the other
5. Log which server each message originates from

**Verification checklist:**

- [ ] Tab 1 (port 5000) sends "Hello" → Tab 2 (port 5001) receives it
- [ ] Tab 2 sends "Hi back" → Tab 1 receives it
- [ ] Each message shows which server processed it

---

## 🟡 Kata 3: Cross-Server Groups (Medium)

**Objective**: Verify that SignalR groups work across server instances.

**Requirements:**

1. Implement group join/leave using SignalR's Groups API
2. Test this scenario:
   - Client A (Server 1) joins room "general"
   - Client B (Server 2) joins room "general"
   - Client A sends a message to "general"
   - **Verify**: Client B receives it (cross-server!)
3. Test another scenario:
   - Client C (Server 1) joins room "sports"
   - Client A sends to "general" → Client C should NOT receive it
4. Display the server port in each message to prove cross-server delivery

---

## 🟡 Kata 4: Server Health Dashboard (Medium)

**Objective**: Build a monitoring dashboard that shows all server instances.

**Requirements:**

1. Each server registers itself in Redis with a heartbeat:

   ```csharp
   // Store server info in Redis
   await redis.StringSetAsync($"server:{port}", JsonSerializer.Serialize(new {
       Port = port,
       StartedAt = DateTime.UtcNow,
       ConnectionCount = connections.Count
   }), TimeSpan.FromSeconds(30)); // TTL = 30s
   ```

2. Create a `/api/cluster` endpoint that reads all live servers from Redis
3. Build a dashboard page showing:
   - All active servers (port, uptime, connection count)
   - Total connections across the cluster
   - Which server the current client is connected to
4. Auto-refresh every 5 seconds
5. Highlight servers that haven't refreshed their heartbeat (potentially down)

<details>
<summary>💡 Hints</summary>

- Use Redis keys with a pattern `server:*` and scan with `KEYS` or `SCAN`
- Set a TTL on each server's key — if the server dies, the key expires
- Use `IConnectionMultiplexer` from DI to access Redis
- The dashboard can be a static HTML page that calls the API endpoint

</details>

---

## 🟡 Kata 5: Sticky Sessions Simulator (Medium)

**Objective**: Understand why sticky sessions matter and simulate the problem.

**Requirements:**

1. **Without sticky sessions**: Set up a simple round-robin between two servers
2. Demonstrate the problem:
   - Client connects to Server 1 via WebSocket
   - A reconnection attempt goes to Server 2 → **FAILS** (no connection state)
3. **With sticky sessions**: Route based on a session cookie
4. Document the difference and create a comparison table:

| Aspect | Without Sticky Sessions | With Sticky Sessions |
|--------|------------------------|---------------------|
| Reconnection | ? | ? |
| Load distribution | ? | ? |
| Server failure handling | ? | ? |

<details>
<summary>💡 Hints</summary>

- You can simulate a "load balancer" using a simple ASP.NET reverse proxy
- Or just manually switch the port in the client URL to simulate
- The key insight: WebSocket state lives on one server, reconnection must go back there
- Document: what does sticky sessions mean for server failover?

</details>

---

## 🔴 Kata 6: Graceful Server Shutdown (Hard)

**Objective**: Implement zero-downtime deployment with graceful shutdown.

**Requirements:**

1. When a server receives a shutdown signal (`SIGTERM`/`Ctrl+C`):
   - Stop accepting NEW connections
   - Notify all connected clients: `{ "type": "server_shutting_down", "reconnectTo": "other-server-url" }`
   - Wait up to **30 seconds** for clients to reconnect elsewhere
   - Close remaining connections gracefully
   - Deregister from Redis
2. Clients should:
   - Receive the shutdown warning
   - Immediately reconnect to a different server
   - Resume their group memberships on the new server
3. Test the flow:
   - Start Server A and B with Redis
   - Connect clients to Server A
   - Shut down Server A → verify clients automatically move to Server B

<details>
<summary>💡 Hints</summary>

- Use `IHostApplicationLifetime.ApplicationStopping` to hook into shutdown
- Store a list of available servers in Redis for the "reconnectTo" URL
- Set a `isShuttingDown` flag to reject new connections
- Use `CancellationToken` with a timeout for the grace period

</details>

---

## 🔴 Kata 7: Multi-Channel Chat with Redis Streams (Hard)

**Objective**: Build a scalable multi-channel chat using Redis Streams for message persistence.

**Requirements:**

1. Use **Redis Streams** (not just pub/sub) for message storage:
   - Each chat room maps to a Redis Stream: `stream:room:{roomName}`
   - Messages are persisted and can be replayed
2. When a new client joins a room:
   - Load the last **50 messages** from the Redis Stream
   - Send them as a "history" batch
3. New messages are:
   - Added to the Redis Stream (persistent)
   - Published via SignalR backplane (real-time)
4. Support cross-server group messaging (via Redis backplane)
5. Implement a `/api/rooms/{roomName}/history?count=50` REST endpoint
6. Add message TTL: Auto-expire messages older than 24 hours

**Redis Stream commands:**

```csharp
// Add to stream
await db.StreamAddAsync("stream:room:general", new NameValueEntry[] {
    new("user", userId),
    new("text", message),
    new("timestamp", DateTime.UtcNow.ToString("o"))
});

// Read last 50
var entries = await db.StreamRangeAsync("stream:room:general", count: 50, messageOrder: Order.Descending);
```

<details>
<summary>💡 Hints</summary>

- Use `StreamRangeAsync` with `"-"` and `"+"` for full range, or specify message IDs
- For TTL, use `StreamTrimAsync` with `MAXLEN` or a background job that trims old entries
- `XRANGE` and `XREVRANGE` are the underlying Redis commands
- Consider using consumer groups (`XREADGROUP`) for more advanced scenarios
- NuGet: `StackExchange.Redis` supports streams via `IDatabase.Stream*` methods

</details>
