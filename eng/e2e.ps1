param([switch]$NoBuild)

$ErrorActionPreference = "Stop"
$repositoryRoot = Split-Path -Parent $PSScriptRoot
Push-Location $repositoryRoot
try {
    $env:CI = "true"
    $env:PB_RUN_E2E = "true"
    $env:PLAYWRIGHT_BROWSERS_PATH = Join-Path $repositoryRoot "artifacts/playwright"

    docker info --format '{{.ServerVersion}}' | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "A running Docker-compatible engine is required for PostgreSQL E2E evidence." }

    if (-not $NoBuild) {
        dotnet build tests/ProjectBuilder.EndToEnd.Tests/ProjectBuilder.EndToEnd.Tests.csproj --configuration Release
        if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    }

    & tests/ProjectBuilder.EndToEnd.Tests/bin/Release/net10.0/playwright.ps1 install chromium
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    dotnet test tests/ProjectBuilder.EndToEnd.Tests/ProjectBuilder.EndToEnd.Tests.csproj --configuration Release --no-build --no-restore --report-trx --report-trx-filename "ProjectBuilder.EndToEnd.Tests.trx" --results-directory artifacts/test-results
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}
finally {
    Pop-Location
}
