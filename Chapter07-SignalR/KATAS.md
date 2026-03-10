# 🥋 Chapter 07 — Code Katas: SignalR

> **Type**: Hands-on coding exercises  
> **Goal**: Master ASP.NET Core SignalR — Hubs, groups, typed methods, and auto-reconnection

---

## 🟢 Kata 1: Hello SignalR (Easy)

**Objective**: Build the simplest possible SignalR app.

**Requirements:**

1. Create a `ChatHub` class that extends `Hub`
2. Add a single method `SendMessage(string message)` that broadcasts to all clients
3. Set up the server with `AddSignalR()` and `MapHub<ChatHub>("/chatHub")`
4. Create a minimal HTML client using the SignalR JavaScript client
5. Verify messages are echoed to all connected tabs

**Server:**

```csharp
public class ChatHub : Hub
{
    public async Task SendMessage(string message)
    {
        await Clients.All.SendAsync("ReceiveMessage", Context.ConnectionId, message);
    }
}
```

**Client:**

```javascript
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/chatHub")
    .build();

connection.on("ReceiveMessage", (user, message) => {
    console.log(`${user}: ${message}`);
});

await connection.start();
await connection.invoke("SendMessage", "Hello!");
```

---

## 🟢 Kata 2: Connection Events (Easy)

**Objective**: Handle connection lifecycle events in SignalR.

**Requirements:**

1. Override `OnConnectedAsync()` — broadcast `"User X connected"` to all
2. Override `OnDisconnectedAsync()` — broadcast `"User X disconnected"` to all
3. Track connected users in a `static ConcurrentDictionary`
4. Add a hub method `GetConnectedUsers()` that returns the list of connected user IDs
5. Display the user list in the client UI

<details>
<summary>💡 Hints</summary>

- `Context.ConnectionId` gives you the unique connection ID
- Use a static dictionary since Hub instances are transient (created per invocation)
- Call `Clients.All.SendAsync("UserConnected", userId)` in `OnConnectedAsync`

</details>

---

## 🟡 Kata 3: Group Chat with SignalR (Medium)

**Objective**: Implement group chat using SignalR's built-in Groups API.

**Requirements:**

1. Add hub methods:
   - `JoinRoom(string roomName)` — add caller to a group
   - `LeaveRoom(string roomName)` — remove caller from a group
   - `SendToRoom(string roomName, string message)` — send to a specific room
   - `GetRooms()` — return all active room names with member counts
2. Limit each user to **one room at a time** (leave old room when joining new)
3. Notify room members when someone joins or leaves
4. Compare the code size to your Chapter 04 implementation

**Compare:**

```csharp
// Chapter 04 (raw WebSocket): ~50 lines of ConcurrentDictionary + HashSet + locks
// SignalR: 
await Groups.AddToGroupAsync(Context.ConnectionId, roomName);
await Clients.Group(roomName).SendAsync("ReceiveMessage", "User X joined!");
```

---

## 🟡 Kata 4: Strongly-Typed Hub (Medium)

**Objective**: Replace magic strings with a strongly-typed hub interface.

**Requirements:**

1. Define a client interface:

   ```csharp
   public interface IChatClient
   {
       Task ReceiveMessage(string user, string message);
       Task UserConnected(string userId);
       Task UserDisconnected(string userId);
       Task RoomNotification(string room, string message);
   }
   ```

2. Change your hub to inherit from `Hub<IChatClient>`
3. Replace all `SendAsync("MethodName", args)` with typed calls
4. Verify that typos are now caught at compile time

**Before (magic strings):**

```csharp
await Clients.All.SendAsync("ReceiveMessage", user, msg);  // typo = runtime error!
```

**After (strongly typed):**

```csharp
await Clients.All.ReceiveMessage(user, msg);  // typo = compile error!
```

---

## 🟡 Kata 5: Auto-Reconnect with State Recovery (Medium)

**Objective**: Implement automatic reconnection and restore client state.

**Requirements:**

1. Configure the SignalR client with `.withAutomaticReconnect([0, 2000, 5000, 10000, 30000])`
2. Handle these events:
   - `onreconnecting` — show "Reconnecting..." UI
   - `onreconnected` — show "Reconnected!" and restore state
   - `onclose` — show "Disconnected" and offer manual reconnect
3. On reconnect, automatically:
   - Re-join the previously joined room
   - Send a `{ "action": "sync" }` request to get missed messages
4. Store the client's state (room, username) in `sessionStorage`
5. Display a reconnection attempt counter in the UI

<details>
<summary>💡 Hints</summary>

- `onreconnected` gives you the new `connectionId` — it changes on reconnect!
- Store `lastRoom` and `userName` before disconnect
- Call `connection.invoke("JoinRoom", lastRoom)` after reconnect
- Consider implementing a "missed messages" buffer on the server side

</details>

---

## 🔴 Kata 6: SignalR Streaming (Hard)

**Objective**: Implement server-to-client streaming with `IAsyncEnumerable`.

**Requirements:**

1. Create a hub method that **streams** data to the client:

   ```csharp
   public async IAsyncEnumerable<StockPrice> StreamStockPrices(
       string symbol,
       [EnumeratorCancellation] CancellationToken cancellationToken)
   {
       while (!cancellationToken.IsCancellationRequested)
       {
           yield return new StockPrice(symbol, GetRandomPrice());
           await Task.Delay(1000, cancellationToken);
       }
   }
   ```

2. Generate fake stock prices with random walk: `price += (random.NextDouble() - 0.5) * 2`
3. Client subscribes to the stream and displays prices in a live-updating list
4. Support streaming multiple symbols simultaneously
5. Client can cancel individual streams

**Client-side streaming:**

```javascript
const stream = connection.stream("StreamStockPrices", "AAPL");
stream.subscribe({
    next: (price) => updateUI(price),
    error: (err) => console.error(err),
    complete: () => console.log("Stream ended")
});
```

---

## 🔴 Kata 7: Build a Notification System (Hard)

**Objective**: Build a complete real-time notification system using SignalR.

**Requirements:**

1. **Notification types**: `info`, `warning`, `error`, `success`
2. **Targeting**:
   - Send to all users
   - Send to a specific user
   - Send to a role group (all admins, all moderators)
   - Send to a custom channel (users subscribe to channels)
3. **Notification model**:

   ```csharp
   record Notification(
       string Id,
       string Type,
       string Title,
       string Message,
       DateTime CreatedAt,
       bool IsRead
   );
   ```

4. **Persistence**: Store last 50 notifications per user in memory
5. **Read receipts**: Client can mark notifications as read
6. **REST API** to send notifications: `POST /api/notifications`
7. **Toast UI**: Display notifications as animated toast popups that auto-dismiss

<details>
<summary>💡 Hints</summary>

- Use `IHubContext<NotificationHub>` to send notifications from controllers/services
- Group users by role at connection time: `Groups.AddToGroupAsync(connectionId, "role:admin")`
- For persistence, use `ConcurrentDictionary<string, Queue<Notification>>`
- Inject `IHubContext` into your API controller for REST-triggered notifications
- Use CSS animations (`@keyframes slideIn`) for toast popups

</details>
