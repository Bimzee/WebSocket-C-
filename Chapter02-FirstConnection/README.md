# 💻 Chapter 02 — Your First WebSocket Connection

> **Goal**: Create your first WebSocket server in C# and connect to it from a browser  
> **Concepts**: WebSocket middleware, AcceptWebSocketAsync, connection lifecycle, browser WebSocket API

---

## 🎯 What You'll Learn

- How to enable WebSocket support in ASP.NET Core
- How to accept a WebSocket connection on the server
- How to send and receive messages in a loop
- How to handle graceful disconnection
- The browser `WebSocket` API (`onopen`, `onmessage`, `onclose`, `onerror`)

---

## 🏗 Architecture

```
┌─────────────────┐         ws://localhost:5000/ws         ┌──────────────────┐
│                 │ ──────── HTTP Upgrade Request ────────► │                  │
│   Browser       │                                        │   ASP.NET Core   │
│   (index.html)  │ ◄──── 101 Switching Protocols ──────── │   Server         │
│                 │                                        │   (Program.cs)   │
│   WebSocket API │ ◄═══════ Bidirectional Messages ═════► │   WebSocket      │
│                 │                                        │   Middleware      │
└─────────────────┘                                        └──────────────────┘
```

---

## 🔑 Key Concepts

### WebSocket Connection Lifecycle

![Connection Lifecycle](./images/connection-lifecycle.png)

### Server-Side Flow (C#)

```csharp
// 1. Enable WebSocket middleware
app.UseWebSockets();

// 2. Check if it's a WebSocket request
if (context.WebSockets.IsWebSocketRequest)
{
    // 3. Accept the connection (completes the handshake)
    var webSocket = await context.WebSockets.AcceptWebSocketAsync();
    
    // 4. Receive messages in a loop
    var result = await webSocket.ReceiveAsync(buffer, CancellationToken.None);
    
    // 5. Send a response
    await webSocket.SendAsync(data, WebSocketMessageType.Text, true, CancellationToken.None);
    
    // 6. Close gracefully
    await webSocket.CloseAsync(closeStatus, reason, CancellationToken.None);
}
```

### Client-Side Flow (JavaScript)

```javascript
// 1. Create a WebSocket connection
const ws = new WebSocket('ws://localhost:5000/ws');

// 2. Handle events
ws.onopen = () => console.log('Connected!');
ws.onmessage = (event) => console.log('Received:', event.data);
ws.onclose = (event) => console.log('Closed:', event.code);
ws.onerror = (event) => console.log('Error!');

// 3. Send a message
ws.send('Hello, Server!');

// 4. Close the connection
ws.close(1000, 'Done');
```

---

## 📁 Files in This Chapter

| File | Purpose |
|------|---------|
| `Program.cs` | ASP.NET Core WebSocket server with echo functionality |
| `wwwroot/index.html` | Browser client with connect/disconnect/send UI |

---

## 🚀 How to Run

```bash
cd Chapter02-FirstConnection
dotnet run
```

Then open your browser to **<http://localhost:5000>**

### What to Try

1. Click **🔌 Connect** — watch the connection establish
2. Type a message and click **📤 Send** — the server echoes it back
3. Click **🔴 Disconnect** — observe the graceful close
4. Open the browser's Developer Tools (F12) → Network tab → filter by "WS" to see the WebSocket frames

---

## 📝 Code Walkthrough

### Server — `Program.cs`

#### 1. Enable WebSocket Middleware

```csharp
app.UseWebSockets(new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromSeconds(30)
});
```

The `UseWebSockets()` middleware tells ASP.NET Core to handle WebSocket upgrade requests. The `KeepAliveInterval` automatically sends ping frames to keep the connection alive.

#### 2. Accept the Connection

```csharp
if (context.WebSockets.IsWebSocketRequest)
{
    using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
    // Connection is now open!
}
```

`IsWebSocketRequest` checks if the incoming HTTP request has the `Upgrade: websocket` header. `AcceptWebSocketAsync()` completes the handshake by sending the `101 Switching Protocols` response.

#### 3. The Receive Loop

```csharp
var buffer = new byte[1024 * 4];
var receiveResult = await webSocket.ReceiveAsync(
    new ArraySegment<byte>(buffer), 
    CancellationToken.None
);

while (!receiveResult.CloseStatus.HasValue)
{
    // Process message...
    receiveResult = await webSocket.ReceiveAsync(...);
}
```

This is the **heart of any WebSocket server**. We continuously call `ReceiveAsync()` which blocks until:

- A message arrives → we process it
- A close frame arrives → we exit the loop

#### 4. Send a Response

```csharp
await webSocket.SendAsync(
    new ArraySegment<byte>(responseBytes),
    WebSocketMessageType.Text,
    endOfMessage: true,
    CancellationToken.None
);
```

- `WebSocketMessageType.Text` — sending UTF-8 text (use `.Binary` for binary data)
- `endOfMessage: true` — this is a complete message (not a fragment)

### Client — `wwwroot/index.html`

#### WebSocket States

The `WebSocket.readyState` property tells you the current state:

| Value | Constant | Meaning |
|-------|----------|---------|
| 0 | `WebSocket.CONNECTING` | Connection in progress |
| 1 | `WebSocket.OPEN` | Connected and ready |
| 2 | `WebSocket.CLOSING` | Close in progress |
| 3 | `WebSocket.CLOSED` | Connection closed |

#### Close Codes

When a WebSocket closes, it includes a status code:

| Code | Meaning |
|------|---------|
| 1000 | Normal closure |
| 1001 | Going away (page navigation) |
| 1006 | Abnormal closure (no close frame received) |
| 1011 | Server error |

---

## 🧪 Experiments to Try

1. **Open multiple browser tabs** — each gets its own WebSocket connection
2. **Kill the server** while connected — observe `onclose` with code `1006` (abnormal)
3. **Send an empty message** — what happens?
4. **Check the Network tab** — see the HTTP 101 upgrade and subsequent WebSocket frames

---

## 🧠 Key Takeaways

1. `UseWebSockets()` is **required** to enable WebSocket in ASP.NET Core
2. `AcceptWebSocketAsync()` completes the HTTP-to-WebSocket upgrade
3. The server runs a **receive loop** — continuously waiting for messages
4. Both sides can **send at any time** — that's full-duplex!
5. Always handle **graceful close** with proper close codes

---

## ⏭ Next Chapter

**[Chapter 03 — Sending & Receiving Messages →](../Chapter03-Messaging/)**

Next, we'll build a proper JSON-based messaging protocol and create a chat interface!
