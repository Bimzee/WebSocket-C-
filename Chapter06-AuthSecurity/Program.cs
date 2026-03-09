// =============================================================================
// Chapter 06 — Authentication & Security
// =============================================================================
// Demonstrates:
//   1. JWT token generation on login
//   2. Token validation during WebSocket upgrade
//   3. Origin validation middleware
//   4. Rate limiting for WebSocket messages
//   5. Secure WebSocket patterns
// =============================================================================

using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Net.WebSockets;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.UseWebSockets();
app.UseStaticFiles();

// JWT Configuration
const string JwtSecret =
    "WebSocket-Learning-Project-Super-Secret-Key-2024-Must-Be-At-Least-32-Bytes!";
const string JwtIssuer = "WebSocket-Chapter06";
var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
var connections = new ConcurrentDictionary<string, (WebSocket Socket, string Username)>();

// =========================================================================
// Mock user database (in production, use a real database!)
// =========================================================================
var users = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
{
    { "alice", "password123" },
    { "bob", "password456" },
    { "charlie", "password789" },
    { "demo", "demo" },
};

// =========================================================================
// LOGIN API — Issues JWT tokens
// =========================================================================
app.MapPost(
    "/api/login",
    async (HttpContext ctx) =>
    {
        var body = await JsonSerializer.DeserializeAsync<JsonElement>(ctx.Request.Body);
        var username = body.GetProperty("username").GetString() ?? "";
        var password = body.GetProperty("password").GetString() ?? "";

        // Validate credentials
        if (!users.TryGetValue(username, out var storedPassword) || storedPassword != password)
        {
            ctx.Response.StatusCode = 401;
            await ctx.Response.WriteAsJsonAsync(new { error = "Invalid username or password" });
            return;
        }

        // Generate JWT token
        var token = GenerateJwtToken(username);
        Console.WriteLine($"🔑 Token issued for: {username}");

        await ctx.Response.WriteAsJsonAsync(
            new
            {
                token,
                username,
                expiresIn = 3600, // 1 hour
                message = "Login successful! Use this token to connect via WebSocket.",
            }
        );
    }
);

// =========================================================================
// WEBSOCKET ENDPOINT — Requires valid JWT
// =========================================================================
app.Map(
    "/ws",
    async (HttpContext ctx) =>
    {
        if (!ctx.WebSockets.IsWebSocketRequest)
        {
            ctx.Response.StatusCode = 400;
            await ctx.Response.WriteAsync("WebSocket connection required.");
            return;
        }

        // -----------------------------------------------------------------
        // STEP 1: Extract the JWT token
        // -----------------------------------------------------------------
        // Three common strategies for passing the token:
        //   a) Query parameter: ws://server/ws?token=JWT_TOKEN    ← We use this
        //   b) First message: Connect, then send token as first message
        //   c) Cookie: Set during HTTP login, sent automatically
        //
        // Note: WebSocket does NOT support custom HTTP headers from the browser,
        // so Authorization: Bearer <token> header does NOT work for browser clients!

        var token = ctx.Request.Query["token"].ToString();

        if (string.IsNullOrEmpty(token))
        {
            Console.WriteLine("❌ No token provided");
            ctx.Response.StatusCode = 401;
            await ctx.Response.WriteAsync("Authentication required. Provide ?token=JWT");
            return;
        }

        // -----------------------------------------------------------------
        // STEP 2: Validate the JWT token
        // -----------------------------------------------------------------
        var principal = ValidateJwtToken(token);
        if (principal == null)
        {
            Console.WriteLine("❌ Invalid or expired token");
            ctx.Response.StatusCode = 401;
            await ctx.Response.WriteAsync("Invalid or expired token.");
            return;
        }

        var username = principal.FindFirst(ClaimTypes.Name)?.Value ?? "unknown";
        var role = principal.FindFirst(ClaimTypes.Role)?.Value ?? "user";

        // -----------------------------------------------------------------
        // STEP 3: Origin Validation (optional but recommended)
        // -----------------------------------------------------------------
        var origin = ctx.Request.Headers.Origin.ToString();
        var allowedOrigins = new[] { "http://localhost:5000", "https://localhost:5001", "" };
        if (!string.IsNullOrEmpty(origin) && !allowedOrigins.Contains(origin))
        {
            Console.WriteLine($"❌ Origin rejected: {origin}");
            ctx.Response.StatusCode = 403;
            await ctx.Response.WriteAsync("Origin not allowed.");
            return;
        }

        // -----------------------------------------------------------------
        // STEP 4: Accept the authenticated connection
        // -----------------------------------------------------------------
        using var webSocket = await ctx.WebSockets.AcceptWebSocketAsync();
        var connectionId = Guid.NewGuid().ToString("N")[..8];
        connections.TryAdd(connectionId, (webSocket, username));

        Console.WriteLine($"✅ [{username}] Connected (role: {role}, id: {connectionId})");

        // Send welcome with user info
        await SendJson(
            webSocket,
            new
            {
                type = "system",
                @event = "authenticated",
                username,
                role,
                connectionId,
                message = $"Welcome, {username}! You are authenticated with role: {role}",
            }
        );

        // Notify others
        foreach (var (id, conn) in connections.Where(c => c.Key != connectionId))
        {
            await SendJson(
                conn.Socket,
                new
                {
                    type = "system",
                    @event = "user_joined",
                    username,
                    message = $"{username} joined the chat",
                }
            );
        }

        // Rate limiting state
        var messageTimestamps = new Queue<DateTime>();
        const int maxMessagesPerMinute = 30;

        // Receive loop
        var buffer = new byte[1024 * 4];
        try
        {
            var result = await webSocket.ReceiveAsync(
                new ArraySegment<byte>(buffer),
                CancellationToken.None
            );
            while (!result.CloseStatus.HasValue)
            {
                var raw = Encoding.UTF8.GetString(buffer, 0, result.Count);

                // ---------------------------------------------------------
                // STEP 5: Rate Limiting
                // ---------------------------------------------------------
                var now = DateTime.UtcNow;
                while (
                    messageTimestamps.Count > 0
                    && (now - messageTimestamps.Peek()).TotalMinutes >= 1
                )
                    messageTimestamps.Dequeue();

                if (messageTimestamps.Count >= maxMessagesPerMinute)
                {
                    await SendJson(
                        webSocket,
                        new
                        {
                            type = "error",
                            message = $"Rate limited! Max {maxMessagesPerMinute} messages per minute.",
                            retryAfter = 60 - (int)(now - messageTimestamps.Peek()).TotalSeconds,
                        }
                    );
                }
                else
                {
                    messageTimestamps.Enqueue(now);
                    await ProcessMessage(webSocket, connectionId, username, raw);
                }

                result = await webSocket.ReceiveAsync(
                    new ArraySegment<byte>(buffer),
                    CancellationToken.None
                );
            }
            await webSocket.CloseAsync(
                result.CloseStatus.Value,
                result.CloseStatusDescription,
                CancellationToken.None
            );
        }
        catch (WebSocketException) { }
        finally
        {
            connections.TryRemove(connectionId, out _);
            Console.WriteLine($"👋 [{username}] Disconnected");
            foreach (var (_, conn) in connections)
            {
                await SendJson(
                    conn.Socket,
                    new
                    {
                        type = "system",
                        @event = "user_left",
                        username,
                        message = $"{username} left the chat",
                    }
                );
            }
        }
    }
);

