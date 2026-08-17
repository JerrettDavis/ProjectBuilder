$ErrorActionPreference = "Stop"
$repositoryRoot = Split-Path -Parent $PSScriptRoot
Push-Location $repositoryRoot
try {
    $env:CI = "true"
    $revision = "local"
    if (Test-Path .git) {
        $candidateRevision = git rev-parse --verify HEAD 2>$null
        if ($LASTEXITCODE -eq 0) { $revision = $candidateRevision }
    }
    dotnet build ProjectBuilder.slnx --configuration Release -p:SourceRevisionId=$revision
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}
finally {
    Pop-Location
}
