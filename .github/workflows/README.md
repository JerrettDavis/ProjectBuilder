# Workflows

| Workflow | Trigger | Purpose |
|---|---|---|
| [`verify.yml`](verify.yml) | push to `main`, all pull requests | Canonical CI entry point. Runs `eng/verify.sh` (format check, restore, build, full test suite with coverage, headless-browser E2E evidence, vulnerable-package audit, secret scan), boots the `web` app against a real PostgreSQL service container, and smoke-tests `/health`. This is the only build/test path — do not duplicate it in another workflow. |
| [`codeql-analysis.yml`](codeql-analysis.yml) | push/PR to `main`, weekly (Sun 12:00 UTC) | CodeQL static analysis for C#. |
| [`dependency-review.yml`](dependency-review.yml) | pull requests to `main` | Fails PRs that introduce moderate+ severity vulnerable dependencies or copyleft-licensed packages (GPL/AGPL). |
| [`labeler.yml`](labeler.yml) | PR opened/updated, issue opened | Applies area labels from [`../labeler.yml`](../labeler.yml) based on changed files, and a size label based on diff size. |
| [`stale.yml`](stale.yml) | daily cron | Marks and closes inactive issues (60d/7d) and PRs (30d/14d). |

Dependency updates are managed by Dependabot ([`../dependabot.yml`](../dependabot.yml)) for `nuget` and `github-actions` ecosystems, grouped weekly with a 48-hour cooldown.
