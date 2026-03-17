---
name: csharp-xunit
description: "Guidance, templates and best practices for writing and running xUnit tests for TeamSearch (targeting .NET 10)."
---

# xUnit Skill for TeamSearch

Purpose
- Provide concise, actionable guidance and templates for writing reliable xUnit tests for this repository.
- Include recommended packages, example test templates (unit + integration), coverage and CI snippets, plus EF Core testing guidance.

Scope
- Unit tests for services, mappers and controllers (mock dependencies where possible).
- Small integration tests for EF Core using disposable SQLite or the Microsoft-provided IDbContextFactory<T>.

Quick facts for this repo
- Solution target: .NET 10 (projects in this repo use net10.0)
- Test project: `TeamSearch.Tests`
- Shared DTOs / queries: `TeamSearch.Shared`

Recommended packages
- Microsoft.NET.Test.Sdk: 18.3.0
- xunit: 2.6.3
- xunit.runner.visualstudio: 2.8.0
- coverlet.collector: 6.0.1 (or the latest patched version your policy allows)
- Moq (or NSubstitute) for mocking (optional, choose one)
- FluentAssertions for expressive assertions (recommended)

Example `TeamSearch.Tests.csproj` snippet

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.NET.Test.Sdk" Version="18.3.0" />
  <PackageReference Include="xunit" Version="2.6.3" />
  <PackageReference Include="xunit.runner.visualstudio" Version="2.8.0" />
  <PackageReference Include="coverlet.collector" Version="6.0.1" />
  <PackageReference Include="Moq" Version="4.21.0" />
</ItemGroup>

<ItemGroup>
  <ProjectReference Include="..\TeamSearch.Shared\TeamSearch.Shared.csproj" />
  <ProjectReference Include="..\TeamSearch.Domain\TeamSearch.Domain.csproj" />
  <ProjectReference Include="..\TeamSearch.Application\TeamSearch.Application.csproj" />
  <ProjectReference Include="..\TeamSearch.Infrastructure\TeamSearch.Infrastructure.csproj" />
</ItemGroup>
```

Test structure and practices
- Follow Arrange-Act-Assert (AAA).
- Use `[Fact]` for single-case tests, `[Theory]` + `[InlineData]`/`[MemberData]` for data-driven tests.
- Name tests clearly: `MethodOrBehavior_Scenario_ExpectedResult`.
- Keep tests small, focused and deterministic (avoid time, file system or network dependence).
- Use constructors for per-test setup and `IDisposable` for cleanup. Use `IClassFixture<T>` or `ICollectionFixture<T>` for shared context.

Assertions and failures
- Prefer specific assertions (e.g., `Assert.Equal`, `Assert.ThrowsAsync<T>`).
- When checking collections, assert count and a couple of representative elements rather than entire dumps.

Mocking and isolation
- Mock external dependencies (repositories, HTTP clients, etc.) to keep unit tests fast.
- Use an IoC-friendly design (interfaces, constructor injection) so tests can replace implementations easily.

EF Core / Integration testing guidance
- Prefer using `IDbContextFactory<T>` (registered with `AddDbContextFactory<T>()`) and create a fresh `DbContext` for each test to avoid shared state.
- For reliable behavior use a file-based SQLite database created in the test's temp folder or an in-memory SQLite with "Mode=Memory;Cache=Shared" and an explicit connection lifetime per test.
- Ensure migrations are applied for integration tests that exercise schema-specific behavior.

Example unit test (existing pattern) — `TeamRecordQueryTests.cs`

```csharp
using TeamSearch.Shared;
using Xunit;

namespace TeamSearch.Tests;

public class TeamRecordQueryTests
{
    [Fact]
    public void DefaultValues_AreExpected()
    {
        var q = new TeamRecordQuery();
        Assert.Equal(1, q.Page);
        Assert.Equal(20, q.PageSize);
        Assert.Equal("asc", q.SortDir);
        Assert.Null(q.Search);
        Assert.Null(q.SortBy);
    }
}
```

Example unit tests for `TeamRecordService` (Moq + FluentAssertions)

Below are two practical examples showing how to unit-test a service that reads domain entities from a repository and returns DTOs. Adjust the constructor/mapping parts to match your real `TeamRecordService` signature (AutoMapper, a custom mapper interface, or a Func mapper).

1) Service that depends on a repository and an IMapper-like abstraction

```csharp
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Xunit;

// Adjust namespaces to match your project layout
using TeamSearch.Domain;
using TeamSearch.Shared;

namespace TeamSearch.Tests;

