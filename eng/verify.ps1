$ErrorActionPreference = "Stop"
$repositoryRoot = Split-Path -Parent $PSScriptRoot
Push-Location $repositoryRoot
try {
    $env:CI = "true"
    $env:DOTNET_CLI_TELEMETRY_OPTOUT = "1"
    $revision = "local"
    if (Test-Path .git) {
        $candidateRevision = git rev-parse --verify HEAD 2>$null
        if ($LASTEXITCODE -eq 0) { $revision = $candidateRevision }
    }

    dotnet --version
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    dotnet restore ProjectBuilder.slnx
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    dotnet format ProjectBuilder.slnx --verify-no-changes --no-restore --verbosity minimal
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    dotnet build ProjectBuilder.slnx --configuration Release --no-restore -p:SourceRevisionId=$revision
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    $env:PLAYWRIGHT_BROWSERS_PATH = Join-Path $repositoryRoot "artifacts/playwright"
    $env:PB_RUN_E2E = "true"
    $env:PB_RUN_POSTGRES_TESTS = "true"
    docker info --format '{{.ServerVersion}}' | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "A running Docker-compatible engine is required for PostgreSQL E2E evidence." }
    & tests/ProjectBuilder.EndToEnd.Tests/bin/Release/net10.0/playwright.ps1 install chromium
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    dotnet test ProjectBuilder.slnx --configuration Release --no-build --no-restore --minimum-expected-tests 1 --report-trx --report-trx-filename "{pname}.trx" --results-directory artifacts/test-results
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    dotnet test tests/ProjectBuilder.Domain.Tests/ProjectBuilder.Domain.Tests.csproj --configuration Release --no-build --no-restore --minimum-expected-tests 1 --coverage --coverage-output-format cobertura --coverage-output domain.cobertura.xml --results-directory artifacts/test-results/coverage
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    dotnet list ProjectBuilder.slnx package --vulnerable --include-transitive
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    rg --hidden --glob "!.git/**" --glob "!**/bin/**" --glob "!**/obj/**" --glob "!artifacts/**" "AKIA[0-9A-Z]{16}|-----BEGIN (RSA |EC |OPENSSH )?PRIVATE KEY-----|gh[pousr]_[A-Za-z0-9_]{20,}" .
    if ($LASTEXITCODE -eq 0) { throw "High-confidence secret pattern found." }
    if ($LASTEXITCODE -gt 1) { exit $LASTEXITCODE }
}
finally {
    Pop-Location
}
