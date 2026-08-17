#!/usr/bin/env bash
set -euo pipefail
base_url="${1:?Usage: bash eng/smoke-health.sh <base-url>}"
base_url="${base_url%/}"
curl --fail --silent --show-error "$base_url/health" >/dev/null
foundation="$(curl --fail --silent --show-error "$base_url/api/foundation")"
if [[ "$foundation" != *'"name":"Project Builder"'* ]]; then
    echo "Foundation API returned an unexpected product identity." >&2
    exit 1
fi
echo "Health smoke passed for $base_url."
