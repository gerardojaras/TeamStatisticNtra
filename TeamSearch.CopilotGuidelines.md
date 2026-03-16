# TeamSearch — Copilot Guidelines

Purpose
- Provide concise guidance and examples for GitHub Copilot (and contributors) so generated C# code follows the project's preferred dependency-injection style: inject dependencies on the class declaration (primary-constructor style) when possible.

Location
- This file lives at the repository root (`TeamSearch.CopilotGuidelines.md`).

Guidelines
- Prefer primary-constructor style for dependency injection (DI) across the project. Use the primary-constructor/class-declaration form (e.g. `public class MyService(IDep dep)`) for repositories and application services by default.
- Prefer primary-constructor style for simple classes (repositories/services) to keep the class compact and readonly fields implicit. This project uses C# 10+ file-scoped namespaces and primary-constructor syntax in many places.
- When multiple dependencies are required, prefer an explicit parameter list on the class declaration and assign to readonly backing fields only if you need to store them; otherwise use the parameters directly.
- Always validate required arguments (null checks) when necessary.
- Keep DI registration and interface abstractions in mind: services should depend on interfaces where appropriate.

Examples

1) Single dependency (repository style)

```csharp
namespace TeamSearch.Infrastructure.Repositories;

public class TeamRecordRepository(TeamSearchDbContext db) : ITeamRecordRepository
{
    private readonly TeamSearchDbContext _db = db ?? throw new ArgumentNullException(nameof(db));

    // ... methods that use _db ...
}
```

2) Multiple dependencies (service style)

```csharp
namespace TeamSearch.Application.Services;

public class TeamRecordService(ITeamRecordRepository repo, ILogger<TeamRecordService> logger) : ITeamRecordService
{
    private readonly ITeamRecordRepository _repo = repo ?? throw new ArgumentNullException(nameof(repo));
    private readonly ILogger<TeamRecordService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    // ... methods that use _repo and _logger ...
}
```

3) When you prefer explicit constructor body (for complex initialization)

```csharp
public class ComplexService
{
    private readonly IDep _dep;

    public ComplexService(IDep dep)
    {
        _dep = dep ?? throw new ArgumentNullException(nameof(dep));
        // additional initialization
    }
}
```

DI registration (Program.cs)

```csharp
builder.Services.AddScoped<ITeamRecordRepository, TeamRecordRepository>();
builder.Services.AddScoped<ITeamRecordService, TeamRecordService>();
```

Notes and best practices
- Prefer interfaces for application-layer dependencies (`ITeamRecordRepository`, `ITeamRecordService`) so tests can mock them easily.
- Keep mapping logic in the application layer or a dedicated mapper; services should return DTOs from `TeamSearch.Shared.Dtos` where useful.
- Avoid long parameter lists; if a class needs many dependencies consider refactoring into smaller services or a facade.
- For nullable dependencies, prefer explicit null-handling at construction time.

Style rules for Copilot suggestions
- When Copilot proposes a repository/service class, ensure:
  - The class declaration uses primary-constructor syntax by default for DI (e.g., `public class Foo(IBar bar)`).
  - Required using directives are included (e.g., `using TeamSearch.Domain;`).
  - The class implements relevant interfaces and the DI registration is suggested.

If you want these guidelines enforced automatically, add editorconfig/StyleCop rules and a CI job to validate.

--
Generated guideline — edit as needed.


