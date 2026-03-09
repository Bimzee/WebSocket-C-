# 🌐 WebSocket Mastery in C# — From Basics to Advanced

A comprehensive, chapter-based learning project to master **WebSocket** concepts using **C# / ASP.NET Core 8**. Each chapter builds on the previous one, progressing from foundational theory to production-grade patterns, culminating in a **real-time financial dashboard** capstone project.

---

## 📋 Table of Contents

| Chapter | Topic | Type |
|---------|-------|------|
| [Chapter 01](./Chapter01-WhatIsWebSocket/) | What is WebSocket? | 📖 Theory |
| [Chapter 02](./Chapter02-FirstConnection/) | Your First WebSocket Connection | 💻 Code |
| [Chapter 03](./Chapter03-Messaging/) | Sending & Receiving Messages | 💻 Code |
| [Chapter 04](./Chapter04-Broadcasting/) | Broadcasting & Groups | 💻 Code |
| [Chapter 05](./Chapter05-HeartbeatReconnection/) | Heartbeat & Reconnection | 💻 Code |
| [Chapter 06](./Chapter06-AuthSecurity/) | Authentication & Security | 💻 Code |
| [Chapter 07](./Chapter07-SignalR/) | SignalR — The .NET Way | 💻 Code |
| [Chapter 08](./Chapter08-Scaling/) | Scaling WebSockets with Redis | 💻 Code + 🐳 Docker |
| [Capstone](./Capstone-LiveDashboard/) | Real-Time Financial Dashboard | 🚀 Project |

---

## 🛠 Prerequisites

Before you begin, make sure you have the following installed:

| Tool | Version | Purpose |
|------|---------|---------|
| [.NET SDK](https://dotnet.microsoft.com/download) | 8.0+ | Building and running C# projects |
| [Docker Desktop](https://www.docker.com/products/docker-desktop/) | Latest | Running Redis for chapters 8 + Capstone |
| [Git](https://git-scm.com/) | Latest | Version control |
| A modern browser | Chrome / Edge / Firefox | Running the WebSocket client pages |

### Optional

| Tool | Purpose |
|------|---------|
| [Visual Studio 2022](https://visualstudio.microsoft.com/) or [VS Code](https://code.visualstudio.com/) | IDE for editing and debugging |
| [Postman](https://www.postman.com/) | Testing WebSocket connections |

---

## 🚀 Getting Started

### 1. Clone or download this repository

```bash
cd c:\Bimal\Antigravity\WebSocket-C#
```

### 2. Verify .NET SDK installation

```bash
dotnet --version
# Should output 8.0.x or later
```

### 3. Run any chapter

Each chapter is a **standalone ASP.NET Core project**. Navigate to the chapter folder and run:

```bash
# Example: Running Chapter 02
cd Chapter02-FirstConnection
dotnet run
```

Then open your browser to `http://localhost:5000` to interact with the client page.

### 4. Docker (for Chapter 08 and Capstone)

```bash
# Start Redis
docker-compose up -d

# Verify Redis is running
docker ps
```

---

## 📁 Project Structure

```
WebSocket-C#/
├── README.md                          ← You are here!
├── WebSocket-CSharp.sln               ← Solution file (all projects)
├── docker-compose.yml                 ← Redis for scaling chapters
│
├── Chapter01-WhatIsWebSocket/         ← 📖 Theory only
├── Chapter02-FirstConnection/         ← 💻 First WebSocket server + client
├── Chapter03-Messaging/               ← 💻 JSON messaging & echo
├── Chapter04-Broadcasting/            ← 💻 Multi-client & groups
├── Chapter05-HeartbeatReconnection/   ← 💻 Ping/pong & auto-reconnect
├── Chapter06-AuthSecurity/            ← 💻 JWT auth & security
├── Chapter07-SignalR/                 ← 💻 SignalR hub pattern
├── Chapter08-Scaling/                 ← 💻🐳 Redis backplane
└── Capstone-LiveDashboard/            ← 🚀 Real-time financial dashboard
```

---

## 🧰 Technology Stack

| Layer | Technology |
|-------|-----------|
| **Server** | C# / ASP.NET Core 8 with native WebSocket middleware |
| **Client** | Vanilla HTML + CSS + JavaScript (browser WebSocket API) |
| **Chapter 7** | ASP.NET Core SignalR |
| **Scaling** | Redis (via Docker) with `StackExchange.Redis` |
| **Capstone APIs** | Binance WebSocket (crypto) + Finnhub WebSocket (stocks) |

---

## 📚 How to Use This Course

1. **Start with Chapter 01** — Read the theory to understand what WebSocket is and why it exists
2. **Follow the chapters in order** — Each chapter builds on concepts from previous ones
3. **Read the README first** — Each chapter has a detailed README explaining concepts before code
4. **Run the code** — Every code chapter has a working server and client you can run locally
5. **Experiment!** — Modify the code, break things, and learn from the behavior
6. **Build the Capstone** — Apply everything you've learned in a real-world project

---

## 🔑 API Keys (Capstone Only)

The capstone project uses two free APIs:

| API | Key Required? | How to Get |
|-----|--------------|-----------|
| **Binance WebSocket** | ❌ No | Public streams, no registration needed |
| **Finnhub WebSocket** | ✅ Free key | Sign up at [finnhub.io](https://finnhub.io) — no credit card |

---

## 📝 License

This project is created for educational purposes. Feel free to use, modify, and share.