app.MapGet("/", () => Results.Redirect("/index.html"));
app.Run("http://localhost:5000");

// =============================================================================
// JWT Helpers
// =============================================================================
string GenerateJwtToken(string username)
{
    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtSecret));
    var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

    var claims = new[]
    {
        new Claim(ClaimTypes.Name, username),
        new Claim(ClaimTypes.Role, username == "alice" ? "admin" : "user"),
        new Claim("sub", username),
        new Claim("iat", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()),
    };

    var token = new JwtSecurityToken(
        issuer: JwtIssuer,
        audience: JwtIssuer,
        claims: claims,
        expires: DateTime.UtcNow.AddHours(1),
        signingCredentials: credentials
    );

    return new JwtSecurityTokenHandler().WriteToken(token);
}

ClaimsPrincipal? ValidateJwtToken(string token)
{
    try
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtSecret));
        var handler = new JwtSecurityTokenHandler();

        var principal = handler.ValidateToken(
            token,
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = JwtIssuer,
                ValidateAudience = true,
                ValidAudience = JwtIssuer,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ClockSkew = TimeSpan.FromMinutes(1),
            },
            out _
        );

        return principal;
    }
    catch
    {
        return null;
    }
}

async Task ProcessMessage(WebSocket ws, string connId, string username, string raw)
{
    try
    {
        using var doc = JsonDocument.Parse(raw);
        var type = doc.RootElement.GetProperty("type").GetString();

        if (type == "chat")
        {
            var text = doc.RootElement.GetProperty("text").GetString() ?? "";
            var msg = new
            {
                type = "chat",
                from = username,
                text,
                timestamp = DateTime.UtcNow,
            };
            foreach (var (_, conn) in connections)
                await SendJson(conn.Socket, msg);
        }
    }
    catch (Exception ex)
    {
        await SendJson(ws, new { type = "error", message = ex.Message });
    }
}

async Task SendJson(WebSocket ws, object msg)
{
    if (ws.State != WebSocketState.Open)
        return;
    var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(msg, jsonOptions));
    await ws.SendAsync(
        new ArraySegment<byte>(bytes),
        WebSocketMessageType.Text,
        true,
        CancellationToken.None
    );
}
