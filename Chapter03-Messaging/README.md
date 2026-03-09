# 💬 Chapter 03 — Sending & Receiving Messages

> **Goal**: Build a structured JSON message protocol for WebSocket communication  
> **Concepts**: JSON serialization, message types, System.Text.Json, text vs binary frames

---

## 🎯 What You'll Learn

- How to design a JSON-based message protocol
- Sending and receiving structured messages with `System.Text.Json`
- Handling different message types (chat, command, ping/pong)
- The difference between text and binary WebSocket frames
- Building a chat-like client interface

---

## 📊 Message Flow

![Message Flow Diagram](./images/message-flow.png)

---

## 🔑 Key Concepts

### Why Use JSON?

Raw text messages work but have no structure. JSON gives us:

- **Message types** — differentiate between chat, commands, system messages
- **Metadata** — timestamps, sender info, message IDs
- **Extensibility** — add new fields without breaking existing code
- **Universality** — both C# and JavaScript handle JSON natively

### Our Message Protocol

```json
// Client → Server (Chat)
{ "type": "chat", "text": "Hello, World!" }

// Server → Client (Chat Response)
{ "type": "chat", "from": "a1b2c3d4", "text": "Hello, World!", "timestamp": "2024-01-01T12:00:00Z", "messageId": "e5f6g7h8" }

// Client → Server (Command)
{ "type": "command", "command": "time" }

// Server → Client (Command Response)
{ "type": "command_response", "command": "time", "result": "2024-01-01 12:00:00 UTC" }

// Client → Server (Ping)
{ "type": "ping" }

// Server → Client (Pong)
{ "type": "pong", "serverTime": "2024-01-01T12:00:00.000Z" }
```

### Text vs Binary Frames

| Frame Type | When to Use | C# Enum |
|-----------|------------|---------|
| **Text** | JSON, plain text, any UTF-8 data | `WebSocketMessageType.Text` |
| **Binary** | Images, files, protobuf, msgpack | `WebSocketMessageType.Binary` |

---

## 📁 Files in This Chapter

| File | Purpose |
|------|---------|
| `Program.cs` | Server with JSON message routing and command handling |
| `wwwroot/index.html` | Chat client with message type selector and JSON viewer |

---

## 🚀 How to Run

```bash
cd Chapter03-Messaging
dotnet run
```

Open **<http://localhost:5000>** → The client auto-connects!

### What to Try

1. **Chat mode** — Send text messages, see them echoed back with metadata
2. **Command mode** — Try `time`, `whoami`, `help`
3. **Ping** — Send a ping and see the server's pong with timestamp
4. **Raw text** — Send non-JSON text and see how the server handles it
5. **Quick actions** — Use the buttons at the bottom for one-click commands

---

## 📝 Code Walkthrough

### JSON Serialization in C #

```csharp
// Configure camelCase for JavaScript compatibility
var jsonOptions = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
};

// Serialize an anonymous object to JSON
var json = JsonSerializer.Serialize(new
{
    Type = "chat",
    From = clientId,
    Text = "Hello!"
}, jsonOptions);
// Result: {"type":"chat","from":"abc123","text":"Hello!"}
```

### Parsing Incoming JSON

```csharp
// Parse the raw JSON string
using var doc = JsonDocument.Parse(rawMessage);
var root = doc.RootElement;

// Safely extract properties
var type = root.TryGetProperty("type", out var typeProp) 
    ? typeProp.GetString() ?? "unknown" 
    : "unknown";
```

> 💡 **Why `JsonDocument.Parse` instead of `JsonSerializer.Deserialize<T>`?**  
> When you don't know the exact message shape in advance (the `type` field determines the structure), `JsonDocument` lets you inspect the JSON dynamically. In production, you'd typically deserialize to typed classes.

### Helper Method Pattern

```csharp
async Task SendJsonMessage(WebSocket webSocket, object message, JsonSerializerOptions options)
{
    if (webSocket.State != WebSocketState.Open) return;
    var json = JsonSerializer.Serialize(message, options);
    var bytes = Encoding.UTF8.GetBytes(json);
    await webSocket.SendAsync(
        new ArraySegment<byte>(bytes),
        WebSocketMessageType.Text,
        endOfMessage: true,
        CancellationToken.None
    );
}
```

This helper method:

1. **Checks** the connection is still open before sending
2. **Serializes** the object to JSON
3. **Encodes** the JSON string to bytes (UTF-8)
4. **Sends** as a text frame

---

## 🧪 Experiments to Try

1. **Send invalid JSON** — Type `not json` in raw mode  → See the echo fallback
2. **Send unknown type** — Manually send `{"type":"unknown"}` → See the error response
3. **Send unknown command** — Try `{"type":"command","command":"foo"}` → See the help suggestion
4. **Watch the JSON viewer** — Expand received messages to see full JSON structure

---

## 🧠 Key Takeaways

1. **Always use a message protocol** — don't send raw strings in real apps
2. **`System.Text.Json`** is the modern, high-performance JSON library in .NET
3. **`JsonDocument`** is great for dynamic JSON parsing when types are unknown
4. **camelCase** convention bridges C# (PascalCase) and JavaScript (camelCase)
5. **Helper methods** for sending avoid code duplication

---

## ⏭ Next Chapter

**[Chapter 04 — Broadcasting & Groups →](../Chapter04-Broadcasting/)**

Next, we'll manage multiple clients and implement broadcasting and group messaging!
