// =============================================================================
// Chapter 03 — Sending & Receiving Messages
// =============================================================================
// This chapter builds on Chapter 02 by introducing:
//   1. Structured JSON message protocol
//   2. Message types (chat, system, command)
//   3. Proper message deserialization with System.Text.Json
//   4. Timestamps and message formatting
//   5. Echo mode and broadcast-ready architecture
// =============================================================================

using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.UseWebSockets();
app.UseStaticFiles();

// JSON serializer options — camelCase for JavaScript compatibility
var jsonOptions = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    WriteIndented = false,
};

app.Map(
    "/ws",
    async context =>
    {
        if (!context.WebSockets.IsWebSocketRequest)
        {
            context.Response.StatusCode = 400;
            await context.Response.WriteAsync("WebSocket connection required.");
            return;
        }

        using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
        var clientId = Guid.NewGuid().ToString("N")[..8]; // Short unique ID
        Console.WriteLine($"✅ Client {clientId} connected");

        // Send a welcome message to the client
        await SendJsonMessage(
            webSocket,
            new
            {
                Type = "system",
                Event = "welcome",
                Message = $"Welcome! Your ID is {clientId}",
                ClientId = clientId,
                Timestamp = DateTime.UtcNow,
            },
            jsonOptions
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
                    var rawMessage = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    Console.WriteLine($"📨 [{clientId}] Raw: {rawMessage}");

                    await ProcessMessage(webSocket, clientId, rawMessage, jsonOptions);
                }
                else if (result.MessageType == WebSocketMessageType.Binary)
                {
                    // Demonstrate handling binary messages
                    Console.WriteLine($"📦 [{clientId}] Binary: {result.Count} bytes");
                    await SendJsonMessage(
                        webSocket,
                        new
                        {
                            Type = "system",
                            Event = "binary_received",
                            Message = $"Received {result.Count} bytes of binary data",
                            Timestamp = DateTime.UtcNow,
                        },
                        jsonOptions
                    );
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
        catch (WebSocketException ex)
        {
            Console.WriteLine($"❌ [{clientId}] WebSocket error: {ex.Message}");
        }

        Console.WriteLine($"👋 Client {clientId} disconnected");
    }
);

app.MapGet("/", () => Results.Redirect("/index.html"));
app.Run("http://localhost:5000");

// =============================================================================
// Message Processing
// =============================================================================

async Task ProcessMessage(
    WebSocket webSocket,
    string clientId,
    string rawMessage,
    JsonSerializerOptions options
)
{
    try
    {
        // Try to parse as JSON
        using var doc = JsonDocument.Parse(rawMessage);
        var root = doc.RootElement;

        // Get the message type
        var type = root.TryGetProperty("type", out var typeProp)
            ? typeProp.GetString() ?? "unknown"
            : "unknown";

        switch (type.ToLower())
        {
            case "chat":
                // Handle chat messages
                var text = root.GetProperty("text").GetString() ?? "";
                await SendJsonMessage(
                    webSocket,
                    new
                    {
                        Type = "chat",
                        From = clientId,
                        Text = text,
                        Timestamp = DateTime.UtcNow,
                        MessageId = Guid.NewGuid().ToString("N")[..8],
                    },
                    options
                );
                break;

            case "command":
                // Handle command messages
                var command = root.GetProperty("command").GetString() ?? "";
                await HandleCommand(webSocket, clientId, command, options);
                break;

            case "ping":
                // Handle custom ping (not WebSocket-level ping)
                await SendJsonMessage(
                    webSocket,
                    new
                    {
                        Type = "pong",
                        Timestamp = DateTime.UtcNow,
                        ServerTime = DateTime.UtcNow.ToString("O"),
                    },
                    options
                );
                break;

            default:
                // Echo unknown types back with info
                await SendJsonMessage(
                    webSocket,
                    new
                    {
                        Type = "error",
                        Message = $"Unknown message type: '{type}'",
                        OriginalMessage = rawMessage,
                        Timestamp = DateTime.UtcNow,
                    },
                    options
                );
                break;
        }
    }
    catch (JsonException)
    {
        // If not valid JSON, treat as plain text and echo
        await SendJsonMessage(
            webSocket,
            new
            {
                Type = "echo",
                OriginalText = rawMessage,
                Message = $"Echo: {rawMessage}",
                Note = "Tip: Send JSON with a 'type' field for structured messaging!",
                Timestamp = DateTime.UtcNow,
            },
            options
        );
    }
}

async Task HandleCommand(
    WebSocket webSocket,
    string clientId,
    string command,
    JsonSerializerOptions options
)
{
    switch (command.ToLower())
    {
        case "time":
            await SendJsonMessage(
                webSocket,
                new
                {
                    Type = "command_response",
                    Command = "time",
                    Result = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC"),
                    Timestamp = DateTime.UtcNow,
                },
                options
            );
            break;

        case "whoami":
            await SendJsonMessage(
                webSocket,
                new
                {
                    Type = "command_response",
                    Command = "whoami",
                    Result = $"You are client: {clientId}",
                    Timestamp = DateTime.UtcNow,
                },
                options
            );
            break;

        case "help":
            await SendJsonMessage(
                webSocket,
                new
                {
                    Type = "command_response",
                    Command = "help",
                    Result = "Available commands: time, whoami, help",
                    AvailableTypes = new[] { "chat", "command", "ping" },
                    Timestamp = DateTime.UtcNow,
                },
                options
            );
            break;

        default:
            await SendJsonMessage(
                webSocket,
                new
                {
                    Type = "error",
                    Message = $"Unknown command: '{command}'. Type 'help' for available commands.",
                    Timestamp = DateTime.UtcNow,
                },
                options
            );
            break;
    }
}

// =============================================================================
// Helper: Send a JSON-serialized object as a WebSocket text message
// =============================================================================
async Task SendJsonMessage(WebSocket webSocket, object message, JsonSerializerOptions options)
{
    if (webSocket.State != WebSocketState.Open)
        return;

    var json = JsonSerializer.Serialize(message, options);
    var bytes = Encoding.UTF8.GetBytes(json);

    await webSocket.SendAsync(
        new ArraySegment<byte>(bytes),
        WebSocketMessageType.Text,
        endOfMessage: true,
        CancellationToken.None
    );
}
