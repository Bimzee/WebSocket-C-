# 📖 Chapter 01 — What is WebSocket?

> **Type**: Theory only — no code in this chapter  
> **Goal**: Understand what WebSocket is, why it exists, and how it compares to HTTP

---

## 🎯 What You'll Learn

- The limitations of traditional HTTP for real-time communication
- What WebSocket is and the problem it solves
- How the WebSocket handshake works (HTTP Upgrade)
- The difference between `ws://` and `wss://`
- Real-world use cases for WebSockets
- Where WebSocket fits in the .NET ecosystem

---

## 🔄 The Problem: HTTP Wasn't Built for Real-Time

Traditional HTTP follows a **request-response** model:

1. The **client** sends a request to the server
2. The **server** processes it and sends back a response
3. The connection is **closed** (or kept alive for reuse)

This works great for loading web pages, submitting forms, and fetching data. But what about scenarios where the **server needs to push data to the client** in real-time?

### Before WebSocket — The Workarounds

| Technique | How It Works | Drawbacks |
|-----------|-------------|-----------|
| **Polling** | Client repeatedly asks "Any new data?" every N seconds | Wasteful — many empty responses, high latency |
| **Long Polling** | Client asks, server holds the connection until data is available | Better, but connection overhead per message |
| **Server-Sent Events (SSE)** | Server pushes data over a one-way HTTP stream | One-directional only (server → client) |

All of these are **hacks** around HTTP's fundamental limitation: **the server cannot initiate communication**.

---

## ⚡ The Solution: WebSocket

**WebSocket** is a communication protocol that provides **full-duplex** (two-way) communication over a **single, persistent TCP connection**.

![HTTP vs WebSocket Comparison](./images/http-vs-websocket.png)

### Key Characteristics

| Feature | HTTP | WebSocket |
|---------|------|-----------|
| **Direction** | Client → Server (request-response) | Bidirectional (full-duplex) |
| **Connection** | New connection per request (or keep-alive) | Single persistent connection |
| **Overhead** | HTTP headers on every request (~800 bytes) | 2-14 bytes per frame after handshake |
| **Initiation** | Only client can initiate | Either side can send at any time |
| **Protocol** | `http://` / `https://` | `ws://` / `wss://` |
| **Latency** | Higher (connection setup + headers) | Very low (no overhead per message) |

### Why WebSocket Wins for Real-Time

- ✅ **Low latency** — No HTTP header overhead per message
- ✅ **Bidirectional** — Server can push data without client asking
- ✅ **Persistent** — Connection stays open, no reconnection overhead
- ✅ **Efficient** — Tiny frame headers (2-14 bytes vs ~800 bytes for HTTP)
- ✅ **Event-driven** — React to data as it arrives, not by polling

---

## 🤝 How WebSocket Works — The Handshake

WebSocket connections start as a regular HTTP request, then **upgrade** to the WebSocket protocol. This is called the **WebSocket Handshake**.

![WebSocket Handshake Process](./images/websocket-handshake.png)

### Step-by-Step Handshake

#### Step 1: Client Sends an HTTP Upgrade Request

```http
GET /ws HTTP/1.1
Host: example.com
Upgrade: websocket
Connection: Upgrade
Sec-WebSocket-Key: dGhlIHNhbXBsZSBub25jZQ==
Sec-WebSocket-Version: 13
Origin: http://example.com
```

**Important Headers:**

- `Upgrade: websocket` — "I want to switch to WebSocket protocol"
- `Connection: Upgrade` — "I want to upgrade this connection"
- `Sec-WebSocket-Key` — A random Base64-encoded value for security
- `Sec-WebSocket-Version: 13` — The WebSocket protocol version

#### Step 2: Server Responds with 101 Switching Protocols

```http
HTTP/1.1 101 Switching Protocols
Upgrade: websocket
Connection: Upgrade
Sec-WebSocket-Accept: s3pPLMBiTxaQ9kYGzzhZRbK+xOo=
```

**Important Headers:**

- `101 Switching Protocols` — "OK, let's switch!"
- `Sec-WebSocket-Accept` — Computed from the client's key (proves the server understood the request)

#### Step 3: Connection Upgraded

After the handshake, the TCP connection stays open and both sides can send **WebSocket frames** freely. No more HTTP — it's pure WebSocket now.

### How `Sec-WebSocket-Accept` is Computed

The server takes the client's `Sec-WebSocket-Key`, appends a magic GUID (`258EAFA5-E914-47DA-95CA-C5AB0DC85B11`), computes the SHA-1 hash, and Base64 encodes it:

```
Sec-WebSocket-Accept = Base64(SHA1(Sec-WebSocket-Key + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11"))
```

This prevents accidental upgrades from non-WebSocket clients.

