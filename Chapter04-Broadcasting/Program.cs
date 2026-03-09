// =============================================================================
// Chapter 04 — Broadcasting & Groups
// =============================================================================
// This chapter demonstrates:
//   1. Managing multiple WebSocket connections
//   2. Broadcasting messages to all clients
//   3. Group (room) based messaging
//   4. Direct (private) messaging between clients
//   5. Thread-safe connection tracking
// =============================================================================

using Chapter04_Broadcasting;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.UseWebSockets();
app.UseStaticFiles();

// Create a single instance of our WebSocket handler
// This tracks ALL connections and groups across the application
var handler = new WebSocketHandler();

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
        var clientId = Guid.NewGuid().ToString("N")[..8];

        // Delegate all handling to the WebSocketHandler
        await handler.HandleConnection(webSocket, clientId);
    }
);

app.MapGet("/", () => Results.Redirect("/index.html"));
app.Run("http://localhost:5000");
