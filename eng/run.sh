#!/usr/bin/env bash
set -euo pipefail
repository_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repository_root"
if [[ -d .git ]] && candidate_revision="$(git rev-parse --verify HEAD 2>/dev/null)"; then
    export GITHUB_SHA="$candidate_revision"
fi
aspire run --apphost src/ProjectBuilder.AppHost/ProjectBuilder.AppHost.csproj
