# 🥋 Chapter 02 — Code Katas: Your First WebSocket Connection

> **Type**: Hands-on coding exercises  
> **Goal**: Master the WebSocket connection lifecycle in ASP.NET Core

---

## 🟢 Kata 1: Echo Server from Scratch (Easy)

**Objective**: Build a minimal WebSocket echo server without looking at the chapter code.

**Requirements:**

1. Create a new ASP.NET Core project (or a new file in this folder called `Kata1.cs`)
2. Enable WebSocket middleware with `UseWebSockets()`
3. Map a `/ws` endpoint that accepts WebSocket connections
4. Echo back any text message the client sends, prefixed with `"Echo: "`
5. Handle graceful close when the client disconnects

**Test it**: Open browser console and run:

```javascript
const ws = new WebSocket('ws://localhost:5000/ws');
ws.onmessage = (e) => console.log(e.data);
ws.onopen = () => ws.send('Hello!');
// Expected output: "Echo: Hello!"
```

<details>
<summary>💡 Hints</summary>

- You need `app.UseWebSockets()` before your route mapping
- Check `context.WebSockets.IsWebSocketRequest` before accepting
- Use `AcceptWebSocketAsync()` to complete the handshake
- Use a `while` loop with `ReceiveAsync()` — exit when `CloseStatus.HasValue`
- Use `Encoding.UTF8.GetString()` to convert the buffer to a string

</details>

---

## 🟢 Kata 2: Connection Logger (Easy)

**Objective**: Log the full WebSocket lifecycle.

**Requirements:**

1. Start from your echo server (Kata 1)
2. Log to the console (with timestamps) when:
   - A new WebSocket connection is accepted
   - A message is received (include the message content)
   - A message is sent back
   - The connection is closed (include the close code and reason)
3. Use `Console.WriteLine` with `DateTime.Now` for timestamps

**Expected console output:**

```
[2024-01-01 12:00:00] WebSocket connected
[2024-01-01 12:00:01] Received: Hello!
[2024-01-01 12:00:01] Sent: Echo: Hello!
[2024-01-01 12:00:05] WebSocket closed: NormalClosure - Done
```

---

## 🟡 Kata 3: Multiple Message Types (Medium)

**Objective**: Handle different types of responses based on the message content.

**Requirements:**

1. If the client sends `"time"` → respond with the current server time
2. If the client sends `"date"` → respond with today's date
3. If the client sends `"random"` → respond with a random number between 1-100
4. For any other message → echo it back with `"Echo: "` prefix
5. All responses should be plain text (no JSON yet)

**Test it**:

```javascript
const ws = new WebSocket('ws://localhost:5000/ws');
ws.onmessage = (e) => console.log(e.data);
ws.onopen = () => {
    ws.send('time');    // → "Server time: 12:00:00"
    ws.send('date');    // → "Today's date: 2024-01-01"
    ws.send('random');  // → "Random number: 42"
    ws.send('Hi!');     // → "Echo: Hi!"
};
```

---

## 🟡 Kata 4: Custom HTML Client (Medium)

**Objective**: Build a browser client from scratch with a UI.

**Requirements:**

1. Create an `index.html` file (or modify the existing one)
2. Include these UI elements:
   - A **Connect** button
   - A **Disconnect** button
   - A text input + **Send** button
   - A message log area that shows all sent/received messages
   - A **status indicator** showing the current connection state
3. The status indicator should show:
   - 🔴 Disconnected (default)
   - 🟡 Connecting...
   - 🟢 Connected
   - 🟠 Closing...
4. Use the JavaScript `WebSocket` API (`onopen`, `onmessage`, `onclose`, `onerror`)
5. Disable the Send button when not connected

<details>
<summary>💡 Hints</summary>

- Use `WebSocket.readyState` to determine the state (0=CONNECTING, 1=OPEN, 2=CLOSING, 3=CLOSED)
- Disable buttons based on state: Connect only when CLOSED, Disconnect/Send only when OPEN
- Append messages to a `<div>` or `<ul>` element
- Style sent vs received messages differently (e.g., different colors or alignment)

</details>

---

## 🟡 Kata 5: Binary Message Support (Medium)

**Objective**: Handle both text and binary WebSocket messages.

**Requirements:**

1. Extend your echo server to detect message type (`WebSocketMessageType.Text` vs `.Binary`)
2. For text messages: echo as before
3. For binary messages: respond with the byte count, e.g., `"Received 256 bytes of binary data"`
4. Log the message type on the server console

**Test binary from browser:**

```javascript
const ws = new WebSocket('ws://localhost:5000/ws');
ws.onmessage = (e) => console.log(e.data);
ws.onopen = () => {
    // Send binary data
    const buffer = new ArrayBuffer(8);
    const view = new Uint8Array(buffer);
    view.fill(42);
    ws.send(buffer);
    // Expected: "Received 8 bytes of binary data"
};
```

---

## 🔴 Kata 6: Connection Limiter (Hard)

**Objective**: Limit the number of simultaneous WebSocket connections.

**Requirements:**

1. Allow a maximum of **3** concurrent WebSocket connections
2. If a 4th client tries to connect, respond with HTTP **503 Service Unavailable** (don't accept the WebSocket)
3. Track the current connection count using a thread-safe counter (`Interlocked`)
4. When a connection closes, decrement the counter
5. Add an HTTP endpoint `GET /api/connections` that returns the current count

**Test it**: Open 4 browser tabs and try to connect from each.

<details>
<summary>💡 Hints</summary>

- Use `Interlocked.Increment()` and `Interlocked.Decrement()` for thread-safe counting
- Check the count **before** calling `AcceptWebSocketAsync()`
- If over the limit, set `context.Response.StatusCode = 503` and return
- Wrap the WebSocket handling in a `try/finally` to ensure the counter is decremented

</details>

---

## 🔴 Kata 7: Message Size Limiter (Hard)

**Objective**: Protect your server from oversized messages.

**Requirements:**

1. Set a maximum message size of **1 KB** (1024 bytes)
2. If a client sends a message larger than 1 KB:
   - Send back an error message: `"Error: Message too large (max 1024 bytes)"`
   - Do NOT close the connection (allow the client to send smaller messages)
3. If a client sends a message larger than **10 KB**:
   - Send an error message and **close the connection** with close code `1009` (Message Too Big)
4. Log all rejected messages on the server

**Test it**:

```javascript
const ws = new WebSocket('ws://localhost:5000/ws');
ws.onmessage = (e) => console.log(e.data);
ws.onopen = () => {
    ws.send('a'.repeat(500));   // OK — echoed back
    ws.send('a'.repeat(2000));  // Rejected — error message
    ws.send('a'.repeat(20000)); // Rejected + connection closed
};
```

<details>
<summary>💡 Hints</summary>

- Check `receiveResult.Count` to get the actual bytes received
- Use `WebSocketCloseStatus.MessageTooBig` (1009) for the close code
- Be careful: `ReceiveAsync` may split large messages into multiple frames — check `receiveResult.EndOfMessage`

</details>
