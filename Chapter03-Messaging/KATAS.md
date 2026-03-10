# 🥋 Chapter 03 — Code Katas: Sending & Receiving Messages

> **Type**: Hands-on coding exercises  
> **Goal**: Master JSON-based WebSocket messaging with System.Text.Json

---

## 🟢 Kata 1: JSON Echo Server (Easy)

**Objective**: Build a server that parses incoming JSON and echoes it back with metadata.

**Requirements:**

1. Accept JSON messages with the format: `{ "text": "Hello" }`
2. Respond with: `{ "echo": "Hello", "receivedAt": "2024-01-01T12:00:00Z", "charCount": 5 }`
3. Use `System.Text.Json` with `JsonNamingPolicy.CamelCase`
4. If the incoming message is not valid JSON, respond with: `{ "error": "Invalid JSON" }`

**Test it**:

```javascript
const ws = new WebSocket('ws://localhost:5000/ws');
ws.onmessage = (e) => console.log(JSON.parse(e.data));
ws.onopen = () => {
    ws.send(JSON.stringify({ text: "Hello" }));
    // → { echo: "Hello", receivedAt: "...", charCount: 5 }
    ws.send("not json");
    // → { error: "Invalid JSON" }
};
```

<details>
<summary>💡 Hints</summary>

- Wrap `JsonDocument.Parse()` in a try-catch to detect invalid JSON
- Use `DateTime.UtcNow.ToString("o")` for ISO 8601 timestamps
- Use anonymous objects with `JsonSerializer.Serialize()` for responses

</details>

---

## 🟢 Kata 2: Command Router (Easy)

**Objective**: Route messages to different handlers based on a `type` field.

**Requirements:**

1. Define a message protocol with a `type` field
2. Handle these types:
   - `"greet"` + `"name"` field → respond with `"Hello, {name}!"`
   - `"math"` + `"a"` + `"b"` fields → respond with `{ "sum": a+b, "product": a*b }`
   - `"reverse"` + `"text"` field → respond with the reversed text
3. Unknown types should get: `{ "error": "Unknown type: {type}" }`

**Test messages:**

```json
{ "type": "greet", "name": "Alice" }
{ "type": "math", "a": 5, "b": 3 }
{ "type": "reverse", "text": "WebSocket" }
{ "type": "foo" }
```

---

## 🟡 Kata 3: Message ID Tracking (Medium)

**Objective**: Implement request-response correlation with message IDs.

**Requirements:**

1. Each client message should include a `"messageId"` field (e.g., `"msg-001"`)
2. The server must include the same `messageId` in its response
3. The server also generates its own `"serverMessageId"` (a GUID)
4. If no `messageId` is provided, the server assigns one (`"server-generated-{n}"`)
5. Track and log the total number of messages processed per connection

**Protocol:**

```json
// Client sends:
{ "messageId": "msg-001", "type": "echo", "text": "Hi" }

// Server responds:
{ "messageId": "msg-001", "serverMessageId": "a1b2c3...", "type": "echo", "text": "Hi", "messageNumber": 1 }
```

---

## 🟡 Kata 4: Typed Message Classes (Medium)

**Objective**: Use strongly-typed C# classes instead of `JsonDocument`.

**Requirements:**

1. Create C# record/class for each message type:

   ```csharp
   record ChatMessage(string Type, string Text);
   record CommandMessage(string Type, string Command);
   record PingMessage(string Type);
   ```

2. Create a base class or use a discriminated approach to parse the `type` first
3. Deserialize to the correct type using `JsonSerializer.Deserialize<T>()`
4. Create matching response types and serialize them back
5. Handle deserialization failures gracefully

<details>
<summary>💡 Hints</summary>

- First parse with `JsonDocument` to read the `type` field
- Then re-deserialize the raw string to the matching C# type
- Consider using `[JsonPropertyName("type")]` attributes for custom naming
- C# records are perfect for immutable message types

</details>

---

## 🟡 Kata 5: Message History Buffer (Medium)

**Objective**: Maintain a server-side history of the last N messages.

**Requirements:**

1. Keep the last **10 messages** in a circular buffer on the server
2. When a new client connects, immediately send them all messages from the buffer
3. Add a `"history"` command type that returns the buffer contents on demand
4. Each stored message should include: `text`, `timestamp`, `clientId`
5. Use a `Queue<T>` or `LinkedList<T>` for the buffer — dequeue the oldest when full

**Protocol:**

```json
// Client sends:
{ "type": "history" }

// Server responds:
{
  "type": "history",
  "messages": [
    { "text": "Hello", "timestamp": "...", "clientId": "abc123" },
    { "text": "World", "timestamp": "...", "clientId": "def456" }
  ]
}
```

---

## 🔴 Kata 6: Message Validation Middleware (Hard)

**Objective**: Build a validation layer for incoming messages.

**Requirements:**

1. Create a `MessageValidator` class with these rules:
   - `type` field is **required** and must be a non-empty string
   - `text` field (when present) must be ≤ 500 characters
   - `messageId` (when present) must match pattern `^[a-zA-Z0-9-]+$`
   - No unknown fields allowed (reject messages with extra properties)
2. If validation fails, respond with:

   ```json
   { "error": "validation_failed", "details": ["text exceeds 500 chars", "unknown field: foo"] }
   ```

3. Only pass valid messages to the message handler
4. Log all validation failures on the server

<details>
<summary>💡 Hints</summary>

- Use `JsonDocument` to enumerate all properties with `root.EnumerateObject()`
- Compare property names against a whitelist of allowed fields per message type
- Collect all errors in a `List<string>` before responding
- Consider using `System.Text.RegularExpressions.Regex` for pattern validation

</details>

---

## 🔴 Kata 7: Binary Protocol (Hard)

**Objective**: Implement a custom binary message protocol alongside JSON.

**Requirements:**

1. Support **two modes**: JSON (text frames) and binary (binary frames)
2. Define a simple binary protocol:

   ```
   [1 byte: message type] [4 bytes: payload length (big-endian)] [N bytes: payload]
   ```

   - Type `0x01` = Text message (payload is UTF-8 string)
   - Type `0x02` = Timestamp request (no payload, server responds with 8-byte Unix timestamp)
   - Type `0x03` = Ping (payload = client timestamp, response = client timestamp + server timestamp)
3. Parse incoming binary frames according to this protocol
4. Respond in binary format for binary requests, JSON for JSON requests
5. Log the frame type (text/binary) for each message

<details>
<summary>💡 Hints</summary>

- Check `receiveResult.MessageType` to determine if it's `Text` or `Binary`
- Use `BitConverter.ToInt32()` for reading the length field (watch endianness!)
- Use `BinaryPrimitives.ReadInt32BigEndian()` from `System.Buffers.Binary` for big-endian
- `DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()` gives you a long timestamp

</details>
