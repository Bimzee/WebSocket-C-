// =============================================================================
// Capstone — Real-Time Multi-Source Live Dashboard
// =============================================================================
// This capstone project combines ALL concepts from Chapters 1-8:
//
//   Chapter 2: WebSocket connections
//   Chapter 3: Structured JSON messaging
//   Chapter 4: Broadcasting to multiple clients
//   Chapter 5: Heartbeat & connection management
//   Chapter 6: Rate limiting (for API calls)
//   Chapter 7: SignalR-like patterns (though we use raw WS here for practice)
//   Chapter 8: Multi-service architecture
//
// FREE APIs used:
//   1. CoinGecko API — Real-time cryptocurrency prices (no API key needed)
//   2. Open-Meteo API — Weather data (no API key needed)
//   3. Wikipedia API — Recent changes feed (no API key needed)
//   4. WorldTimeAPI — World clocks (no API key needed)
// =============================================================================

using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHttpClient();

var app = builder.Build();
app.UseWebSockets();
app.UseStaticFiles();

var connections = new ConcurrentDictionary<string, WebSocket>();
var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
var httpFactory = app.Services.GetRequiredService<IHttpClientFactory>();

// =========================================================================
// Background data fetchers (run every N seconds)
// =========================================================================

// Crypto prices — CoinGecko API (free, no key)
_ = Task.Run(async () =>
{
    var client = httpFactory.CreateClient();
    while (true)
    {
        try
        {
            var response = await client.GetStringAsync(
                "https://api.coingecko.com/api/v3/simple/price?ids=bitcoin,ethereum,dogecoin,solana,cardano&vs_currencies=usd&include_24hr_change=true"
            );
            var data = JsonDocument.Parse(response);
            await BroadcastAll(
                new
                {
                    type = "crypto",
                    data = data.RootElement,
                    timestamp = DateTime.UtcNow,
                }
            );
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Crypto fetch error: {ex.Message}");
        }
        await Task.Delay(TimeSpan.FromSeconds(15)); // CoinGecko rate limit: ~10-30 calls/min
    }
});

// Weather data — Open-Meteo API (free, no key)
_ = Task.Run(async () =>
{
    var client = httpFactory.CreateClient();
    // Cities: New York, London, Tokyo, Sydney, Mumbai
    var cities = new[]
    {
        (Name: "New York", Lat: 40.71, Lon: -74.01),
        (Name: "London", Lat: 51.51, Lon: -0.13),
        (Name: "Tokyo", Lat: 35.68, Lon: 139.69),
        (Name: "Sydney", Lat: -33.87, Lon: 151.21),
        (Name: "Mumbai", Lat: 19.08, Lon: 72.88),
    };

    while (true)
    {
        try
        {
            var weatherData = new List<object>();
            foreach (var city in cities)
            {
                var url =
                    $"https://api.open-meteo.com/v1/forecast?latitude={city.Lat}&longitude={city.Lon}&current=temperature_2m,wind_speed_10m,weather_code,relative_humidity_2m";
                var response = await client.GetStringAsync(url);
                var data = JsonDocument.Parse(response);
                var current = data.RootElement.GetProperty("current");
                weatherData.Add(
                    new
                    {
                        city = city.Name,
                        temperature = current.GetProperty("temperature_2m").GetDouble(),
                        windSpeed = current.GetProperty("wind_speed_10m").GetDouble(),
                        humidity = current.GetProperty("relative_humidity_2m").GetInt32(),
                        weatherCode = current.GetProperty("weather_code").GetInt32(),
                    }
                );
            }
            await BroadcastAll(
                new
                {
                    type = "weather",
                    data = weatherData,
                    timestamp = DateTime.UtcNow,
                }
            );
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Weather fetch error: {ex.Message}");
        }
        await Task.Delay(TimeSpan.FromSeconds(60)); // Update every minute
    }
});

