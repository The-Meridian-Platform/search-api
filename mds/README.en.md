# 🔎 Meridian Search API

> A .NET backend for focused, asynchronous OSINT searches.

Meridian accepts a target — an email, username, domain, or IP address — runs the appropriate data providers, saves the search lifecycle, and delivers results to a desktop client as they become available.

> 🚧 **Status:** active development. The domain and persistence foundations are being built; providers and live streaming are next.

← [Choose another language](../README.md)

---

## ✨ The idea

This is a search-oriented OSINT workspace, not a generic graph editor.

```text
🎯 Target → 🧾 Search session → ⚙️ Provider queue → 🌐 Provider results
                                                     ↓
🖥️ Desktop client ← 📡 Live card updates ← 💾 Persisted results
```

While a provider works, the client can show a spinner on its card. When a result arrives, only that card changes — no long-running HTTP request and no waiting for the entire search to finish.

---

## 🏗️ Architecture

The solution follows **Clean Architecture** and **Ports & Adapters**. Dependencies always point inward.

```text
MeridianHost.Api
        ↓
MeridianHost.Persistence / MeridianHost.Infrastructure
        ↓
MeridianHost.Application
        ↓
MeridianHost.Domain
```

| Project | Purpose |
| --- | --- |
| 🧠 `MeridianHost.Domain` | Search rules and value objects: `SearchSession`, `Email`, `Username`, `DomainName`, DNS records. |
| 🎯 `MeridianHost.Application` | Use cases, commands/queries, validation, handlers, and ports for repositories, queues, and update publishers. |
| 💾 `MeridianHost.Persistence` | EF Core / PostgreSQL entities, mappings, configurations, and repository implementations. |
| 🌐 `MeridianHost.Infrastructure` | OSINT providers, HTTP integrations, retry/rate-limit policies, and background processing. |
| 🚀 `MeridianHost.Api` | ASP.NET Core host, endpoints, gRPC services, transport DTOs, and composition root. |

> 📌 gRPC stays in `Api`. Application communicates with external concerns only through interfaces.

---

## 🧩 Current domain model

```text
SearchSession
 ├─ 🆔 Id
 ├─ 🎯 SearchTarget: Email | Username | Domain | IpAddress
 ├─ 📊 Status: Pending | Running | Completed | Cancelled | Failed
 └─ 🕒 CreatedAt / StartedAt / FinishedAt
```

Input is normalized and validated by value objects before it becomes a search target. Timestamps use `DateTimeOffset`, keeping moments unambiguous across server and client time zones.

---

## 🛠️ Technology

- ⚡ .NET 10 / C# with nullable reference types
- 🌐 ASP.NET Core
- 🐘 EF Core + PostgreSQL (`Npgsql`)
- ✅ FluentValidation
- 📦 CSharpFunctionalExtensions for the Result pattern
- 📡 Planned: gRPC server streaming and `System.Threading.Channels`

---

## 🚀 Getting started

### 1. Prerequisites

- .NET SDK 10
- PostgreSQL 16+ or a compatible PostgreSQL instance

### 2. Configure PostgreSQL

Create `MeridianHost.Api/appsettings.Local.json`. It is intentionally ignored by Git.

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=meridian;Username=postgres;Password=change-me"
  }
}
```

### 3. Build and run

```powershell
dotnet restore MeridianHost.sln
dotnet build MeridianHost.sln
dotnet run --project MeridianHost.Api
```

---

## 🗺️ Roadmap

- [x] 🎯 Target value objects and search-session lifecycle foundation
- [x] 💾 EF Core / PostgreSQL project structure
- [ ] 🧱 Complete session persistence mapping and repository implementation
- [ ] ✉️ `CreateSearch` command and handler
- [ ] 📬 Bounded background queue
- [ ] 🖼️ First provider: email/avatar lookup
- [ ] 🗃️ Persist provider execution and results
- [ ] 📡 gRPC stream for live card updates
- [ ] 🔁 Reconnect through ordered search updates
- [ ] 🖥️ WPF desktop client

---

## 🤝 Development principles

- Keep Domain free from transport, database, and provider implementation details.
- Use `Result` for invalid user input instead of business exceptions.
- Pass `CancellationToken` through every I/O operation and provider call.
- Providers do not write directly to UI or persistence; Application orchestrates results.
- Respect provider terms, rate limits, privacy, and applicable law.
