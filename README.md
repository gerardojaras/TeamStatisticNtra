# TeamSearch — Sport Statistics

This repository contains a Blazor WebAssembly client, an ASP.NET Core server, EF Core-based persistence, and a seeder that imports a CSV of team statistics.

This README provides a concise quickstart and architecture overview. Commands use relative paths and PowerShell examples for local development.

## Prerequisites

- .NET 10 SDK (projects target `net10.0`). Install from https://dotnet.microsoft.com/ if needed.

## Architecture

The solution uses a layered architecture to separate concerns:

- `TeamSearch.Client` — Blazor WebAssembly UI (uses Fluent UI components).
- `TeamSearch.Server` — ASP.NET Core Web API (controllers, DI composition, CORS).
- `TeamSearch.Application` — application use-cases and interfaces (services, repositories).
- `TeamSearch.Domain` — domain entities and business rules.
- `TeamSearch.Infrastructure` — EF Core DbContext, repository implementations, and migrations.
- `TeamSearch.Shared` — DTOs and shared models between client and server.
- `TeamSearch.Seeder` — tool that applies migrations (by default) and imports CSV data into the database.

Patterns used: dependency injection, repository/service separation, DTO mapping, and IDbContextFactory for background contexts. The seeder configures SQLite PRAGMA settings (busy_timeout, WAL) for reliable local usage.

## Why Domain-Driven Design (DDD)

This repository follows a lightweight DDD approach: domain types and business rules live in `TeamSearch.Domain`, application use-cases and interfaces live in `TeamSearch.Application`, and persistence is implemented in `TeamSearch.Infrastructure`. This separation improves testability and maintainability and avoids leaking domain logic into transport or UI layers.

Practical note: keep DDD pragmatic for small projects — focus on clear boundaries, concise domain models, and targeted tests.

## SOLID principles

The code follows SOLID-friendly practices: small interfaces (`ITeamRecordRepository` / `ITeamRecordService`), constructor injection, clear separation between application and infrastructure, and focused classes for single responsibilities. This improves testability and makes implementations replaceable.

## Why we chose Blazor

Blazor lets client and server be written in C#, share DTOs, and reuse domain logic when appropriate. This reduces duplication, improves type safety, and streamlines developer workflows. For public-facing sites consider prerendering or other hosting options to reduce initial payloads.

## Quickstart

1) Restore packages (from the repository root):

```powershell
dotnet restore
```

2) Apply migrations & seed data (recommended):

```powershell
dotnet run --project ./TeamSearch.Seeder -- data/CollegeFootballTeamWinsWithMascots.csv seeder
```

Use `--dry-run` (or `-n`) to preview the import without applying migrations or writing data.

3) Run the server (recommended to start first):

```powershell
cd ./TeamSearch.Server
dotnet run --launch-profile https
```

4) In a separate terminal run the client (if running standalone):

```powershell
cd ./TeamSearch.Client
dotnet run --launch-profile https
```

5) Open the app in your browser (server `https` profile commonly serves the host page):

```powershell
Start-Process 'https://localhost:7216/'
```

## Client dev workflow (optional)

- Run the client with file-watch for fast edit-reload:

```powershell
cd ./TeamSearch.Client
dotnet watch run --launch-profile https
```

- To override the API base URL when running the client standalone:

```powershell
$env:ApiBaseUrl = 'https://localhost:7216/'
dotnet run --project ./TeamSearch.Client --launch-profile https
```

## Publish (host client from server)

```powershell
dotnet publish ./TeamSearch.Client -c Release -o ./publish/client
# then copy the publish contents into TeamSearch.Server/wwwroot or configure CI
```

## launchSettings (pre-configured)

- `./TeamSearch.Server/Properties/launchSettings.json` — `https` profile: https://localhost:7216 (also listens on http://localhost:5149)
- `./TeamSearch.Client/Properties/launchSettings.json` — `https` profile: https://localhost:7114 (also listens on http://localhost:5287)

## Troubleshooting

- Database locked: if migration or seeding fails, the SQLite file is likely locked by another process (for example an already running API/seeder instance or an open DB browser). Stop those processes and run the command again. The seeder sets `PRAGMA journal_mode=WAL` and a small busy timeout to reduce contention.
- Migration may fail when DB is locked: `dotnet ef database update` and startup auto-migration can fail until the lock is released. If this happens, stop any process using the DB, then retry.

### Quick unlock checklist (PowerShell)

```powershell
# 1) Stop running app/seeder processes that may hold the SQLite file
Get-Process dotnet -ErrorAction SilentlyContinue | Stop-Process -Force

# 2) Optional: close DB Browser/SQLite tools manually if open

# 3)