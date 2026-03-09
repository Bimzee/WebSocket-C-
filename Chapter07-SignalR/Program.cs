// =============================================================================
// Chapter 07 — SignalR Server
// =============================================================================
// Compare this to previous chapters' Program.cs files:
//   - No UseWebSockets() middleware
//   - No byte buffer management
//   - No JSON serialization boilerplate
//   - Just AddSignalR() + MapHub() — SignalR handles the rest!
// =============================================================================

using Chapter07_SignalR.Hubs;

var builder = WebApplication.CreateBuilder(args);

// One line to add SignalR services!
builder.Services.AddSignalR();

var app = builder.Build();
app.UseStaticFiles();

// One line to map the hub endpoint!
app.MapHub<ChatHub>("/chatHub");

app.MapGet("/", () => Results.Redirect("/index.html"));
app.Run("http://localhost:5000");
