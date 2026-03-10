# 🥋 Chapter 04 — Code Katas: Broadcasting & Groups

> **Type**: Hands-on coding exercises  
> **Goal**: Master multi-client connection management, broadcasting, groups, and direct messaging

---

## 🟢 Kata 1: Connection Counter (Easy)

**Objective**: Track and broadcast the total number of connected clients.

**Requirements:**

1. Use a `ConcurrentDictionary<string, WebSocket>` to track connections
2. When a client connects, broadcast to ALL clients: `{ "type": "user_count", "count": N }`
3. When a client disconnects, broadcast the updated count
4. Each client gets a unique ID (use `Guid.NewGuid().ToString("N")[..8]`)

**Expected behavior with 3 tabs:**

```
Tab 1 connects → all see: { count: 1 }
Tab 2 connects → all see: { count: 2 }
Tab 3 connects → all see: { count: 3 }
Tab 2 closes   → remaining see: { count: 2 }
```

---

## 🟢 Kata 2: Broadcast Chat (Easy)

**Objective**: Build the simplest possible chat — every message goes to everyone.

**Requirements:**

1. When a client sends `{ "type": "chat", "text": "Hello" }`
2. Broadcast to ALL connected clients (including the sender):

   ```json
   { "type": "chat", "from": "abc123", "text": "Hello", "timestamp": "..." }
   ```

3. When a client joins, broadcast: `{ "type": "system", "text": "User abc123 joined" }`
4. When a client leaves, broadcast: `{ "type": "system", "text": "User abc123 left" }`
5. Use `Task.WhenAll()` to send to all clients in parallel

<details>
<summary>💡 Hints</summary>

- Filter out closed connections: `.Where(c => c.Value.State == WebSocketState.Open)`
- Wrap each send in a try-catch — don't let one failed send crash the broadcast
- Remember to remove disconnected clients from the dictionary

</details>

---

## 🟡 Kata 3: Group Chat Rooms (Medium)

**Objective**: Implement join/leave room functionality.

**Requirements:**

1. Support these commands:
   - `{ "type": "join", "room": "general" }` — join a room
   - `{ "type": "leave" }` — leave current room
   - `{ "type": "chat", "text": "Hi" }` — send to current room only
   - `{ "type": "rooms" }` — list all rooms with member counts
2. A client can only be in **one room at a time**
3. Joining a new room automatically leaves the old one
4. Room messages only go to members of that room
5. When the last member leaves a room, clean up the empty room

**Data structures:**

```csharp
ConcurrentDictionary<string, HashSet<string>> _rooms;    // room → clientIds
ConcurrentDictionary<string, string> _clientRoom;         // clientId → room
```

---

## 🟡 Kata 4: Direct Messaging (Medium)

**Objective**: Send private messages between specific clients.

**Requirements:**

1. Support setting a display name: `{ "type": "setname", "name": "Alice" }`
2. Support direct messages: `{ "type": "dm", "to": "bob123", "text": "Secret!" }`
3. The DM should only be delivered to the target client
4. If the target client doesn't exist, respond with an error
5. Add a `{ "type": "users" }` command that lists all connected users with their IDs and names

**Protocol:**

```json
// Sender sends:
{ "type": "dm", "to": "bob123", "text": "Hey!" }

// Recipient receives:
{ "type": "dm", "from": "alice456", "fromName": "Alice", "text": "Hey!", "private": true }

// Sender gets confirmation:
{ "type": "dm_sent", "to": "bob123", "text": "Hey!" }
```

---

## 🟡 Kata 5: Typing Indicator (Medium)

**Objective**: Implement "User is typing..." indicators.

**Requirements:**

1. Client sends `{ "type": "typing", "isTyping": true }` when the user starts typing
2. Client sends `{ "type": "typing", "isTyping": false }` when the user stops
3. Broadcast the typing status to all OTHER clients (not the sender)
4. Auto-expire typing status after **3 seconds** of no typing update (server-side)
5. The typing broadcast should include the user's name/ID

**Bonus**: Only broadcast typing status to members of the same room (if rooms are implemented).

<details>
<summary>💡 Hints</summary>

- Use `BroadcastExcept(senderId, message)` — a variant of broadcast that skips the sender
- Track last typing time per client with a `ConcurrentDictionary<string, DateTime>`
- Use a background timer to clear expired typing statuses

</details>

---

## 🔴 Kata 6: Message Delivery Acknowledgment (Hard)

**Objective**: Implement guaranteed message delivery with acknowledgments.

**Requirements:**

1. Every broadcast message gets a unique `deliveryId`
2. Clients must acknowledge receipt: `{ "type": "ack", "deliveryId": "..." }`
3. Track which clients have acknowledged each message
4. If a client doesn't ACK within **5 seconds**, retry the send (max 3 retries)
5. After 3 failed retries, mark the message as `undelivered` for that client
6. Add a `{ "type": "delivery_status", "deliveryId": "..." }` command to check delivery status

**Tracking structure:**

```csharp
class PendingDelivery
{
    public string DeliveryId { get; set; }
    public object Message { get; set; }
    public HashSet<string> AckedBy { get; set; }
    public HashSet<string> PendingFor { get; set; }
    public int RetryCount { get; set; }
    public DateTime SentAt { get; set; }
}
```

---

## 🔴 Kata 7: Role-Based Access Control (Hard)

**Objective**: Implement roles and permissions for chat rooms.

**Requirements:**

1. Define 3 roles: `owner`, `moderator`, `member`
2. The first person to create a room becomes the `owner`
3. Permissions:
   - `member` — can send messages, leave room
   - `moderator` — can mute/unmute members, kick members
   - `owner` — can promote/demote users, delete the room
4. Support these commands:

   ```json
   { "type": "promote", "userId": "...", "role": "moderator" }
   { "type": "kick", "userId": "..." }
   { "type": "mute", "userId": "..." }
   { "type": "unmute", "userId": "..." }
   { "type": "delete_room" }
   ```

5. Muted members can still receive messages but cannot send
6. Kicked members are removed from the room and receive a notification

<details>
<summary>💡 Hints</summary>

- Create a `RoomMember` class with `ClientId`, `Role`, `IsMuted` properties
- Check permissions before executing each command
- Broadcast role changes to all room members
- When the owner leaves, transfer ownership to the next moderator (or oldest member)

</details>
