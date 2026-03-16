# csharp-xunit skill for this repository

This folder is intended to hold the GitHub Copilot "csharp-xunit" skill manifest or notes for contributors who want to use the skill to generate or improve xUnit tests in this repository.

The original skill content is maintained at:
https://github.com/github/awesome-copilot/blob/main/skills/csharp-xunit/SKILL.md

Because that upstream file may change, we include below a short, repository-specific guide and a minimal example so the skill can be exercised locally by contributors.

What this file contains
- A short summary of how to use the csharp-xunit skill with this repo.
- A checklist of repository touchpoints (where tests live, how to run them).
- Notes on serialization / naming conventions that the skill may need to know.

Repository-specific guidance
1. Tests project: TeamSearch.Tests (path: TeamSearch.Tests/)
   - This repo already includes a test project `TeamSearch.Tests` (SDK-style .NET test project).
   - To run tests locally: `dotnet test TeamSearch.Tests/TeamSearch.Tests.csproj` from the repository root.

2. Existing projects to consider when generating tests:
   - TeamSearch.Domain (domain models)
   - TeamSearch.Shared (shared DTOs like `TeamRecordQuery`)
   - TeamSearch.Application (application logic: services)
   - TeamSearch.Infrastructure (data access and mapping code)

3. How to exercise the skill
   - Open an editor in the repository and point Copilot to a target class you want tests for (for example `TeamRecordQuery` in `TeamSearch.Shared`).
   - Use the csharp-xunit skill prompt templates (see upstream SKILL.md) or instruct Copilot to "generate xUnit tests for <Type/Method>".
   - Review and adapt generated tests to your project's conventions (naming, DI, factories).

4. Running tests in CI
   - Add or update GitHub Actions to run `dotnet test` on push/PR. The repo already builds successfully. If you add tests, ensure CI runs them.

5. If you need to preserve legacy JSON property names
   - If any tests depend on serialized names (for example legacy "q" query-string), consider adding `[JsonPropertyName("q")]` to the `Search` property or write tests that use System.Text.Json options.

Example: quick test you can use as a starting point
- See `TeamSearch.Tests/TeamRecordQueryTests.cs` (a minimal xUnit test that asserts default values for `TeamRecordQuery`).

Notes and links
- Upstream skill: https://github.com/github/awesome-copilot/tree/main/skills/csharp-xunit
- If you want the exact SKILL.md content from upstream, copy it into this file. We intentionally keep this file as a lightweight, repo-specific guide to avoid license or drift concerns.

If you'd like, I can:
- paste the upstream SKILL.md contents into this file (you'll need to confirm you want to copy it),
- create additional example tests for specific services or mappers in the project,
- add a GitHub Actions workflow that runs tests on PRs and pushes.

