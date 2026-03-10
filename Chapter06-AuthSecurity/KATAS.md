# 🥋 Chapter 06 — Code Katas: Authentication & Security

> **Type**: Hands-on coding exercises  
> **Goal**: Master JWT authentication, origin validation, and rate limiting for WebSocket connections

---

## 🟢 Kata 1: Basic Token Gate (Easy)

**Objective**: Reject WebSocket connections that don't have a token.

**Requirements:**

1. Require a `token` query parameter: `ws://localhost:5000/ws?token=SECRET`
2. If the token equals `"SECRET123"` → accept the connection
3. If the token is missing or wrong → respond with HTTP **401 Unauthorized**
4. After accepting, send a welcome message: `{ "type": "welcome", "message": "Authenticated!" }`

**Test it:**

```javascript
// Should work:
new WebSocket('ws://localhost:5000/ws?token=SECRET123');

// Should fail (401):
new WebSocket('ws://localhost:5000/ws');
new WebSocket('ws://localhost:5000/ws?token=WRONG');
```

<details>
<summary>💡 Hints</summary>

- Access query params: `context.Request.Query["token"]`
- Check the token BEFORE calling `AcceptWebSocketAsync()`
- If unauthorized: `context.Response.StatusCode = 401; return;`

</details>

---

## 🟢 Kata 2: JWT Token Generator (Easy)

**Objective**: Build a REST endpoint that generates JWT tokens.

**Requirements:**

1. Create `POST /api/login` that accepts `{ "username": "alice", "password": "pass123" }`
2. Validate against a hardcoded user dictionary
3. Return a JWT token with claims: `sub` (username), `role` (admin/user), `exp` (1 hour)
4. Return **401** for invalid credentials
5. Use `Microsoft.AspNetCore.Authentication.JwtBearer` NuGet package

**Hardcoded users:**

```csharp
var users = new Dictionary<string, (string Password, string Role)>
{
    ["alice"] = ("pass123", "admin"),
    ["bob"] = ("pass456", "user"),
    ["charlie"] = ("pass789", "user")
};
```

**Response format:**

```json
{ "token": "eyJhbGciOi...", "expiresAt": "2024-01-01T13:00:00Z", "role": "admin" }
```

---

## 🟡 Kata 3: JWT-Authenticated WebSocket (Medium)

**Objective**: Combine the JWT login with WebSocket authentication.

**Requirements:**

1. Use your JWT generator from Kata 2
2. The WebSocket endpoint reads the token from query params: `ws://localhost:5000/ws?token=eyJ...`
3. Validate the JWT signature, expiration, and issuer
4. Extract the username and role from the token claims
5. Include the authenticated user info in all messages:

   ```json
   { "type": "chat", "from": "alice", "role": "admin", "text": "Hello!" }
   ```

6. If the token is expired, return **401** with a message

<details>
<summary>💡 Hints</summary>

- Use `JwtSecurityTokenHandler` to validate tokens
- Set `TokenValidationParameters` with your signing key, issuer, and audience
- After validation, extract claims: `principal.FindFirst(ClaimTypes.Name)?.Value`
- Wrap validation in try-catch — `SecurityTokenExpiredException` specifically for expired tokens

</details>

---

## 🟡 Kata 4: Origin Validation (Medium)

**Objective**: Prevent cross-site WebSocket hijacking.

**Requirements:**

1. Read the `Origin` header from the WebSocket upgrade request
2. Define an allowlist of origins: `["http://localhost:5000", "https://myapp.com"]`
3. Reject connections from unknown origins with **403 Forbidden**
4. Log all rejected origins (potential attacks)
5. Make the allowlist configurable (read from `appsettings.json`)
6. In development mode, optionally allow all origins (with a warning log)

**appsettings.json:**

```json
{
  "WebSocket": {
    "AllowedOrigins": ["http://localhost:5000", "https://myapp.com"],
    "AllowAllOriginsInDev": true
  }
}
```

---

## 🟡 Kata 5: Rate Limiter (Medium)