// Wikipedia recent changes — the Wikimedia EventStreams API (free, no key)
_ = Task.Run(async () =>
{
    var client = httpFactory.CreateClient();
    while (true)
    {
        try
        {
            var url =
                "https://en.wikipedia.org/w/api.php?action=query&list=recentchanges&rcnamespace=0&rclimit=10&rcprop=title|timestamp|user|comment&format=json&rctype=edit";
            var response = await client.GetStringAsync(url);
            var data = JsonDocument.Parse(response);
            var changes = data.RootElement.GetProperty("query").GetProperty("recentchanges");

            await BroadcastAll(
                new
                {
                    type = "wiki",
                    data = changes,
                    timestamp = DateTime.UtcNow,
                }
            );
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Wiki fetch error: {ex.Message}");
        }
        await Task.Delay(TimeSpan.FromSeconds(20));
    }
});

// World clocks — WorldTimeAPI (free, no key)
_ = Task.Run(async () =>
{
    var client = httpFactory.CreateClient();
    var zones = new[]
    {
        "America/New_York",
        "Europe/London",
        "Asia/Tokyo",
        "Australia/Sydney",
        "Asia/Kolkata",
    };
    while (true)
    {
        try
        {
            var clocks = new List<object>();
            foreach (var zone in zones)
            {
                var response = await client.GetStringAsync(
                    $"http://worldtimeapi.org/api/timezone/{zone}"
                );
                var data = JsonDocument.Parse(response);
                clocks.Add(
                    new
                    {
                        timezone = zone,
                        datetime = data.RootElement.GetProperty("datetime").GetString(),
                        abbreviation = data.RootElement.GetProperty("abbreviation").GetString(),
                    }
                );
            }
            await BroadcastAll(
                new
                {
                    type = "worldclock",
                    data = clocks,
                    timestamp = DateTime.UtcNow,
                }
            );
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Clock fetch error: {ex.Message}");
        }
        await Task.Delay(TimeSpan.FromSeconds(30));
    }
});

// =========================================================================
// WebSocket endpoint
// =========================================================================
app.Map(
    "/ws",
    async context =>
    {
        if (!context.WebSockets.IsWebSocketRequest)
        {
            context.Response.StatusCode = 400;
            return;
        }

        using var ws = await context.WebSockets.AcceptWebSocketAsync();
        var id = Guid.NewGuid().ToString("N")[..8];
        connections.TryAdd(id, ws);
        Console.WriteLine($"✅ Dashboard client {id} connected. Total: {connections.Count}");

        await SendToClient(
            id,
            new
            {
                type = "system",
                message = $"Connected to Live Dashboard! Streaming data from 4 free APIs.",
                clientId = id,
            }
        );

        var buffer = new byte[1024];
        try
        {
            var result = await ws.ReceiveAsync(
                new ArraySegment<byte>(buffer),
                CancellationToken.None
            );
            while (!result.CloseStatus.HasValue)
                result = await ws.ReceiveAsync(
                    new ArraySegment<byte>(buffer),
                    CancellationToken.None
                );
            await ws.CloseAsync(
                result.CloseStatus.Value,
                result.CloseStatusDescription,
                CancellationToken.None
            );
        }
        catch { }
        finally
        {
            connections.TryRemove(id, out _);
            Console.WriteLine($"👋 Dashboard client {id} disconnected.");
        }
    }
);

app.MapGet("/", () => Results.Redirect("/index.html"));
app.Run("http://localhost:5000");

// =========================================================================
// Broadcasting helpers
// =========================================================================
async Task BroadcastAll(object msg)
{
    var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(msg, jsonOptions));
    foreach (var (_, ws) in connections.Where(c => c.Value.State == WebSocketState.Open))
    {
        try
        {
            await ws.SendAsync(
                new ArraySegment<byte>(bytes),
                WebSocketMessageType.Text,
                true,
                CancellationToken.None
            );
        }
        catch
        { /* connection may have closed between check and send */
        }
    }
}

async Task SendToClient(string id, object msg)
{
    if (connections.TryGetValue(id, out var ws) && ws.State == WebSocketState.Open)
    {
        var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(msg, jsonOptions));
        await ws.SendAsync(
            new ArraySegment<byte>(bytes),
            WebSocketMessageType.Text,
            true,
            CancellationToken.None
        );
    }
}
