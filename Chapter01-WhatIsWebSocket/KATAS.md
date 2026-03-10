# 🥋 Chapter 01 — Code Katas: WebSocket Theory

> **Type**: Theory & conceptual exercises  
> **Goal**: Solidify your understanding of WebSocket fundamentals before writing code

---

## 🟢 Kata 1: HTTP vs WebSocket — Spot the Difference (Easy)

**Objective**: Identify which scenarios benefit from WebSocket vs HTTP.

For each scenario below, decide: **HTTP** or **WebSocket**? Write your answer and a one-sentence justification.

| # | Scenario | Your Answer | Why? |
|---|----------|-------------|------|
| 1 | Loading a user's profile page | | |
| 2 | Live stock ticker updating every 100ms | | |
| 3 | Submitting a contact form | | |
| 4 | Collaborative document editing (Google Docs style) | | |
| 5 | Downloading a PDF report | | |
| 6 | Multiplayer game state sync | | |
| 7 | Fetching a list of blog posts | | |
| 8 | Push notifications for a social media app | | |

<details>
<summary>✅ Answers</summary>

1. **HTTP** — One-time data fetch, no real-time updates needed
2. **WebSocket** — Continuous stream of frequent data from server
3. **HTTP** — Simple request-response, one-time action
4. **WebSocket** — Bidirectional real-time sync between multiple users
5. **HTTP** — One-time file download
6. **WebSocket** — Low-latency bidirectional communication required
7. **HTTP** — Static data fetch, no live updates
8. **WebSocket** — Server needs to push data to client without polling

</details>

---

## 🟢 Kata 2: The Handshake (Easy)

**Objective**: Reconstruct a WebSocket handshake from memory.

Without looking at the README, write out:

1. The **HTTP method** used in the handshake request
2. The **4 important headers** the client sends
3. The **HTTP status code** the server responds with
4. The **formula** for computing `Sec-WebSocket-Accept`
5. What happens **after** the handshake completes

<details>
<summary>✅ Answers</summary>

1. `GET`
2. `Upgrade: websocket`, `Connection: Upgrade`, `Sec-WebSocket-Key: <base64>`, `Sec-WebSocket-Version: 13`
3. `101 Switching Protocols`
4. `Base64(SHA1(Sec-WebSocket-Key + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11"))`
5. The TCP connection stays open and both sides communicate using WebSocket frames (no more HTTP)

</details>

---

## 🟡 Kata 3: Frame Format Quiz (Medium)

**Objective**: Test your knowledge of WebSocket frame structure.

Answer the following:

1. What is the overhead per WebSocket frame (in bytes)?
2. Match each opcode to its type:

| Opcode | Type |
|--------|------|
| `0x1` | ? |
| `0x2` | ? |
| `0x8` | ? |
| `0x9` | ? |
| `0xA` | ? |

1. Why is WebSocket frame overhead so much smaller than HTTP headers?
2. What is the purpose of the **MASK** bit in the frame?
3. How many bytes can a single frame carry if the payload length field is 7 bits?

<details>
<summary>✅ Answers</summary>

