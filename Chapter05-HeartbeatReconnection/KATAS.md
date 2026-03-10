# 🥋 Chapter 05 — Code Katas: Heartbeat & Reconnection

> **Type**: Hands-on coding exercises  
> **Goal**: Master keep-alive mechanisms, dead connection detection, and automatic reconnection

---

## 🟢 Kata 1: Simple Ping-Pong (Easy)

**Objective**: Implement basic application-level ping/pong.

**Requirements:**

1. Server sends `{ "type": "ping", "timestamp": "..." }` every **10 seconds**
2. Client must respond with `{ "type": "pong", "timestamp": "..." }` (echo back the server's timestamp)
3. Server logs each pong received with round-trip time
4. Use `Task.Delay()` in a loop to schedule pings

**Expected server console output:**

```
[12:00:00] Sent ping to client abc123
[12:00:00] Received pong from abc123 (round-trip: 12ms)
[12:00:10] Sent ping to client abc123
[12:00:10] Received pong from abc123 (round-trip: 8ms)
```

<details>
<summary>💡 Hints</summary>

- Run the ping loop as a separate `Task` alongside the receive loop
- Use `CancellationTokenSource` to cancel the ping loop when the connection closes
- Calculate round-trip time by comparing sent and received timestamps

</details>

---

## 🟢 Kata 2: Connection Health Tracker (Easy)

**Objective**: Track the health status of each connection.

**Requirements:**

1. Create a `ConnectionHealth` class:

   ```csharp
   class ConnectionHealth
   {
       public string ClientId { get; set; }
       public DateTime ConnectedAt { get; set; }
       public DateTime LastPongReceived { get; set; }
       public int MissedPings { get; set; }
       public int TotalMessagesSent { get; set; }
       public int TotalMessagesReceived { get; set; }
   }
   ```

2. Update health stats as messages flow
3. Add an endpoint `GET /api/health` that returns all connection health data as JSON
4. Include uptime calculation (now - connectedAt)

---

## 🟡 Kata 3: Dead Connection Cleanup (Medium)

**Objective**: Automatically detect and remove dead connections.

**Requirements:**

1. After **3 missed pings** (30 seconds without pong), consider a connection dead
2. Close the dead connection with code `1001` (Going Away) and reason `"Ping timeout"`
3. Log when connections are cleaned up
4. Broadcast to remaining clients: `{ "type": "system", "text": "User X timed out" }`
5. Add a configurable `maxMissedPings` parameter

**Test scenario:**

```
1. Connect a client
2. Stop the client from responding to pings (simulate by not sending pong)
3. Wait 30+ seconds
4. Observe the server closing the connection
```

<details>
<summary>💡 Hints</summary>

- Increment `MissedPings` when sending a ping
- Reset `MissedPings` to 0 when receiving a pong
- Run cleanup check after each ping cycle
- Use try-catch when closing — the connection might already be closed

</details>

---

## 🟡 Kata 4: BackgroundService Heartbeat (Medium)

**Objective**: Use `BackgroundService` (IHostedService) for the heartbeat instead of per-connection tasks.

**Requirements:**

1. Create a `HeartbeatService` class that extends `BackgroundService`
2. Register it with `builder.Services.AddHostedService<HeartbeatService>()`
3. The service should:
   - Ping all connections every 10 seconds
   - Check for dead connections every 15 seconds
   - Log a heartbeat summary every 30 seconds
4. Inject the connection manager via dependency injection
5. Handle graceful shutdown when the application stops

<details>
<summary>💡 Hints</summary>

- Create a `ConnectionManager` class and register it as a singleton: `builder.Services.AddSingleton<ConnectionManager>()`
- The `HeartbeatService` receives the `ConnectionManager` via constructor injection
- Override `ExecuteAsync(CancellationToken stoppingToken)` — use the token in `Task.Delay()`
- Use `PeriodicTimer` instead of `Task.Delay` for more accurate intervals

</details>

---

## 🟡 Kata 5: Client-Side Reconnection (Medium)

**Objective**: Implement auto-reconnect on the client with exponential backoff.

**Requirements:**

1. Write a JavaScript reconnection manager that:
   - Detects disconnection via `onclose`
   - Waits with exponential backoff: 1s → 2s → 4s → 8s → 16s → 30s (max)
   - Adds random jitter (±500ms) to prevent thundering herd
   - Shows a reconnection UI with attempt count and countdown
   - Caps at **10 reconnection attempts** then gives up
2. On successful reconnect:
   - Reset the attempt counter
   - Restore the previous state (username, room, etc.)
   - Show a "Reconnected!" message

**Implement the formula:**

```javascript
function getBackoffDelay(attempt) {
    const baseDelay = 1000; // 1 second
    const maxDelay = 30000; // 30 seconds
    const jitter = Math.random() * 1000 - 500; // ±500ms
    return Math.min(baseDelay * Math.pow(2, attempt) + jitter, maxDelay);
}
```

---

## 🔴 Kata 6: Connection State Machine (Hard)

**Objective**: Implement a proper state machine for connection management.

**Requirements:**

1. Define these states: `DISCONNECTED`, `CONNECTING`, `CONNECTED`, `RECONNECTING`, `FAILED`
2. Define valid transitions:

   ```
   DISCONNECTED  → CONNECTING     (user clicks connect)
   CONNECTING    → CONNECTED      (connection succeeded)
   CONNECTING    → FAILED         (connection failed)
   CONNECTED     → DISCONNECTED   (user clicks disconnect)
   CONNECTED     → RECONNECTING   (unexpected disconnection)
   RECONNECTING  → CONNECTING     (retry attempt)
   RECONNECTING  → FAILED         (max retries exceeded)
   FAILED        → CONNECTING     (user clicks reconnect)
   ```

3. Reject invalid transitions (e.g., `DISCONNECTED → CONNECTED`)
4. Emit events on state change: `onStateChange(oldState, newState)`
5. Update the UI to reflect the current state with colors and icons
6. Implement both on client-side (JavaScript) and log state changes on server-side

<details>
<summary>💡 Hints</summary>

- Use a `Map<State, Set<State>>` to define valid transitions
- Throw an error on invalid transitions to catch bugs early
- Consider the `EventTarget` API or a simple callbacks array for events
- This is a textbook State Pattern — each state can be a class with allowed methods

</details>

---

## 🔴 Kata 7: Resilient Connection Manager (Hard)

**Objective**: Build a production-grade connection manager combining heartbeat, reconnection, and health monitoring.

**Requirements:**

1. Create a `ResilientConnectionManager` that combines:
   - Server-side heartbeat with configurable interval
   - Dead connection detection and cleanup
   - Client-side auto-reconnect with exponential backoff
   - Connection health API endpoint
2. Support **connection priority levels**:
   - `HIGH` — ping every 5s, cleanup after 2 missed pings
   - `NORMAL` — ping every 15s, cleanup after 3 missed pings
   - `LOW` — ping every 30s, cleanup after 5 missed pings
3. Implement a **connection quality score** (0-100) based on:
   - Response time to pings (lower = better)
   - Missed pings (fewer = better)
   - Connection uptime (longer = better)
4. Broadcast quality degradation warnings when score drops below 50
5. Add a `/api/dashboard` endpoint returning all connections with their quality scores

<details>
<summary>💡 Hints</summary>

- Use a `SemaphoreSlim` or similar for rate-limiting ping sends
- Quality formula example: `100 - (missedPings * 15) - (avgLatency > 200 ? 20 : 0) + min(uptimeMinutes, 30)`
- Consider using `Channel<T>` for queuing messages when the connection is in RECONNECTING state
- Use `IOptions<T>` pattern for configurable ping intervals and thresholds

</details>
