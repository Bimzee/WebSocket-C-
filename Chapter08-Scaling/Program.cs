// =============================================================================
// Chapter 08 — Scaling WebSockets with Redis Backplane
// =============================================================================
// Demonstrates:
//   1. The horizontal scaling problem for WebSockets
//   2. Redis as a message backplane (pub/sub)
//   3. SignalR Redis backplane configuration
//   4. Running multiple server instances
//   5. Cross-server message delivery
//
// THE PROBLEM:
// When you have 2+ server instances behind a load balancer:
//   Client A → Server 1  (connected)
//   Client B → Server 2  (connected)
//   Client A sends message → Server 1 broadcasts...
//   ...but Server 1 doesn't know about Client B on Server 2!
//
// THE SOLUTION:
// Redis Pub/Sub acts as a message bus between all server instances.
//   Server 1 publishes → Redis → Server 2 receives → delivers to Client B
// =============================================================================

using Chapter08_Scaling.Hubs;

var builder = WebApplication.CreateBuilder(args);

// Read port from command line: dotnet run -- --port 5001
var port = args.FirstOrDefault(a => a.StartsWith("--port="))?.Split("=")[1] ?? "5000";

// Add SignalR with Redis backplane
builder
    .Services.AddSignalR()
    .AddStackExchangeRedis(
        "localhost:6379",
        options =>
        {
            options.Configuration.ChannelPrefix = new StackExchange.Redis.RedisChannel(
                "WebSocketScaling",
                StackExchange.Redis.RedisChannel.PatternMode.Literal
            );
        }
    );

// Add CORS to allow cross-origin connections (needed for multi-server demo)
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy
            .WithOrigins("http://localhost:5000", "http://localhost:5001", "http://localhost:5002")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var app = builder.Build();
app.UseCors();
app.UseStaticFiles();
app.MapHub<ScalingHub>("/scalingHub");
app.MapGet("/", () => Results.Redirect("/index.html"));
app.MapGet(
    "/api/info",
    () =>
        new
        {
            port,
            serverId = Environment.MachineName,
            pid = Environment.ProcessId,
        }
);

Console.WriteLine($"🚀 Server starting on port {port}...");
app.Run($"http://localhost:{port}");