1. **2-14 bytes** (vs ~800 bytes for HTTP headers)
2. `0x1` = Text, `0x2` = Binary, `0x8` = Close, `0x9` = Ping, `0xA` = Pong
3. After the initial handshake, there's no need to resend HTTP method, URL, cookies, etc. — the connection is already established
4. Client-to-server frames are masked (XOR'd with a 4-byte key) to prevent cache poisoning attacks on intermediary proxies
5. 125 bytes (values 126 and 127 trigger extended payload length fields)

</details>

---

## 🟡 Kata 4: Protocol Comparison Table (Medium)

**Objective**: Build a comparison table from memory.

Create a table comparing these 5 communication techniques:

- Polling
- Long Polling
- Server-Sent Events (SSE)
- WebSocket
- HTTP (standard)

Your table should have columns for:

- **Direction** (unidirectional / bidirectional)
- **Connection** (persistent / per-request)
- **Overhead** (high / medium / low)
- **Server Push?** (yes / no)
- **Best Use Case**

<details>
<summary>✅ Answer</summary>

| Technique | Direction | Connection | Overhead | Server Push? | Best Use Case |
|-----------|-----------|------------|----------|-------------|---------------|
| HTTP | Client → Server | Per-request | High | No | REST APIs, page loads |
| Polling | Client → Server | Per-request | High | No (simulated) | Simple dashboards |
| Long Polling | Client → Server | Held open | Medium | Simulated | Chat (legacy) |
| SSE | Server → Client | Persistent | Low | Yes (one-way) | Live feeds, notifications |
| WebSocket | Bidirectional | Persistent | Very Low | Yes | Chat, gaming, live data |

</details>

---

## 🟡 Kata 5: ws:// vs wss:// Security (Medium)

**Objective**: Understand the security implications of each protocol.

1. What port does `ws://` use by default?
2. What port does `wss://` use by default?
3. What does the extra "s" stand for?
4. Why should you **never use `ws://` in production**? Give 2 reasons.
5. What is the HTTP equivalent of `wss://`?
6. If a user connects via `ws://` on a public Wi-Fi, what attacks are they vulnerable to?

<details>
<summary>✅ Answers</summary>

1. Port **80**
2. Port **443**
3. **Secure** (TLS/SSL encrypted)
4. (a) Data is transmitted in plaintext — anyone on the network can read messages. (b) No server identity verification — vulnerable to man-in-the-middle attacks
5. `https://`
6. Packet sniffing (reading messages), man-in-the-middle (modifying messages), session hijacking

</details>

---

## 🔴 Kata 6: Architecture Decision (Hard)

**Objective**: Choose the right .NET technology for real-world scenarios.

For each scenario, choose the most appropriate .NET WebSocket technology and justify your choice:

| Level | Technology |
|-------|-----------|
| Low-Level | `System.Net.WebSockets` |
| ASP.NET Core Middleware | `UseWebSockets()` |
| High-Level | SignalR |
| Client | `ClientWebSocket` |

**Scenarios:**

1. You're building a chat app and need quick development with groups and reconnection
2. You're creating a custom binary protocol for a game server
3. You need a C# service that connects to a third-party WebSocket API
4. You're building a real-time dashboard and need to support browsers that don't have WebSocket
5. You're building a microservice that communicates with another service over WebSocket

**For each, write**: Technology choice + 1-2 sentence justification.

<details>
<summary>✅ Answers</summary>

1. **SignalR** — Built-in groups, auto-reconnect, and serialization eliminate boilerplate
2. **`UseWebSockets()` + `System.Net.WebSockets`** — Need full control over frame types and binary data
3. **`ClientWebSocket`** — It's the .NET client for connecting to external WebSocket servers
4. **SignalR** — Transport fallback (WebSocket → SSE → Long Polling) ensures all browsers work
5. **`ClientWebSocket`** — For a service-to-service connection, you need the client-side library

</details>

---

## 🔴 Kata 7: Design a Real-Time System (Hard)

**Objective**: Apply your theory knowledge to design a system.

You're asked to design a **real-time auction platform** where:

- Users can bid on items in real-time
- All users watching an item see bids instantly
- The auctioneer can start/stop auctions
- A countdown timer syncs across all clients

**Write a design document (1 page) covering:**

1. Why WebSocket is the right choice (vs polling, SSE)
2. The message types you'd define (JSON protocol)
3. Whether you'd use `ws://` or `wss://` and why
4. Whether you'd use raw WebSocket or SignalR and why
5. How you'd handle a user whose connection drops mid-auction

<details>
<summary>💡 Hints</summary>

- Think about bidirectional needs: clients send bids, server pushes updates
- Consider the timer sync problem — who is the source of truth?
- What happens if a winning bidder disconnects?
- Think about groups — each auction item could be a "room"

</details>
