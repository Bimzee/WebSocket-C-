// =============================================================================
// Chapter 05 — Heartbeat & Reconnection
// =============================================================================
// Demonstrates:
//   1. Server-initiated heartbeat (ping/pong) to detect dead connections
//   2. Background service (IHostedService) for periodic health checks
//   3. Application-level ping with response tracking
//   4. Client-side auto-reconnection with exponential backoff
//   5. Connection state management
// =============================================================================

using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Register our heartbeat background service
builder.Services.AddSingleton<ConnectionManager>();
builder.Services.AddHostedService<HeartbeatService>();

var app = builder.Build();

app.UseWebSockets(
    new WebSocketOptions
    {
        KeepAliveInterval = TimeSpan.FromSeconds(30), // Built-in WebSocket-level ping
    }
);
app.UseStaticFiles();

var connManager = app.Services.GetRequiredService<ConnectionManager>();

app.Map(
    "/ws",
    async context =>
    {
        if (!context.WebSockets.IsWebSocketRequest)
        {
            context.Response.StatusCode = 400;
            return;
        }

        using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
        var clientId = Guid.NewGuid().ToString("N")[..8];

        connManager.AddConnection(clientId, webSocket);
        Console.WriteLine($"✅ [{clientId}] Connected. Total: {connManager.Count}");

        await connManager.SendToClient(
            clientId,
            new
            {
                type = "system",
                @event = "welcome",
                clientId,
                message = $"Connected! Your ID: {clientId}. Heartbeat interval: 10s",
                heartbeatInterval = 10,
                serverTime = DateTime.UtcNow,
            }
        );

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
                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var raw = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    await ProcessMessage(connManager, clientId, raw);
                }

                // Mark this connection as alive (it sent us data)
                connManager.MarkAlive(clientId);

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
            connManager.RemoveConnection(clientId);
            Console.WriteLine($"👋 [{clientId}] Disconnected. Total: {connManager.Count}");
        }
    }
);

app.MapGet("/", () => Results.Redirect("/index.html"));

// API endpoint to see connection health status
app.MapGet(
    "/api/health",
    (ConnectionManager cm) =>
        new { totalConnections = cm.Count, connections = cm.GetHealthStatus() }
);

app.Run("http://localhost:5000");

// =============================================================================
// Message Processing
// =============================================================================
async Task ProcessMessage(ConnectionManager cm, string clientId, string raw)
{
    try
    {
        using var doc = JsonDocument.Parse(raw);
        var type = doc.RootElement.GetProperty("type").GetString();

        switch (type)
        {
            case "pong":
                // Client responded to our ping - mark as alive
                cm.MarkAlive(clientId);
                Console.WriteLine($"🏓 [{clientId}] Pong received");
                break;

            case "chat":
                var text = doc.RootElement.GetProperty("text").GetString() ?? "";
                // Broadcast to all
                await cm.BroadcastAll(
                    new
                    {
                        type = "chat",
                        from = clientId,
                        text,
                        timestamp = DateTime.UtcNow,
                    }
                );
                break;

            case "status":
                await cm.SendToClient(
                    clientId,
                    new
                    {
                        type = "status",
                        connections = cm.Count,
                        health = cm.GetHealthStatus(),
                        serverUptime = Environment.TickCount64 / 1000,
                    }
                );
                break;
        }
    }
    catch (Exception ex)
    {
        await cm.SendToClient(clientId, new { type = "error", message = ex.Message });
    }
}

// =============================================================================
// Connection Manager — Tracks connections and their health
// =============================================================================
public class ConnectionManager
{
    private readonly ConcurrentDictionary<string, ClientConnection> _connections = new();
    private readonly JsonSerializerOptions _json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public int Count => _connections.Count;

    public void AddConnection(string id, WebSocket ws)
    {
        _connections.TryAdd(id, new ClientConnection(ws));
    }

    public void RemoveConnection(string id)
    {
        _connections.TryRemove(id, out _);
    }

