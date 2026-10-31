# 🔎 Meridian Search API

> .NET backend для сфокусированного асинхронного OSINT-поиска в реальном времени.

Meridian принимает target — email, username, домен или IP-адрес, — конкурентно запускает подходящие источники данных, сохраняет жизненный цикл поиска и транслирует результаты в desktop-клиент по **gRPC Server Streaming** сразу по мере их обнаружения.

> 🚀 **Статус:** Движок поиска, конкурентный Fan-Out/Fan-In сбор находок, батчинг в PostgreSQL, gRPC-стриминг и WPF-клиент успешно реализованы. Проект активно расширяется новыми OSINT-провайдерами.

← [Выбрать другой язык](../README.md)

---

## ✨ Идея

Это OSINT workspace, ориентированный на скорость и моментальную реакцию интерфейса:

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

Пока providers работают, клиент показывает статус сканирования. Как только DNS или Whois находит запись, она **мгновенно** летит по открытому gRPC-стриму на экран, не дожидаясь окончания остального поиска и без тяжелого HTTP polling.

---

## 🏗️ Архитектура

Решение строится по принципам **Clean Architecture**, **DDD** и **Ports & Adapters**. Зависимости направлены строго внутрь.

```text
MeridianHost.Api (REST Endpoints, Scalar UI, gRPC Streaming Service)
        ↓
MeridianHost.Persistence / MeridianHost.Infrastructure
        ↓
MeridianHost.Application (Use Cases, Queue/Broadcaster Ports, Handlers)
        ↓
MeridianHost.Domain (Entities, Value Objects, Search Rules)
```

| Проект | Назначение |
| --- | --- |
| 🧠 `MeridianHost.Domain` | Правила предметной области, инварианты и value objects: `SearchSession`, `SearchTarget`, `SearchFinding`, `DomainName`, статусы сессий. |
| 🎯 `MeridianHost.Application` | Use cases, порты для очередей (`ISearchWorkQueue`), шины событий (`ISearchSessionBroadcaster`), репозиториев и DTO. |
| 💾 `MeridianHost.Persistence` | EF Core / PostgreSQL entities, mappings (`jsonb` для находок), миграции, батчинг и репозитории. |
| 🌐 `MeridianHost.Infrastructure` | Фоновый воркер `SearchExecutionWorker`, реализация каналов `SearchWorkQueue`, шина `SearchSessionBroadcaster`, OSINT-провайдеры (DNS DoH, Whois RDAP). |
| 🚀 `MeridianHost.Api` | ASP.NET Core host, Minimal API endpoints, Scalar API Reference, Protobuf-контракты и `SearchStreamGrpcService`. |

---

## 🧩 Текущая доменная модель

```text
SearchSession
 ├─ 🆔 Id
 ├─ 🎯 SearchTarget: Domain | Email | Username | IpAddress
 ├─ 📦 Findings: List<SearchFinding> (хранение payload в PostgreSQL jsonb)
 ├─ 📊 Status: Pending | Running | Completed | Cancelled | Failed
 └─ 🕒 CreatedAt / StartedAt / FinishedAt
```

Value objects нормализуют и валидируют цель поиска до старта. Все временные метки используют `DateTimeOffset` для корректной синхронизации часовых поясов.

---

## 🛠️ Технологический стек

- ⚡ **.NET 10 / C#** с nullable reference types
- 🌐 **ASP.NET Core Minimal APIs**
- 📡 **gRPC & Protocol Buffers** (`Grpc.AspNetCore`) — Server Streaming событий поиска
- 🔀 **System.Threading.Channels** — потокобезопасные очереди и in-memory Fan-In каналы
- 🐘 **EF Core + PostgreSQL** (`Npgsql`) с multi-row вставкой и `jsonb` полями
- 📚 **Scalar API Reference** (`Scalar.AspNetCore`) вместо Swagger UI
- 📦 **CSharpFunctionalExtensions** для надежного Result pattern
- 🖥️ **WPF (.NET 10 / WPF-UI)** — реактивный Fluent-клиент для Windows

---

## 🚀 Быстрый старт

### 1. Требования

- [.NET SDK 10](https://dotnet.microsoft.com/)
- PostgreSQL 16+ (локально или в Docker)

### 2. Настрой базу данных

Создай файл `MeridianHost.Api/appsettings.Local.json` (файл в `.gitignore`):

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=meridian;Username=postgres;Password=postgres"
  }
}
```

### 3. Собери и запусти сервер

```powershell
dotnet restore
dotnet build
dotnet run --project MeridianHost.Api
```

- **REST API & Документация Scalar:** `http://localhost:5168/scalar/v1`
- **gRPC Streaming Endpoint:** `http://localhost:5168` / `https://localhost:7013`

---

## 🗺️ Roadmap

- [x] 🎯 Базовые Value Objects target-ов и богатая доменная модель `SearchSession`
- [x] 💾 Структура EF Core / PostgreSQL, маппинг `SearchFinding` с типом `jsonb`
- [x] ✉️ Обработчики создания сессии, получения сессий и фильтрации `includeFindings`
- [x] 📬 Асинхронная очередь `SearchWorkQueue` на базе `System.Threading.Channels`
- [x] ⚙️ Фоновый воркер `SearchExecutionWorker` с изоляцией `IServiceScopeFactory`
- [x] 🌐 Первый OSINT-провайдер: **DNS over HTTPS** (Cloudflare DoH: A, AAAA, MX, TXT, NS, CNAME)
- [x] 📋 Второй OSINT-провайдер: **WHOIS / RDAP** (ICANN / IETF RDAP с обработкой редиректов)
- [x] ⚡ **Конкурентный Fan-Out / Fan-In сбор находок** (ускорение поиска в 2 раза)
- [x] 🚀 **Пакетная вставка в БД (Batching)** через `AddRange` и единый `SaveChangesAsync`
- [x] 📡 **gRPC Server Streaming** (`search_stream.proto`, `SearchStreamGrpcService`, `ISearchSessionBroadcaster`)
- [x] 🖥️ **WPF Desktop Client** на базе `WPF-UI` с живым отображением находок
- [ ] 🔍 Новые OSINT-провайдеры: Certificate Transparency (crt.sh), Subdomain Enumeration, Reverse IP
- [ ] 🔁 Механизм Reconnect для восстановления прерванных стримов

---

## 🤝 Принципы разработки

- **Clean Architecture:** Domain чист и не зависит от транспорта, баз данных или сторонних API.
- **Fail-Safe Concurrency:** Ошибки отдельных провайдеров изолированы и не прерывают выполнение всей сессии.
- **Zero Captive Dependencies:** Одноразовые скоупы сервисов создаются внутри долгоживущих фоновых воркеров.
- **Cancellation-Aware:** `CancellationToken` передаётся абсолютно во все I/O, HTTP и асинхронные вызовы.
- **Этика OSINT:** Использование только легальных и открытых источников данных в соответствии с законами и правилами платформ.

---

## 📄 Лицензирование (Dual-License)

Проект использует модель двойного лицензирования:

1. **Open Source (GNU AGPLv3)**: Бесплатно для персонального использования, исследований и открытых проектов. При модификации или развертывании в качестве сетевого сервиса исходный код должен быть открыт под AGPLv3.
2. **Коммерческая лицензия (Commercial License)**: Для использования в закрытых корпоративных продуктах, запуска частного SaaS-сервиса без публикации кода или enterprise-поддержки — свяжитесь с The Meridian Platform Team.
