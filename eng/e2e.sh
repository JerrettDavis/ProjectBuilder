#!/usr/bin/env bash
set -euo pipefail

repository_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repository_root"
export CI=true
export PB_RUN_E2E=true
export PLAYWRIGHT_BROWSERS_PATH="$repository_root/artifacts/playwright"

docker info --format '{{.ServerVersion}}' >/dev/null
dotnet build tests/ProjectBuilder.EndToEnd.Tests/ProjectBuilder.EndToEnd.Tests.csproj --configuration Release
pwsh tests/ProjectBuilder.EndToEnd.Tests/bin/Release/net10.0/playwright.ps1 install chromium
dotnet test tests/ProjectBuilder.EndToEnd.Tests/ProjectBuilder.EndToEnd.Tests.csproj \
  --configuration Release --no-build --no-restore \
  --report-trx --report-trx-filename "ProjectBuilder.EndToEnd.Tests.trx" \
  --results-directory artifacts/test-results