    public void MarkAlive(string id)
    {
        if (_connections.TryGetValue(id, out var conn))
        {
            conn.LastPongReceived = DateTime.UtcNow;
            conn.MissedPings = 0;
            conn.IsAlive = true;
        }
    }

    public async Task SendPingToAll()
    {
        foreach (var (id, conn) in _connections)
        {
            if (conn.WebSocket.State != WebSocketState.Open)
                continue;

            try
            {
                conn.MissedPings++;
                conn.LastPingSent = DateTime.UtcNow;

                // Send application-level ping (JSON)
                await SendToClient(
                    id,
                    new
                    {
                        type = "ping",
                        timestamp = DateTime.UtcNow,
                        missedPings = conn.MissedPings,
                    }
                );
            }
            catch
            {
                conn.IsAlive = false;
            }
        }
    }

    public async Task CleanupDeadConnections(int maxMissedPings = 3)
    {
        var deadConnections = _connections
            .Where(c =>
                c.Value.MissedPings >= maxMissedPings
                || c.Value.WebSocket.State != WebSocketState.Open
            )
            .ToList();

        foreach (var (id, conn) in deadConnections)
        {
            Console.WriteLine(
                $"💀 [{id}] Dead connection detected (missed {conn.MissedPings} pings). Removing."
            );
            try
            {
                if (conn.WebSocket.State == WebSocketState.Open)
                    await conn.WebSocket.CloseAsync(
                        WebSocketCloseStatus.PolicyViolation,
                        "Heartbeat timeout",
                        CancellationToken.None
                    );
            }
            catch { }
            _connections.TryRemove(id, out _);
        }
    }

    public async Task SendToClient(string id, object message)
    {
        if (
            _connections.TryGetValue(id, out var conn)
            && conn.WebSocket.State == WebSocketState.Open
        )
        {
            var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message, _json));
            await conn.WebSocket.SendAsync(
                new ArraySegment<byte>(bytes),
                WebSocketMessageType.Text,
                true,
                CancellationToken.None
            );
        }
    }

    public async Task BroadcastAll(object message)
    {
        var tasks = _connections
            .Where(c => c.Value.WebSocket.State == WebSocketState.Open)
            .Select(c => SendToClient(c.Key, message));
        await Task.WhenAll(tasks);
    }

    public object[] GetHealthStatus()
    {
        return _connections
            .Select(c => new
            {
                id = c.Key,
                isAlive = c.Value.IsAlive,
                missedPings = c.Value.MissedPings,
                lastPing = c.Value.LastPingSent,
                lastPong = c.Value.LastPongReceived,
                connectedAt = c.Value.ConnectedAt,
                state = c.Value.WebSocket.State.ToString(),
            })
            .ToArray<object>();
    }
}

public class ClientConnection
{
    public WebSocket WebSocket { get; }
    public DateTime ConnectedAt { get; } = DateTime.UtcNow;
    public DateTime? LastPingSent { get; set; }
    public DateTime? LastPongReceived { get; set; }
    public int MissedPings { get; set; }
    public bool IsAlive { get; set; } = true;

    public ClientConnection(WebSocket ws) => WebSocket = ws;
}

// =============================================================================
// Heartbeat Background Service — Runs periodically
// =============================================================================
public class HeartbeatService : BackgroundService
{
    private readonly ConnectionManager _connManager;
    private readonly ILogger<HeartbeatService> _logger;

    public HeartbeatService(ConnectionManager connManager, ILogger<HeartbeatService> logger)
    {
        _connManager = connManager;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("💓 Heartbeat service started (interval: 10s)");

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

            if (_connManager.Count > 0)
            {
                _logger.LogInformation($"💓 Sending ping to {_connManager.Count} client(s)");
                await _connManager.SendPingToAll();

                // Wait a bit for pong responses, then cleanup
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                await _connManager.CleanupDeadConnections(maxMissedPings: 3);
            }
        }
    }
}
