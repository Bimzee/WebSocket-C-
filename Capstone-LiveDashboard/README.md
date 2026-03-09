# 🚀 Capstone — Real-Time Multi-Source Live Dashboard

> **Goal**: Apply ALL WebSocket concepts from Chapters 1-8 in a real-world project  
> **Tech**: Raw WebSocket server, background data fetchers, 4 free APIs, rich HTML dashboard

---

## 🎯 What This Project Demonstrates

| Chapter | Concept Applied Here |
|---------|---------------------|
| Ch 2 | WebSocket connection & message loop |
| Ch 3 | Structured JSON messaging with typed messages |
| Ch 4 | Broadcasting real-time data to multiple clients |
| Ch 5 | Auto-reconnection on the client side |
| Ch 6 | Rate limiting API calls to respect free tier limits |
| Ch 7 | Hub-like pattern (typed messages, method dispatch) |
| Ch 8 | Multi-service architecture (multiple background fetchers) |

---

## 🌐 Free APIs Used

| API | Data | Rate Limit | URL |
|-----|------|-----------|-----|
| **CoinGecko** | Crypto prices (BTC, ETH, DOGE, SOL, ADA) | ~30 req/min | [coingecko.com](https://www.coingecko.com/en/api) |
| **Open-Meteo** | Weather for 5 cities | 10,000 req/day | [open-meteo.com](https://open-meteo.com/) |
| **Wikipedia** | Recent article edits | Generous | [wikipedia.org/w/api.php](https://en.wikipedia.org/w/api.php) |
| **WorldTimeAPI** | World clocks (5 timezones) | Unlimited | [worldtimeapi.org](http://worldtimeapi.org/) |

> 💡 **No API keys needed!** All APIs are completely free and require no registration.

---

## 📊 Architecture

```
┌─────────────────────────────────────────────────────────┐
│                    ASP.NET Core Server                   │
│                                                         │
│   ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  │
│   │ CoinGecko    │  │ Open-Meteo   │  │  Wikipedia   │  │
│   │ Fetcher      │  │ Fetcher      │  │  Fetcher     │  │
│   │ (every 15s)  │  │ (every 60s)  │  │  (every 20s) │  │
│   └──────┬───────┘  └──────┬───────┘  └──────┬───────┘  │
│          │                 │                 │          │
│   ┌──────────────┐                                      │
│   │ WorldTimeAPI │        ┌─────────────────┐          │
│   │ Fetcher      │───────►│   Broadcast     │          │
│   │ (every 30s)  │        │   to all WS     │          │
│   └──────────────┘        │   clients       │          │
│                           └────────┬────────┘          │
│                                    │                    │
└────────────────────────────────────┼────────────────────┘
                                     │ WebSocket
                    ┌────────────────┼──────────────────┐
                    │                │                  │
               ┌────┴───┐     ┌────┴───┐        ┌────┴───┐
               │Browser │     │Browser │        │Browser │
               │ Tab 1  │     │ Tab 2  │        │ Tab 3  │
               └────────┘     └────────┘        └────────┘
```

---

## 📁 Files

| File | Purpose |
|------|---------|
| `Program.cs` | Server with 4 background fetchers + WebSocket endpoint |
| `wwwroot/index.html` | Rich dashboard with 4 live widgets |

---

## 🚀 How to Run

```bash
cd Capstone-LiveDashboard
dotnet run
```

Open **<http://localhost:5000>** to see your live dashboard!

### What You'll See

1. **💰 Cryptocurrency** — Bitcoin, Ethereum, Doge, Solana, Cardano prices updating every 15s
2. **🌤️ World Weather** — Temperature, wind, humidity for 5 cities, updating every 60s
3. **📚 Wikipedia** — Latest article edits happening RIGHT NOW on Wikipedia
4. **🕐 World Clocks** — Real-time clocks from 5 timezones

### Open Multiple Tabs

Open 2-3 tabs — all receive the same real-time data simultaneously, demonstrating the broadcasting pattern from Chapter 4.

---

## 🧠 What to Study

1. **Background Tasks** — Each API fetcher runs as an independent `Task.Run()` loop
2. **Rate Limiting** — Each API has a different polling interval to respect rate limits
3. **Error Handling** — API failures don't crash the server; they're logged and retried
4. **Broadcasting** — All connected clients receive the same data simultaneously
5. **JSON Messaging** — Each data source has its own message `type` for routing

---

## 📚 Back to Start

**[← Back to Main README](../README.md)**

Congratulations! You've completed the entire WebSocket learning project! 🎉
