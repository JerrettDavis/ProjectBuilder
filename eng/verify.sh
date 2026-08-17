#!/usr/bin/env bash
set -euo pipefail
repository_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repository_root"
export CI=true
export DOTNET_CLI_TELEMETRY_OPTOUT=1
revision="local"
if [[ -d .git ]] && candidate_revision="$(git rev-parse --verify HEAD 2>/dev/null)"; then
    revision="$candidate_revision"
fi

dotnet --version
dotnet restore ProjectBuilder.slnx
dotnet format ProjectBuilder.slnx --verify-no-changes --no-restore --verbosity minimal
dotnet build ProjectBuilder.slnx --configuration Release --no-restore -p:SourceRevisionId="$revision"
export PLAYWRIGHT_BROWSERS_PATH="$repository_root/artifacts/playwright"
export PB_RUN_E2E=true
export PB_RUN_POSTGRES_TESTS=true
docker info --format '{{.ServerVersion}}' >/dev/null
pwsh tests/ProjectBuilder.EndToEnd.Tests/bin/Release/net10.0/playwright.ps1 install chromium
dotnet test ProjectBuilder.slnx --configuration Release --no-build --no-restore \
    --minimum-expected-tests 1 \
    --report-trx --report-trx-filename "{pname}.trx" \
    --results-directory artifacts/test-results
dotnet test tests/ProjectBuilder.Domain.Tests/ProjectBuilder.Domain.Tests.csproj \
    --configuration Release --no-build --no-restore \
    --minimum-expected-tests 1 \
    --coverage --coverage-output-format cobertura --coverage-output domain.cobertura.xml \
    --results-directory artifacts/test-results/coverage
dotnet list ProjectBuilder.slnx package --vulnerable --include-transitive

if rg --hidden --glob '!.git/**' --glob '!**/bin/**' --glob '!**/obj/**' --glob '!artifacts/**' \
    'AKIA[0-9A-Z]{16}|-----BEGIN (RSA |EC |OPENSSH )?PRIVATE KEY-----|gh[pousr]_[A-Za-z0-9_]{20,}' .; then
    echo "High-confidence secret pattern found." >&2
    exit 1
fi
