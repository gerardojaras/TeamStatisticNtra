<#
dev-bootstrap.ps1

Starts the server, waits until the API responds, then starts the Blazor client.

Usage (from repository root):
  powershell -ExecutionPolicy Bypass -File .\dev-bootstrap.ps1

Optional parameters (PowerShell named args):
  -ServerProject  Path to server project folder (default ./TeamSearch.Server)
  -ClientProject  Path to client project folder (default ./TeamSearch.Client)
  -LaunchProfile  Launch profile to use (default 'https')
  -ApiUrl         URL to poll for readiness (default https://localhost:7216/)
  -MaxAttempts    How many times to poll before giving up (default 60)
  -DelaySeconds   Seconds between polls (default 1)

Notes:
- The script launches `dotnet run` processes in new windows so you can see logs.
- Ensure you trust the HTTPS dev certificate first: `dotnet dev-certs https --trust`.
#>

param(
    [string]$ServerProject = './TeamSearch.Server',
    [string]$ClientProject = './TeamSearch.Client',
    [string]$LaunchProfile = 'https',
    [string]$ApiUrl = 'https://localhost:7216/',
    [int]$MaxAttempts = 60,
    [int]$DelaySeconds = 1,
    [bool]$CreateMigration = $true
)

function Write-Info($txt) { Write-Host $txt -ForegroundColor Cyan }
function Write-Success($txt) { Write-Host $txt -ForegroundColor Green }
function Write-ErrorAndExit($txt) { Write-Host $txt -ForegroundColor Red; exit 1 }

Write-Info "Starting server project: $ServerProject"

# Optional: create a migration and update the database before starting the server
if ($CreateMigration) {
    Write-Info "Creating EF Core migration (if there are model changes)..."
    $timestamp = Get-Date -Format yyyyMMddHHmmss
    $migrationName = "AutoMigration_$timestamp"
    try {
        # Add migration in Infrastructure project using the Server as startup project
        Write-Info "Running: dotnet ef migrations add $migrationName --project ./TeamSearch.Infrastructure --startup-project ./TeamSearch.Server"
        & dotnet ef migrations add $migrationName --project ./TeamSearch.Infrastructure --startup-project ./TeamSearch.Server
    } catch {
        Write-Info "dotnet ef migrations add failed or no changes detected. Continuing..."
    }

    try {
        Write-Info "Applying migrations to the database (dotnet ef database update)..."
        & dotnet ef database update --project ./TeamSearch.Infrastructure --startup-project ./TeamSearch.Server
        Write-Success "Database update completed."
    } catch {
        Write-ErrorAndExit "Database update failed. Ensure dotnet-ef is installed and the startup project is correct."
    }
} else {
    Write-Info "Skipping migration creation and database update (CreateMigration=false)."
}

# Start server in a new process (new window) so logs are visible
$serverArgs = @('run','--project',$ServerProject,'--launch-profile',$LaunchProfile)
$serverProc = Start-Process -FilePath 'dotnet' -ArgumentList $serverArgs -WorkingDirectory $ServerProject -PassThru
Write-Info "Server process started (PID: $($serverProc.Id)). Waiting for API at $ApiUrl ..."

# Poll the API until ready
$ok = $false
for ($i = 0; $i -lt $MaxAttempts; $i++) {
    try {
        Invoke-RestMethod -Uri $ApiUrl -TimeoutSec 5 | Out-Null
        $ok = $true
        Write-Success "API is reachable after $i second(s)."
        break
    } catch {
        Start-Sleep -Seconds $DelaySeconds
    }
}

if (-not $ok) {
    Write-ErrorAndExit "API did not respond at $ApiUrl after $MaxAttempts attempts. Check server logs (PID: $($serverProc.Id))."
}

Write-Info "Starting client project: $ClientProject"
$clientArgs = @('run','--project',$ClientProject,'--launch-profile',$LaunchProfile)
Start-Process -FilePath 'dotnet' -ArgumentList $clientArgs -WorkingDirectory $ClientProject

Write-Success "Server and client start commands issued. Server PID: $($serverProc.Id)" 
Write-Info "If you prefer to see logs inline, run the server and client commands in separate terminals instead of using this bootstrap script."

