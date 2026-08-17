param(
    [Parameter(Mandatory = $true)]
    [string]$BaseUrl
)

$ErrorActionPreference = "Stop"
$base = $BaseUrl.TrimEnd("/")
$health = Invoke-WebRequest -UseBasicParsing "$base/health"
$foundation = Invoke-RestMethod "$base/api/foundation"
if ($health.StatusCode -ne 200) { throw "Readiness endpoint returned $($health.StatusCode)." }
if ($foundation.name -ne "Project Builder") { throw "Foundation API returned an unexpected product identity." }
Write-Output "Health smoke passed for $base (version $($foundation.version), revision $($foundation.commit))."