**Objective**: Prevent message spam with a sliding window rate limiter.

**Requirements:**

1. Limit each client to **20 messages per minute**
2. Use a **sliding window** algorithm (not a fixed window)
3. When the limit is hit, send: `{ "type": "rate_limited", "retryAfterMs": 5000 }`
4. Do NOT close the connection — just reject excess messages
5. After **5 consecutive rate-limit violations**, disconnect with a warning
6. Log rate-limited clients on the server

**Sliding window implementation:**

```csharp
class SlidingWindowRateLimiter
{
    private readonly Queue<DateTime> _timestamps = new();
    private readonly int _maxRequests;
    private readonly TimeSpan _window;

    public bool IsAllowed()
    {
        var now = DateTime.UtcNow;
        // Remove timestamps outside the window
        while (_timestamps.Count > 0 && _timestamps.Peek() < now - _window)
            _timestamps.Dequeue();
        
        if (_timestamps.Count >= _maxRequests)
            return false;
        
        _timestamps.Enqueue(now);
        return true;
    }
}
```

---

## 🔴 Kata 6: Role-Based Message Filtering (Hard)

**Objective**: Implement fine-grained permissions based on JWT roles.

**Requirements:**

1. Define permissions per role:

   | Action | `admin` | `moderator` | `user` | `guest` |
   |--------|---------|-------------|--------|---------|
   | Send chat | ✅ | ✅ | ✅ | ❌ |
   | Create room | ✅ | ✅ | ❌ | ❌ |
   | Delete room | ✅ | ❌ | ❌ | ❌ |
   | Kick user | ✅ | ✅ | ❌ | ❌ |
   | View admin stats | ✅ | ❌ | ❌ | ❌ |
   | Read messages | ✅ | ✅ | ✅ | ✅ |

2. Check permissions before executing any command
3. Return `{ "type": "forbidden", "action": "...", "requiredRole": "..." }` for unauthorized actions
4. Admin can impersonate another user (send messages as them)
5. Log all permission-denied actions as security events

<details>
<summary>💡 Hints</summary>

- Create a `PermissionMatrix` class with a `Dictionary<string, HashSet<string>>` (role → allowed actions)
- Extract the role from JWT claims at connection time
- Create a middleware-like check: `if (!permissions.IsAllowed(role, action)) { ... }`
- Consider using policy-based authorization with custom requirements

</details>

---

## 🔴 Kata 7: Full Security Suite (Hard)

**Objective**: Combine all security features into a hardened WebSocket server.

**Requirements:**

1. **Authentication**: JWT-based login with token refresh
2. **Origin validation**: Configurable allowlist
3. **Rate limiting**: 30 messages/minute per user
4. **Input validation**: Reject messages over 2KB, sanitize HTML/XSS
5. **Audit logging**: Log all connections, disconnections, auth failures, and rate limits to a file
6. **Token refresh**: When a token is about to expire (< 5 min left), send `{ "type": "token_expiring", "expiresIn": 300 }`
7. **Brute-force protection**: Lock out IP after 5 failed login attempts for 15 minutes

**Audit log format (JSON lines):**

```json
{"timestamp":"...","event":"auth_success","user":"alice","ip":"127.0.0.1"}
{"timestamp":"...","event":"auth_failure","user":"hacker","ip":"10.0.0.5","reason":"invalid_password"}
{"timestamp":"...","event":"rate_limited","user":"alice","messageCount":31}
{"timestamp":"...","event":"connection_closed","user":"alice","reason":"token_expired"}
```

<details>
<summary>💡 Hints</summary>

- Use `IHttpContextAccessor` to get the client's IP address
- Use `ConcurrentDictionary<string, (int Attempts, DateTime LockoutUntil)>` for brute-force tracking
- For HTML sanitization, use `System.Web.HttpUtility.HtmlEncode()` or a library like HtmlSanitizer
- Write audit logs using `ILogger` with a file provider, or append to a `.jsonl` file
- Use a background timer to check token expiration and warn clients proactively

</details>
