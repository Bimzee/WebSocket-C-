// =============================================================================
// Chapter 02 — Your First WebSocket Connection
// =============================================================================
// This is the simplest possible WebSocket server in ASP.NET Core.
// It demonstrates:
//   1. Enabling WebSocket middleware
//   2. Accepting WebSocket connections
//   3. Receiving and sending messages in a loop
//   4. Handling connection close gracefully
// =============================================================================

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// -----------------------------------------------------------------------------
// STEP 1: Enable WebSocket support
// -----------------------------------------------------------------------------
// The UseWebSockets() middleware enables the server to accept WebSocket
// upgrade requests. Without this, any attempt to upgrade will fail.
// You can configure options like KeepAliveInterval here.
app.UseWebSockets(
    new WebSocketOptions
    {
        KeepAliveInterval = TimeSpan.FromSeconds(30), // Server sends ping every 30s
    }
);

// Serve static files (our client HTML page) from the wwwroot folder
app.UseStaticFiles();

// -----------------------------------------------------------------------------
// STEP 2: Define the WebSocket endpoint
// -----------------------------------------------------------------------------
// We map a specific path "/ws" for WebSocket connections.
// When a client connects to ws://localhost:5000/ws, this code handles it.
app.Map(
    "/ws",
    async context =>
    {
        // Check if this is a WebSocket upgrade request
        if (context.WebSockets.IsWebSocketRequest)
        {
            // STEP 3: Accept the WebSocket connection
            // This completes the HTTP 101 Switching Protocols handshake
            using var webSocket = await context.WebSockets.AcceptWebSocketAsync();

            Console.WriteLine($"✅ Client connected! Remote: {context.Connection.RemoteIpAddress}");

            // STEP 4: Enter the receive loop
            // We continuously listen for messages from the client
            var buffer = new byte[1024 * 4]; // 4 KB buffer for incoming messages

            // ReceiveAsync will block until a message is received or the connection closes
            var receiveResult = await webSocket.ReceiveAsync(
                new ArraySegment<byte>(buffer),
                CancellationToken.None
            );

            // Keep receiving messages until the client sends a Close frame
            while (!receiveResult.CloseStatus.HasValue)
            {
                // Decode the received message
                var receivedMessage = System.Text.Encoding.UTF8.GetString(
                    buffer,
                    0,
                    receiveResult.Count
                );
                Console.WriteLine($"📨 Received: {receivedMessage}");

                // STEP 5: Echo the message back to the client
                // SendAsync sends data back through the WebSocket
                var responseMessage = $"Server received messages: {receivedMessage}";
                var responseBytes = System.Text.Encoding.UTF8.GetBytes(responseMessage);

                await webSocket.SendAsync(
                    new ArraySegment<byte>(responseBytes),
                    System.Net.WebSockets.WebSocketMessageType.Text, // We're sending text
                    endOfMessage: true, // This is a complete message
                    CancellationToken.None
                );

                // Wait for the next message
                receiveResult = await webSocket.ReceiveAsync(
                    new ArraySegment<byte>(buffer),
                    CancellationToken.None
                );
            }

            // STEP 6: Handle graceful close
            // When the client sends a Close frame, we respond with our own Close frame
            Console.WriteLine(
                $"👋 Client disconnecting. Reason: {receiveResult.CloseStatusDescription}"
            );

            await webSocket.CloseAsync(
                receiveResult.CloseStatus.Value,
                receiveResult.CloseStatusDescription,
                CancellationToken.None
            );

            Console.WriteLine("❌ Connection closed.");
        }
        else
        {
            // If someone tries to access /ws without a WebSocket upgrade, return 400
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsync("This endpoint requires a WebSocket connection.");
        }
    }
);

// Fallback: serve index.html for the root path
app.MapGet("/", () => Results.Redirect("/index.html"));

// Run the server on port 5000
app.Run("http://localhost:5000");