public class TeamRecordServiceTests
{
    [Fact]
    public async Task ListAsync_WhenRecordsExist_ReturnsMappedDtos()
    {
        // Arrange
        var domainRecords = new[]
        {
            new TeamRecord { Id = 1, Team = "Alpha", Mascot = "A", Wins = 10, WinningPercentage = 0.5m },
            new TeamRecord { Id = 2, Team = "Beta", Mascot = "B", Wins = 8, WinningPercentage = 0.44m }
        };

        var repo = new Mock<ITeamRecordRepository>();
        repo.Setup(r => r.ListAsync()).ReturnsAsync(domainRecords);

        // Example mapper abstraction; replace with your project's mapper (AutoMapper IMapper, custom interface, etc.)
        var mapper = new Mock<System.Func<TeamRecord, TeamRecordDto>>();
        mapper.Setup(m => m(It.IsAny<TeamRecord>()))
              .Returns<TeamRecord>(d => new TeamRecordDto { Id = d.Id, Team = d.Team, Mascot = d.Mascot, Wins = d.Wins, WinningPercentage = d.WinningPercentage });

        var service = new TeamRecordService(repo.Object, mapper.Object);

        // Act
        var result = await service.ListAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result[0].Team.Should().Be("Alpha");
        result[1].Wins.Should().Be(8);
        repo.Verify(r => r.ListAsync(), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotFound_ReturnsNull()
    {
        // Arrange
        var repo = new Mock<ITeamRecordRepository>();
        repo.Setup(r => r.GetByIdAsync(123)).ReturnsAsync((TeamRecord?)null);

        var mapper = new Mock<System.Func<TeamRecord, TeamRecordDto>>();
        var service = new TeamRecordService(repo.Object, mapper.Object);

        // Act
        var result = await service.GetByIdAsync(123);

        // Assert
        result.Should().BeNull();
        repo.Verify(r => r.GetByIdAsync(123), Times.Once);
    }
}
```

2) Variant using AutoMapper (if your project uses AutoMapper's `IMapper`)

```csharp
using System.Threading.Tasks;
using AutoMapper;
using FluentAssertions;
using Moq;
using Xunit;

using TeamSearch.Domain;
using TeamSearch.Shared;

namespace TeamSearch.Tests;

public class TeamRecordService_AutoMapper_Tests
{
    [Fact]
    public async Task GetByIdAsync_WhenFound_ReturnsDto()
    {
        // Arrange
        var record = new TeamRecord { Id = 42, Team = "Gamma", Mascot = "G", Wins = 12, WinningPercentage = 0.6m };
        var repo = new Mock<ITeamRecordRepository>();
        repo.Setup(r => r.GetByIdAsync(42)).ReturnsAsync(record);

        var mapper = new Mock<IMapper>();
        mapper.Setup(m => m.Map<TeamRecordDto>(It.IsAny<TeamRecord>()))
              .Returns<TeamRecord>(d => new TeamRecordDto { Id = d.Id, Team = d.Team, Mascot = d.Mascot, Wins = d.Wins, WinningPercentage = d.WinningPercentage });

        var service = new TeamRecordService(repo.Object, mapper.Object);

        // Act
        var dto = await service.GetByIdAsync(42);

        // Assert
        dto.Should().NotBeNull();
        dto!.Id.Should().Be(42);
        dto.Team.Should().Be("Gamma");
    }
}
```

Notes
- Replace `TeamRecordService(repo.Object, mapper.Object)` with the real constructor signature used by your service. If your service takes `IMapper` (AutoMapper) use the AutoMapper example; if it takes a custom mapping function or interface, use the first example.
- Add `FluentAssertions` to the recommended packages list in this SKILL (example csproj already updated). FluentAssertions makes assertions more expressive and reduces brittle test code.
- If you prefer not to inject a mapper in unit tests, you can exercise the real mapping by constructing the real mapper or using the mapping helper used in production (still keep repository mocked).

Running tests locally (PowerShell)

```powershell
cd "C:\Users\gerar\Code\Code26\JobTests\Ntra\TeamStatistics"
dotnet restore
dotnet test TeamSearch.Tests\TeamSearch.Tests.csproj
```

Collect code coverage

```powershell
dotnet test --collect:"XPlat Code Coverage"
```

CI snippet (GitHub Actions)

```yaml
name: .NET Tests
on: [push, pull_request]
jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.x'
      - name: Restore
        run: dotnet restore
      - name: Test
        run: dotnet test --no-build --verbosity normal
      - name: Check for vulnerable packages
        run: dotnet list package --vulnerable || true
```

Security and dependency hygiene
- Pin test-tooling package versions in the csproj to avoid unexpected CI breakages.
- Add `dotnet list package --vulnerable` to CI or run it in a scheduled job to detect new advisories.

Where to put this SKILL
- Keep this file under `skills/csharp-xunit/SKILL.md`. Optionally duplicate or reference it from the repo root README or `.git/` if you prefer global discoverability.

How to register/use this skill in local dev
- Add the markdown under `skills/` (done). When asking the Copilot/assistant to add tests, reference this skill name or path so templates and patterns are followed.

Follow-ups I can do for you
- Add a sample `TeamRecordService` unit test using Moq and FluentAssertions.
- Add an integration test that uses `IDbContextFactory<TeamSearchDbContext>` and a disposable SQLite DB.
- Create a GitHub Actions workflow that runs tests and uploads coverage.
- Scan all projects for test-tooling package versions and propose safe upgrades.

Feedback
- Tell me which follow-up you'd like and I will implement it: add tests, CI, or integration tests.

