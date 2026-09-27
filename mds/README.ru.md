# 🔎 Meridian Search API

> .NET backend для сфокусированного асинхронного OSINT-поиска.

Meridian принимает target — email, username, домен или IP-адрес, — запускает подходящие источники данных, сохраняет жизненный цикл поиска и отправляет результаты в desktop-клиент сразу по мере появления.

> 🚧 **Статус:** активная разработка. Сейчас формируется Domain и фундамент Persistence; далее — providers и live-стриминг.

← [Выбрать другой язык](../README.md)

---

## ✨ Идея

Это OSINT workspace, ориентированный на поиск, а не обычный редактор графов.

```text
🎯 Target → 🧾 Search session → ⚙️ Очередь providers → 🌐 Результаты providers
                                                         ↓
🖥️ Desktop-клиент ← 📡 Live-обновления карточек ← 💾 Сохранённые результаты
```

Пока provider работает, клиент показывает spinner на его карточке. Когда приходит результат, обновляется только эта карточка — не нужно держать долгий HTTP-запрос и ждать завершения всего поиска.

---

## 🏗️ Архитектура

Решение строится по принципам **Clean Architecture** и **Ports & Adapters**. Зависимости направлены только внутрь.

```text
MeridianHost.Api
        ↓
MeridianHost.Persistence / MeridianHost.Infrastructure
        ↓
MeridianHost.Application
        ↓
MeridianHost.Domain
```

| Проект | Назначение |
| --- | --- |
| 🧠 `MeridianHost.Domain` | Правила предметной области и value objects: `SearchSession`, `Email`, `Username`, `DomainName`, DNS-записи. |
| 🎯 `MeridianHost.Application` | Use cases, команды/запросы, валидация, handlers и порты для repositories, очередей и publishers обновлений. |
| 💾 `MeridianHost.Persistence` | EF Core / PostgreSQL entities, mappings, configurations и реализации repositories. |
| 🌐 `MeridianHost.Infrastructure` | OSINT providers, HTTP-интеграции, retry/rate-limit политики и фоновая обработка. |
| 🚀 `MeridianHost.Api` | ASP.NET Core host, endpoints, gRPC-сервисы, transport DTO и composition root. |

> 📌 gRPC остаётся в `Api`. Application взаимодействует с внешними деталями только через интерфейсы.

---

## 🧩 Текущая доменная модель

```text
SearchSession
 ├─ 🆔 Id
 ├─ 🎯 SearchTarget: Email | Username | Domain | IpAddress
 ├─ 📊 Status: Pending | Running | Completed | Cancelled | Failed
 └─ 🕒 CreatedAt / StartedAt / FinishedAt
```

Value objects нормализуют и валидируют target до запуска поиска. Для времени используется `DateTimeOffset`, поэтому момент события однозначен независимо от часового пояса сервера и клиента.

---

## 🛠️ Технологии

- ⚡ .NET 10 / C# с nullable reference types
- 🌐 ASP.NET Core
- 🐘 EF Core + PostgreSQL (`Npgsql`)
- ✅ FluentValidation
- 📦 CSharpFunctionalExtensions для паттерна Result
- 📡 В планах: gRPC server streaming и `System.Threading.Channels`

---

## 🚀 Быстрый старт

### 1. Требования

- .NET SDK 10
- PostgreSQL 16+ или совместимый инстанс PostgreSQL

### 2. Настрой PostgreSQL

Создай `MeridianHost.Api/appsettings.Local.json`. Этот файл намеренно исключён из Git.

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=meridian;Username=postgres;Password=change-me"
  }
}
```

### 3. Собери и запусти

```powershell
dotnet restore MeridianHost.sln
dotnet build MeridianHost.sln
dotnet run --project MeridianHost.Api
```

---

## 🗺️ Roadmap

- [x] 🎯 Базовые value objects target-ов и фундамент жизненного цикла search session
- [x] 💾 Структура проекта EF Core / PostgreSQL
- [ ] 🧱 Завершить mapping и repository для search session
- [ ] ✉️ `CreateSearch` command и handler
- [ ] 📬 Bounded background queue
- [ ] 🖼️ Первый provider: поиск аватара по email
- [ ] 🗃️ Хранение запусков providers и результатов поиска
- [ ] 📡 gRPC stream для live-обновлений карточек
- [ ] 🔁 Reconnect через упорядоченные search updates
- [ ] 🖥️ WPF desktop-клиент

---

## 🤝 Принципы разработки

- Domain не зависит от транспорта, БД и реализаций providers.
- Некорректный пользовательский ввод выражается через `Result`, а не business exceptions.
- `CancellationToken` передаётся в каждый I/O-вызов и provider.
- Providers не пишут напрямую в UI или Persistence: результатами управляет Application.
- Соблюдай условия providers, rate limits, приватность и применимое законодательство.
