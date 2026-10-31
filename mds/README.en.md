# 🔎 Meridian Search API

> A high-performance .NET backend for focused, real-time asynchronous OSINT reconnaissance.

Meridian accepts a target — an email, username, domain, or IP address — concurrently executes specialized data providers, manages the search lifecycle, and streams findings live to the desktop client via **gRPC Server Streaming** as soon as they are discovered.

> 🚀 **Status:** The search engine, concurrent Fan-Out / Fan-In aggregation, PostgreSQL batching, gRPC streaming, and the WPF desktop client are fully implemented. Actively expanding with new OSINT providers.

← [Choose another language](../README.md)

---

## ✨ The Idea

A search-first OSINT workspace engineered for ultra-low latency and reactive user interfaces:

```text
🎯 Target → 🧾 Search session → 📬 Bounded Queue → ⚡ Concurrency Engine (Fan-Out)
                                                           │
                                   ┌───────────────────────┴───────────────────────┐
                                   ▼                                               ▼
                         🌐 Cloudflare DNS (DoH)                           📋 RDAP / Whois
                                   │                                               │
                                   └───────────────────────┬───────────────────────┘
                                                           ▼ (Fan-In Channel)
                                              📡 Event Broadcaster (Pub/Sub)
                                              ┌────────────┴────────────┐
                                              ▼                         ▼
                                    💾 Batch PostgreSQL      🖥️ gRPC Real-Time Stream
                                                                        ↓
                                                              🏢 WPF Desktop Client
```

While providers run in parallel, the client receives instant notifications. As soon as DNS or WHOIS discovers a record, it is **instantly pushed** across the open gRPC stream to the UI — no long HTTP polling and no blocking on the slowest provider.

---

## 🏗️ Architecture

The solution adheres strictly to **Clean Architecture**, **Domain-Driven Design (DDD)**, and **Ports & Adapters**. Dependencies always point inward.

```text
MeridianHost.Api (REST Endpoints, Scalar UI, gRPC Streaming Service)
        ↓
MeridianHost.Persistence / MeridianHost.Infrastructure
        ↓
MeridianHost.Application (Use Cases, Queue/Broadcaster Ports, Handlers)
        ↓
MeridianHost.Domain (Entities, Value Objects, Search Rules)
```

| Project | Purpose |
| --- | --- |
| 🧠 `MeridianHost.Domain` | Core business rules, invariants, and value objects: `SearchSession`, `SearchTarget`, `SearchFinding`, `DomainName`, status enums. |
| 🎯 `MeridianHost.Application` | Use cases, ports for queues (`ISearchWorkQueue`), event broadcasters (`ISearchSessionBroadcaster`), repositories, and DTOs. |
| 💾 `MeridianHost.Persistence` | EF Core / PostgreSQL entities, mappings (`jsonb` payload storage), migrations, batch inserts, and repository implementations. |
| 🌐 `MeridianHost.Infrastructure` | Background engine `SearchExecutionWorker`, channel implementations, event bus `SearchSessionBroadcaster`, and OSINT providers (DNS DoH, Whois RDAP). |
| 🚀 `MeridianHost.Api` | ASP.NET Core host, Minimal API endpoints, Scalar API Reference, Protobuf schemas, and `SearchStreamGrpcService`. |

---

## 🧩 Domain Model

```text
SearchSession
 ├─ 🆔 Id
 ├─ 🎯 SearchTarget: Domain | Email | Username | IpAddress
 ├─ 📦 Findings: List<SearchFinding> (payload stored as PostgreSQL jsonb)
 ├─ 📊 Status: Pending | Running | Completed | Cancelled | Failed
 └─ 🕒 CreatedAt / StartedAt / FinishedAt
```

Input is validated and normalized by Value Objects prior to session execution. All timestamps use `DateTimeOffset` to ensure seamless time-zone consistency between server and clients.

---

## 🛠️ Technology Stack

- ⚡ **.NET 10 / C#** with nullable reference types
- 🌐 **ASP.NET Core Minimal APIs**
- 📡 **gRPC & Protocol Buffers** (`Grpc.AspNetCore`) — Server Streaming of real-time search events
- 🔀 **System.Threading.Channels** — Thread-safe bounded queues and in-memory Fan-In channels
- 🐘 **EF Core + PostgreSQL** (`Npgsql`) with multi-row batch inserts and `jsonb` columns
- 📚 **Scalar API Reference** (`Scalar.AspNetCore`) for interactive API documentation
- 📦 **CSharpFunctionalExtensions** for the robust Result pattern
- 🖥️ **WPF (.NET 10 / WPF-UI)** — Modern Fluent dark-mode client for Windows

---

## 🚀 Getting Started

### 1. Prerequisites

- [.NET SDK 10](https://dotnet.microsoft.com/)
- PostgreSQL 16+ (local installation or Docker container)

### 2. Configure Database

Create `MeridianHost.Api/appsettings.Local.json` (ignored by Git):

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=meridian;Username=postgres;Password=postgres"
  }
}
```

### 3. Build and Run

```powershell
dotnet restore
dotnet build
dotnet run --project MeridianHost.Api
```

- **REST API & Scalar Documentation:** `http://localhost:5168/scalar/v1`
- **gRPC Streaming Endpoint:** `http://localhost:5168` / `https://localhost:7013`

---

## 🗺️ Roadmap

- [x] 🎯 Target Value Objects and rich `SearchSession` domain aggregate
- [x] 💾 EF Core / PostgreSQL structure with `SearchFinding` mapped to `jsonb`
- [x] ✉️ Session creation handlers and query filtering (`includeFindings`)
- [x] 📬 Asynchronous producer-consumer queue via `System.Threading.Channels`
- [x] ⚙️ Background `SearchExecutionWorker` with scoped dependency lifecycle
- [x] 🌐 OSINT Provider: **DNS over HTTPS** (Cloudflare DoH: A, AAAA, MX, TXT, NS, CNAME)
- [x] 📋 OSINT Provider: **WHOIS / RDAP** (ICANN / IETF RDAP with redirect handling)
- [x] ⚡ **Concurrent Fan-Out / Fan-In aggregation** (2x search execution speedup)
- [x] 🚀 **Database Batching** via `AddRange` and single `SaveChangesAsync`
- [x] 📡 **gRPC Server Streaming** (`search_stream.proto`, `SearchStreamGrpcService`, `ISearchSessionBroadcaster`)
- [x] 🖥️ **WPF Desktop Client** with `WPF-UI` and live-streaming findings feed
- [ ] 🔍 Additional OSINT Providers: Certificate Transparency (crt.sh), Subdomain Enumeration, Reverse IP
- [ ] 🔁 Stream reconnection mechanism for dropped clients

---

## 🤝 Development Principles

- **Clean Architecture:** Domain stays entirely decoupled from transport, databases, and third-party APIs.
- **Fail-Safe Concurrency:** Individual provider errors are isolated and never crash the overall search session.
- **Zero Captive Dependencies:** Ephemeral service scopes are created inside long-running background workers.
- **Cancellation-Aware:** `CancellationToken` is propagated through all I/O, network, and async calls.
- **OSINT Ethics:** Strictly utilizes authorized, publicly accessible data sources in compliance with platform rules and law.

---

## 📄 Licensing (Dual-License)

This project is dual-licensed:

1. **Open Source (GNU AGPLv3)**: Free for personal use, research, and open-source projects. If you modify or run this software as a network service, you must release your complete modified source code under AGPLv3.
2. **Commercial License**: For proprietary commercial software, private SaaS deployments without source disclosure, or enterprise support — contact The Meridian Platform Team.
