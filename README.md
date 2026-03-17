# TeamSearch — Sport Statistics

This repository contains a Blazor WebAssembly client, an ASP.NET Core server, EF Core infrastructure, and a seeder utility that imports a CSV of team statistics.

This README documents the common developer workflow for developers who have cloned the repository: restore packages, apply migrations (or let the seeder do it), seed data, and run the app. Commands below are shown as PowerShell examples and use relative paths so the file is portable in a git repo.

> Why Domain-Driven Design (DDD)?  
> This project uses a lightweight DDD approach to keep business rules and domain types central and isolated from transport and persistence concerns. The `TeamSearch.Domain` project contains the core entities and invariants; `TeamSearch.Application` defines service and repository interfaces (use-cases); `TeamSearch.Infrastructure` implements persistence and migrations. This separation improves maintainability and testability (domain rules are unit-testable without DB or UI), and it makes the codebase easier to evolve or scale (switch persistence, add CQRS/read-models, or split bounded contexts) while keeping the domain model unchanged.


## Prerequisites

- .NET 10 SDK (projects target `net10.0`). Install from https://dotnet.microsoft.com/ if needed.
- Optional: `dotnet-ef` tool to run migrations manually:

```powershell
dotnet tool install --global dotnet-ef
```

## Architecture

The solution follows a layered architecture that separates concerns across several projects. This makes the code easier to reason about, test, and evolve.

- Client (UI): `TeamSearch.Client` — Blazor WebAssembly application that runs in the browser and provides the user interface. It calls the server API via `HttpClient` and uses Fluent UI components for layout and controls.

- Server (API / Host): `TeamSearch.Server` — ASP.NET Core Web API that hosts controllers, registers services and repositories, and exposes the HTTP endpoints consumed by the client. It configures CORS for the local dev client and registers DI services.

- Application (use-cases / interfaces): `TeamSearch.Application` — contains application-level abstractions such as repository and service interfaces (e.g., `ITeamRecordRepository`, `ITeamRecordService`) and any DTO mapping contracts. This layer defines the application's use-cases without depending on EF Core or specific infrastructure.

- Domain (models / entities): `TeamSearch.Domain` — domain entities (e.g., `TeamRecord`) and domain logic. This project contains the core business types and validation rules (if any).

- Infrastructure (persistence / implementations): `TeamSearch.Infrastructure` — EF Core DbContext (`TeamSearchDbContext`), repository implementations, migrations, and utility code (SQLite connection string resolver and PRAGMA setup). This layer depends on EF Core and contains the migration assembly.

- Shared (contracts / DTOs): `TeamSearch.Shared` — data transfer objects, query models, and shared types used by both client and server to avoid duplication.

- Seeder: `TeamSearch.Seeder` — small host that applies EF Core migrations (unless run with `--dry-run`), opens a shared SQLite connection (configures PRAGMA and WAL), and imports/upserts records from `data/CollegeFootballTeamWinsWithMascots.csv`.

Design patterns and practices used:

- Dependency Injection (built-in ASP.NET Core DI) to register repositories and services.
- Repository + Service abstraction: application layer defines interfaces; infrastructure provides implementations.
- DTOs and mapping: keep API shapes separate from domain entities (shared DTOs live in `TeamSearch.Shared`).
- Migrations and IDbContextFactory: migrations live in `TeamSearch.Infrastructure`; `IDbContextFactory` is registered for background services and the seeder.
- SQLite tuning: the seeder opens a shared `SqliteConnection`, sets `PRAGMA busy_timeout` and `PRAGMA journal_mode = WAL` for better concurrency.

How components interact (high level):

1. The Blazor client sends HTTP requests to the ASP.NET Core API (`TeamSearch.Server`).
2. Controllers on the server call application services (from `TeamSearch.Application`) which operate on domain entities and use repository interfaces.
3. Repository implementations in `TeamSearch.Infrastructure` use EF Core to read/write the SQLite database.
4. The seeder can run independently to apply migrations and populate the database from CSV files.

This layered approach keeps persistence and framework concerns separate from business rules and UI.

## Why Domain-Driven Design (DDD)

