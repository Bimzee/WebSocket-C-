# 🔐 Chapter 06 — Authentication & Security

> **Goal**: Secure WebSocket connections with JWT authentication, origin validation, and rate limiting  
> **Concepts**: JWT tokens, query parameter auth, origin validation, rate limiting

---

## 🎯 What You'll Learn

- JWT token generation and validation in C#
- Authenticating WebSocket connections during the HTTP upgrade
- Why browsers can't send custom headers for WebSocket (and the workaround)
- Origin validation to prevent cross-site attacks
- Rate limiting WebSocket messages

---

## 📊 Authentication Flow

![Authentication Flow](./images/auth-flow.png)

---

## 🔑 Key Concepts

### The WebSocket Auth Challenge

Unlike REST APIs where you add `Authorization: Bearer <token>` headers, **browsers cannot send custom HTTP headers during the WebSocket handshake**. This is a fundamental limitation of the browser `WebSocket` API.

### Three Authentication Strategies

| Strategy | How | Pros | Cons |
|----------|-----|------|------|
| **Query Parameter** ✅ | `ws://server/ws?token=JWT` | Simple, works everywhere | Token visible in URLs/logs |
| **Cookie** | Set during HTTP login, auto-sent | Automatic, invisible | CSRF concerns, cookie management |
| **First Message** | Connect first, then send token | Clean URL | Server must handle unauthenticated state |

We use the **query parameter** approach — it's the most common for learning and practical use.

### Security Checklist

- ✅ **Use WSS** (TLS) in production — encrypts the token in transit
- ✅ **Short-lived tokens** — JWT expires in 1 hour
- ✅ **Origin validation** — reject connections from unknown origins
- ✅ **Rate limiting** — prevent abuse (30 msg/min in our example)
- ✅ **Input validation** — always validate incoming messages

---

## 📁 Files in This Chapter

| File | Purpose |
|------|---------|
| `Program.cs` | Server with JWT auth, origin validation, rate limiting |
| `wwwroot/index.html` | Login form + authenticated chat interface |

---

## 🚀 How to Run

```bash
cd Chapter06-AuthSecurity
dotnet restore
dotnet run
```

Open **<http://localhost:5000>** and login with one of the demo accounts.

### Demo Accounts

| Username | Password | Role |
|----------|----------|------|
| `alice` | `password123` | admin |
| `bob` | `password456` | user |
| `demo` | `demo` | user |

### What to Try

1. **Login** with different accounts — see different roles
2. **Open multiple tabs** — login as different users, chat in real-time
3. **Expand the JWT token** — click on it to see the full Base64 string
4. **Try wrong password** — see the 401 error
5. **Spam messages** — hit the rate limit after 30 messages/minute

---

## 🧠 Key Takeaways

1. **Browsers can't send custom WebSocket headers** — use query params or cookies
2. **Validate the token BEFORE `AcceptWebSocketAsync()`** — reject unauthenticated connections at the HTTP level
3. **Origin validation** prevents cross-site WebSocket hijacking
4. **Rate limiting** protects against abuse and DOS attacks
5. **Always use `wss://` in production** to encrypt tokens in transit

---

## ⏭ Next Chapter

**[Chapter 07 — SignalR →](../Chapter07-SignalR/)**

Next, we'll explore SignalR — .NET's high-level real-time framework that handles all of this for you!