---

## 📦 WebSocket Frame Format

After the handshake, data is sent in **frames**. Here's the simplified frame structure:

```
 0                   1                   2                   3
 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
+-+-+-+-+-------+-+-------------+-------------------------------+
|F|R|R|R| opcode|M| Payload len |    Extended payload length    |
|I|S|S|S|  (4)  |A|     (7)     |            (16/64)            |
|N|V|V|V|       |S|             |   (if payload len==126/127)   |
| |1|2|3|       |K|             |                               |
+-+-+-+-+-------+-+-------------+-------------------------------+
|     Masking-key, if MASK set to 1 (4 bytes)                   |
+---------------------------------------------------------------+
|                     Payload Data                               |
+---------------------------------------------------------------+
```

### Frame Types (Opcodes)

| Opcode | Type | Description |
|--------|------|-------------|
| `0x1` | **Text** | UTF-8 text data (most common) |
| `0x2` | **Binary** | Binary data (images, files) |
| `0x8` | **Close** | Connection close request |
| `0x9` | **Ping** | Heartbeat ping (keep-alive) |
| `0xA` | **Pong** | Heartbeat pong (response to ping) |

> 💡 **Key Insight**: The overhead per frame is just **2-14 bytes** compared to HTTP headers which can be **hundreds of bytes**. This is why WebSocket is so efficient for frequent small messages.

---

## 🔒 ws:// vs wss://

| Protocol | Description | Port |
|----------|-------------|------|
| `ws://` | Unencrypted WebSocket | 80 |
| `wss://` | Encrypted WebSocket (over TLS/SSL) | 443 |

**Always use `wss://` in production!** It's the WebSocket equivalent of HTTPS.

```
ws://example.com/chat      ← Unencrypted (development only!)
wss://example.com/chat     ← Encrypted (production)
```

---

## 🌍 Real-World Use Cases

![WebSocket Use Cases](./images/websocket-use-cases.png)

### 1. 💬 Real-Time Chat

Applications like Slack, WhatsApp Web, and Discord use WebSockets for instant message delivery. Every message is pushed to all participants the moment it's sent.

### 2. 📈 Live Financial Data

Stock tickers, cryptocurrency prices, and trading platforms stream real-time price data using WebSockets. Companies like Binance and Bloomberg use WebSocket APIs.

### 3. 🎮 Online Gaming

Multiplayer games need instant state synchronization between players. WebSocket provides the low-latency, bidirectional communication that gaming demands.

### 4. 📡 IoT & Sensor Data

IoT devices stream sensor readings (temperature, GPS, heart rate) to dashboards in real-time. WebSocket's persistent connection model is perfect for this.

### 5. 🔔 Live Notifications

Social media platforms push notifications, comments, and reactions to users in real-time without requiring page refreshes.

### 6. 👥 Collaborative Editing

Google Docs, Figma, and similar tools use WebSocket-like technology to sync edits across multiple users in real-time.

---

## 🏗 WebSocket in the .NET Ecosystem

C# / .NET provides excellent WebSocket support at multiple levels:

| Level | Technology | When to Use |
|-------|-----------|------------|
| **Low-Level** | `System.Net.WebSockets` | When you need full control over the protocol |
| **ASP.NET Core Middleware** | `UseWebSockets()` | Server-side WebSocket handling in web apps |
| **High-Level** | **SignalR** | When you want built-in reconnection, groups, scaling |
| **Client** | `ClientWebSocket` | C# client connecting to a WebSocket server |

### Our Learning Path

```
Chapter 2-6:  Raw WebSocket (ASP.NET Core Middleware)
              └── Learn the fundamentals, understand the protocol

Chapter 7:    SignalR (High-Level Abstraction)
              └── See how .NET simplifies WebSocket development

Chapter 8:    Scaling with Redis
              └── Production patterns for horizontal scaling

Capstone:     Everything combined!
              └── Real-world application with external APIs
```

---

## 🧠 Key Takeaways

1. **WebSocket solves the real-time communication problem** that HTTP wasn't designed for
2. **It starts as HTTP** (the handshake) then upgrades to a persistent, bidirectional connection
3. **Very low overhead** — 2-14 byte frames vs hundreds of bytes for HTTP headers
4. **Full-duplex** — both client and server can send data at any time
5. **Widely supported** — all modern browsers and server frameworks support WebSocket
6. **.NET has excellent support** — from raw `System.Net.WebSockets` to high-level SignalR

---

## ⏭ Next Chapter

**[Chapter 02 — Your First WebSocket Connection →](../Chapter02-FirstConnection/)**

In the next chapter, you'll write your first WebSocket server in C# and connect to it from a browser. Let's get coding! 🚀