This project adopts a lightweight Domain-Driven Design (DDD) approach. DDD is primarily about making the domain explicit and keeping business rules central and independent from transport, UI, and persistence concerns. Even in a small project this brings important benefits:

- Scalability: clear boundaries between the domain, application logic, infrastructure, and UI make it easier to evolve or scale parts of the system independently (for example, swapping or scaling the persistence layer, introducing read-models or CQRS, or splitting services into bounded contexts as the product grows).
- Testability: domain logic lives in `TeamSearch.Domain` and can be unit-tested without a database or HTTP layer. The application layer defines interfaces (e.g., `ITeamRecordRepository`, `ITeamRecordService`) that can be mocked in unit tests, and infra implementations can be verified separately with integration tests.
- Maintainability: separating concerns prevents business rules from leaking into controllers or UI code, reducing technical debt and making future changes safer and smaller in scope.

Mapping to this repository:

- `TeamSearch.Domain` — core entities and domain rules.
- `TeamSearch.Application` — use-cases and interfaces (services/repositories).
- `TeamSearch.Infrastructure` — EF Core, repository implementations, and migrations.
- `TeamSearch.Server` — controllers and DI wiring; keeps transport concerns separate.
- `TeamSearch.Client` — UI and presentation; no direct domain logic.

Paste-ready paragraph (copy into the top-level README or Architecture section):

> Why Domain-Driven Design (DDD)?  
> This project uses a lightweight DDD approach to keep business rules and domain types central and isolated from transport and persistence concerns. The `TeamSearch.Domain` project contains the core entities and invariants; `TeamSearch.Application` defines service and repository interfaces (use-cases); `TeamSearch.Infrastructure` implements persistence and migrations. This separation improves maintainability and testability (domain rules are unit-testable without DB or UI), and it makes the codebase easier to evolve or scale (switch persistence, add CQRS/read-models, or split bounded contexts) while keeping the domain model unchanged.


Practical note for small projects: apply DDD pragmatically — favor clear boundaries, small domain models, and focused tests. Avoid premature complexity (heavy aggregates, event sourcing) until real requirements justify them; incremental application of DDD ideas delivers the most value with the least overhead.

## SOLID principles — how we apply them

We follow SOLID-inspired design to keep the code maintainable and testable. Below is a brief mapping of each principle to concrete project examples:

- Single Responsibility Principle (SRP): classes have one reason to change. Examples: `TeamRecordService` (`TeamSearch.Application`) handles use-case orchestration and DTO mapping, while `TeamRecordRepository` (`TeamSearch.Infrastructure`) handles persistence and query building.
- Open/Closed Principle (OCP): behavior is extendable via interfaces without modifying callers. Examples: `ITeamRecordRepository` and `ITeamRecordService` allow swapping implementations; DI registrations in `TeamSearch.Server/Program.cs` compose concrete implementations.
- Liskov Substitution Principle (LSP): implementations satisfy their interfaces so they can be substituted. Examples: `TeamRecordRepository : ITeamRecordRepository` and `TeamRecordService : ITeamRecordService` are consumed via interfaces in controllers and tests.
- Interface Segregation Principle (ISP): interfaces are focused and small. Examples: `ITeamRecordRepository` exposes only persistence concerns (Get/List/Count/Restore/Purge) while the consumer-facing `ITeamRecordService` exposes DTO-oriented methods.
- Dependency Inversion Principle (DIP): high-level modules depend on abstractions. Examples: constructor injection in `TeamRecordService` and `TeamRecordRepository`, and DI registrations in `TeamSearch.Server/Program.cs`. Tests mock `ITeamRecordRepository` in `TeamSearch.Tests` to unit-test services.

Why it matters: these patterns make the domain testable, allow infrastructure to be replaced or extended, and keep responsibilities clear—important even for a small project because it reduces technical debt as the product evolves.

## Why we chose Blazor (single-technology stack)

This project uses Blazor (WebAssembly for the client and ASP.NET Core for the server) so the team can write both client and server code in C#. That single-technology approach provides several practical benefits:

