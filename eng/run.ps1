$ErrorActionPreference = "Stop"
$repositoryRoot = Split-Path -Parent $PSScriptRoot
Push-Location $repositoryRoot
try {
    if (Test-Path .git) {
        $candidateRevision = git rev-parse --verify HEAD 2>$null
        if ($LASTEXITCODE -eq 0) { $env:GITHUB_SHA = $candidateRevision }
    }
    aspire run --apphost src/ProjectBuilder.AppHost/ProjectBuilder.AppHost.csproj
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}
finally {
    Pop-Location
}
