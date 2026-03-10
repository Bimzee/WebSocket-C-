# 🥋 Capstone — Code Katas: Real-Time Dashboard Challenges

> **Type**: Full-stack coding exercises  
> **Goal**: Extend and enhance the Live Dashboard by applying ALL concepts from Chapters 1-8

---

## 🟢 Kata 1: Add a New Data Source (Easy)

**Objective**: Integrate a new free API into the dashboard.

**Requirements:**

1. Pick one of these free APIs (no API key needed):
   - **Exchange Rates**: `https://open.er-api.com/v6/latest/USD`
   - **ISS Location**: `http://api.open-notify.org/iss-now.json`
   - **Random Facts**: `https://uselessfacts.jsph.pl/api/v2/facts/random`
2. Create a new background fetcher task that polls it at an appropriate interval
3. Broadcast the data to all clients with a new `type` (e.g., `"iss_location"`)
4. Add a new widget card to the HTML dashboard to display the data
5. Match the visual style of the existing widgets

---

## 🟢 Kata 2: Connection Status Widget (Easy)

**Objective**: Add a widget that shows real-time connection information.

**Requirements:**

1. Display on the dashboard:
   - WebSocket connection state (Connected / Disconnected / Reconnecting)
   - Connection uptime (how long the current connection has been active)
   - Total messages received (counter)
   - Messages per second (rolling average over last 10 seconds)
2. Update the counters in real-time as data arrives
3. Style with a green/red indicator matching the connection state

---

## 🟡 Kata 3: Data Source Toggle (Medium)

**Objective**: Let users subscribe/unsubscribe to individual data feeds.

**Requirements:**

1. Add toggle buttons for each data source (Crypto, Weather, Wikipedia, Clocks)
2. When toggled off, the client sends: `{ "type": "unsubscribe", "source": "crypto" }`
3. The server stops sending that data type to THAT specific client (others still get it)
4. Track subscriptions per client on the server side
5. Persist toggles in `localStorage` and restore on reconnect
6. Show a visual indicator (dimmed widget) for disabled sources

<details>
<summary>💡 Hints</summary>

- Track subscriptions: `ConcurrentDictionary<string, HashSet<string>> _subscriptions`
- Before broadcasting, check: `if (_subscriptions[clientId].Contains(source))`
- Default all sources to "subscribed" on initial connect

</details>

---

## 🟡 Kata 4: Alert System (Medium)

**Objective**: Add configurable alerts that trigger on data conditions.

**Requirements:**

1. Let users set alerts, e.g., `{ "type": "set_alert", "source": "crypto", "coin": "bitcoin", "condition": "above", "value": 50000 }`
2. When the condition is met, send a special alert message to that client
3. Support conditions: `above`, `below`, `change_percent` (e.g., >5% change)
4. Display alerts as toast notifications on the dashboard
5. Play a subtle sound effect on alert (using Web Audio API)
6. Allow removing alerts: `{ "type": "remove_alert", "alertId": "..." }`

---

## 🟡 Kata 5: Historical Data Chart (Medium)

**Objective**: Store and visualize historical data points.

**Requirements:**

1. Server stores the last **60 data points** for each source (1 hour of crypto at 1min intervals)
2. New clients receive historical data on connect
3. Add a simple line chart to the dashboard using `<canvas>` (no external libraries)
4. The chart updates in real-time as new data arrives
5. Support switching between data sources on the chart
6. Draw axes, labels, and a grid on the canvas

<details>
<summary>💡 Hints</summary>

- Store history: `Queue<(DateTime, double)>` per metric, cap at 60
- On connect, send: `{ "type": "history", "source": "crypto", "data": [...] }`
- For canvas charting: use `ctx.lineTo()` and scale data points to canvas dimensions
- Formula: `x = (index / points.length) * canvasWidth`, `y = canvasHeight - (value / maxValue) * canvasHeight`

</details>

---

## 🔴 Kata 6: Multi-Server Dashboard (Hard)

**Objective**: Scale the dashboard across multiple server instances using Redis.

**Requirements:**

1. Add `AddStackExchangeRedis()` to enable cross-server broadcasting
2. Run 2-3 server instances on different ports
3. All instances share the same data fetchers (avoid duplicate API calls):
   - Only ONE server should fetch data (leader election or distributed lock)
   - The fetching server publishes data to Redis
   - All servers broadcast to their local clients
4. Add a "Server Info" widget showing which server the client is connected to
5. Test: connect clients to different servers, verify all see the same data

<details>
<summary>💡 Hints</summary>

- Use a Redis lock for leader election: `SET leader:{source} {serverId} NX EX 30`
- Only the server holding the lock fetches data from the API
- Other servers subscribe to the Redis channel for data
- If the leader dies, the lock expires and another server takes over

</details>

---

## 🔴 Kata 7: Build Your Own Dashboard (Hard)

**Objective**: Design and build a completely new real-time dashboard from scratch.

**Pick a theme and build it end-to-end:**

### Option A: Sports Dashboard

- Live scores using a free sports API
- Team standings
- Match events (goals, cards, substitutions)

### Option B: Developer Dashboard

- GitHub trending repos (GitHub API)
- Stack Overflow latest questions (SO API)
- NPM download counts (NPM registry API)

### Option C: Environment Dashboard

- Air quality data (OpenAQ API)
- Earthquake data (USGS API)
- Solar/UV index (Open-Meteo)

**Requirements (all options):**

1. At least **3 different data sources**
2. Background fetchers with appropriate rate limiting
3. WebSocket broadcasting to all clients
4. Auto-reconnection on the client
5. Polished HTML/CSS dashboard with responsive design
6. Error handling for API failures (show "data stale" indicator)
7. At least one interactive element (filter, toggle, alert)

<details>
<summary>💡 Hints</summary>

- Start by testing each API in browser/Postman to understand the response format
- Design your JSON message protocol FIRST before coding
- Build the server data fetchers one at a time, test each independently
- Build the dashboard widgets progressively — get one working before adding the next
- Use CSS Grid for responsive layouts that look good on different screen sizes

</details>