- Shared language and libraries: C# and .NET are used across client, server, domain, and tests. Common types (DTOs, query models) live in `TeamSearch.Shared`, avoiding duplication and keeping contracts consistent.
- Reuse of domain and validation logic: business rules implemented in `TeamSearch.Domain` or `TeamSearch.Application` can be exercised by server code and referenced by client-side code (where appropriate), reducing bugs from duplicated logic.
- Strong typing and compile-time safety: sharing DTOs and models means many errors are caught at compile time rather than at runtime, improving reliability and developer productivity.
- Single toolchain and developer skills: fewer context switches for developers (one language, one debugger, familiar IDE tooling), faster onboarding, and simpler CI pipelines.
- Full-stack debugging and refactoring: debugging in Visual Studio/JetBrains Rider can step through client and server C# code; refactors and renames propagate across projects.
- Component model and ecosystem: Blazor's component model fits well with reusable UI (we use Fluent UI components) and allows incremental enhancement without a full SPA rewrite.

Caveats (brief): Blazor WebAssembly apps have an initial payload cost; for public-facing sites you may consider server prerendering or hosting strategies. For internal tools or small apps the developer productivity and reduced maintenance overhead often outweigh the runtime trade-offs.

## Dev bootstrap script

A small convenience script `dev-bootstrap.ps1` is included at the repository root. It starts the backend server, polls the API until it responds, then starts the Blazor client in a new window so you get logs for both processes.

Quick usage (from the repository root):

```powershell
powershell -ExecutionPolicy Bypass -File .\dev-bootstrap.ps1
```

Example with options (health endpoint and shorter timeout):

```powershell
powershell -ExecutionPolicy Bypass -File .\dev-bootstrap.ps1 -ApiUrl 'https://localhost:7216/health' -MaxAttempts 30
```

Notes:
- Trust the HTTPS dev certificate before running if needed: `dotnet dev-certs https --trust`.
- If the script times out, inspect the server window started by the script or run the server manually:
  ```powershell
  cd ./TeamSearch.Server
  dotnet run --launch-profile https
  ```

## Restore

From the repository root (where this README lives):

```powershell
dotnet restore
```

## Apply migrations

Two recommended approaches:

1) Let the seeder apply migrations automatically (recommended)

The seeder (`TeamSearch.Seeder`) will call `Database.Migrate()` by default when not run with `--dry-run`. It then imports the CSV.

```powershell
# Apply migrations and seed from the default CSV (uses ./data/CollegeFootballTeamWinsWithMascots.csv)
dotnet run --project ./TeamSearch.Seeder -- data/CollegeFootballTeamWinsWithMascots.csv seeder

# Dry-run (preview only; no migrations, no writes)
dotnet run --project ./TeamSearch.Seeder -- --dry-run data/CollegeFootballTeamWinsWithMascots.csv
```

2) Run migrations manually with `dotnet-ef`

```powershell
dotnet ef database update --project ./TeamSearch.Infrastructure --startup-project ./TeamSearch.Server
```

## Auto-seed

- Default CSV: `data/CollegeFootballTeamWinsWithMascots.csv`.
- Usage examples:
  - `dotnet run --project ./TeamSearch.Seeder` — apply migrations and seed using default CSV and importer id `seeder`.
  - `dotnet run --project ./TeamSearch.Seeder -- <csv-path> <importerUserId>` — specify CSV and importer id.
  - Add `--dry-run` or `-n` to simulate without applying migrations or writing data.

The seeder opens a shared SQLite connection, sets PRAGMA options (busy_timeout and WAL), migrates (unless dry-run), and upserts records.

## Run the application

Important: start the backend server first. The client expects the API to be available at the configured `ApiBaseUrl` (defaults to `https://localhost:7216/` in Development). Start the server before starting the front-end to avoid connection errors.

Start the server (recommended) which serves the API and the client:

```powershell
cd ./TeamSearch.Server
dotnet run --launch-profile https
```

Or run the client separately (if desired):

```powershell
cd ./TeamSearch.Client
dotnet run --launch-profile https
```

Frontend (client) — run & dev workflow

Note: make sure the backend server is running before starting the client. If the API is not available the client will fail to fetch data and may show errors while loading.

If you want to run or develop the front-end independently, use the steps below. The client is a Blazor WebAssembly app and can run its own dev server or be hosted by the server project.

- Run the client in development mode (simple):

```powershell
cd ./TeamSearch.Client
dotnet run --launch-profile https
```

