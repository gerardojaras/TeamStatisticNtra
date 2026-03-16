# Fluent UI Blazor Skill (TeamSearch)

Use this skill to design and implement Fluent UI Blazor components in this repository.

## Project context

- Client project: `TeamSearch.Client`
- UI framework today: Blazor WebAssembly + Bootstrap
- Shared contracts: `TeamSearch.Shared`
- Current search page: `TeamSearch.Client/Pages/Search.razor`

## What this skill should do here

When asked to use Fluent UI in this repo, prefer:

1. Converting page markup in `TeamSearch.Client/Pages/*.razor` from Bootstrap classes to Fluent UI components.
2. Keeping data/query logic in frontend services (`TeamSearch.Client/Services/*`) and shared DTOs (`TeamSearch.Shared`).
3. Avoiding breaking route structure and API contracts.
4. Preserving accessibility (labels, keyboard navigation, focus states).

## Recommended setup steps (if not already installed)

1. Add package to `TeamSearch.Client/TeamSearch.Client.csproj`:

```xml
<PackageReference Include="Microsoft.FluentUI.AspNetCore.Components" Version="4.*" />
```

2. Register Fluent UI services in `TeamSearch.Client/Program.cs`:

```csharp
builder.Services.AddFluentUIComponents();
```

3. Add namespace import in `TeamSearch.Client/_Imports.razor`:

```razor
@using Microsoft.FluentUI.AspNetCore.Components
```

4. Optionally replace Bootstrap controls in `Search.razor` with Fluent equivalents:
- `FluentTextField` for search input
- `FluentSelect` for sort fields
- `FluentCheckbox` for column selection
- `FluentButton` for actions
- `FluentDataGrid` or Fluent table pattern for results

## Prompt templates for this repo

- "Use the Fluent UI Blazor skill to convert `TeamSearch.Client/Pages/Search.razor` from Bootstrap to Fluent components, keeping existing service calls unchanged."
- "Apply Fluent UI styles to `TeamSearch.Client/Layout/NavMenu.razor` without changing routes."
- "Refactor this component to Fluent UI and keep all API calls in `ITeamRecordClient`/`TeamRecordClientService`."

## Validation checklist

After changes, run:

```powershell
dotnet restore TeamSearch.slnx
dotnet build TeamSearch.Client/TeamSearch.Client.csproj
dotnet build TeamSearch.Server/TeamSearch.Server.csproj
```

If page behavior changed, also run the app and verify:
- Search still calls `/TeamRecords`
- Sorting and paging still work
- No CORS error in browser console

## Notes

- Prefer incremental migration page-by-page.
- Keep DTO usage from `TeamSearch.Shared` as-is.
- Keep data fetching in services, not inline in page markup.

