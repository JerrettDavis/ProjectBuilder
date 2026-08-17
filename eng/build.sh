#!/usr/bin/env bash
set -euo pipefail
repository_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repository_root"
export CI=true
revision="local"
if [[ -d .git ]] && candidate_revision="$(git rev-parse --verify HEAD 2>/dev/null)"; then
    revision="$candidate_revision"
fi
dotnet build ProjectBuilder.slnx --configuration Release -p:SourceRevisionId="$revision"