- Run the client with file-watch for fast edit-reload:

```powershell
cd ./TeamSearch.Client
dotnet watch run --launch-profile https
```

- Run the client standalone while the API server runs separately (recommended for local dev):

1. Start the server (from repo root or server folder):

```powershell
cd ./TeamSearch.Server
dotnet run --launch-profile https
```

2. In a separate terminal start the client dev server:

```powershell
cd ./TeamSearch.Client
dotnet run --launch-profile https
```

Make sure the API base URL is set correctly (the client defaults to `https://localhost:7216/` in Development). To override it when running the client alone:

```powershell
$env:ApiBaseUrl = 'https://localhost:7216/'
dotnet run --project ./TeamSearch.Client --launch-profile https
```

- Publish the client for production (static assets):

```powershell
dotnet publish ./TeamSearch.Client -c Release -o ./publish/client
```

You can then serve the generated files from a static host or copy them into the server's `wwwroot` for hosting by the API project.

Troubleshooting (common client issues)

- CORS: when running client and server separately, the server allows the local dev origin `https://localhost:7114` by default. If you changed ports, update the CORS policy in `TeamSearch.Server/Program.cs` or set `ApiBaseUrl` accordingly.
- HTTPS dev cert: if the browser blocks the local HTTPS dev certificate, run:

```powershell
dotnet dev-certs https --trust
```

- API unreachable: verify `ApiBaseUrl` in environment or that the server is running and listening on the profile URL (see `launchSettings.json`).

Preflight / health check (verify backend is up)

Before starting the front-end, you can run a quick preflight check to confirm the backend API is responding. Use the examples below.

PowerShell (single check):

```powershell
$uri = 'https://localhost:7216/'
try {
    Invoke-RestMethod -Uri $uri -TimeoutSec 5 | Out-Null
    Write-Host 'API is reachable'
} catch {
    Write-Host 'API not reachable. Start the backend first.'; exit 1
}
```

PowerShell (poll until ready — retries for up to ~30 seconds):

```powershell
$uri = 'https://localhost:7216/'
$maxAttempts = 30
for ($i = 0; $i -lt $maxAttempts; $i++) {
    try {
        Invoke-RestMethod -Uri $uri -TimeoutSec 5 | Out-Null
        Write-Host "API is up after $i seconds"
        exit 0
    } catch {
        Start-Sleep -Seconds 1
    }
}
Write-Host "API not responding after $maxAttempts seconds"; exit 1
```

curl (platform-agnostic single check):

```bash
curl -fsS --max-time 5 https://localhost:7216/ >/dev/null && echo "API is reachable" || echo "API not reachable"
```

If your server exposes a dedicated health endpoint (e.g., `/health` or `/healthz`) prefer using that URL for the checks.


## launchSettings (pre-configured)

Both client and server include `Properties/launchSettings.json` with useful development URLs.

- `./TeamSearch.Server/Properties/launchSettings.json`
  - `http` profile: http://localhost:5149
  - `https` profile: https://localhost:7216 (also listens on http://localhost:5149)

- `./TeamSearch.Client/Properties/launchSettings.json`
  - `http` profile: http://localhost:5287
  - `https` profile: https://localhost:7114 (also listens on http://localhost:5287)

When running the server with the `https` profile, the API is commonly available at `https://localhost:7216/` and the client dev server at `https://localhost:7114/`.

## Overriding the API base URL used by the client

The client reads `ApiBaseUrl` from configuration or environment. To override when running from PowerShell:

```powershell
$env:ApiBaseUrl = 'https://localhost:7216/'
dotnet run --project ./TeamSearch.Client --launch-profile https
```

## Troubleshooting

- Database locked: ensure no other process holds an exclusive lock. The seeder sets `PRAGMA journal_mode=WAL` and a small busy timeout to reduce contention.
- `dotnet ef` fails: ensure `dotnet-ef` tool is installed and your .NET SDK matches project target.
- Port conflicts: adjust `applicationUrl` in the project's `Properties/launchSettings.json` or pass `--urls` to `dotnet run`.

## Notes

- Projects target `net10.0` — verify your SDK version.
- The seeder is quiet on success; use `--dry-run` to preview the import.
