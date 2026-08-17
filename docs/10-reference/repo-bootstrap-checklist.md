# Repository Bootstrap Checklist

## Prerequisites

- approved .NET 10 SDK installed,
- Docker or approved PostgreSQL development path,
- Git,
- repository ownership and license decision,
- CI platform,
- package-source policy.

## Repository root

- [ ] `ProjectBuilder.slnx`
- [ ] `global.json`
- [ ] `Directory.Build.props`
- [ ] `Directory.Packages.props`
- [ ] `.editorconfig`
- [ ] `.gitignore`
- [ ] `README.md`
- [ ] `AGENTS.md`
- [ ] `CONTRIBUTING.md`
- [ ] `SECURITY.md`
- [ ] `CODEOWNERS`
- [ ] license file or explicit private-license notice
- [ ] `eng/` command scripts
- [ ] `docs/` package copied intact
- [ ] `dogfood/` fixture location

## Solution

- [ ] AppHost
- [ ] ServiceDefaults
- [ ] Web
- [ ] Web.Client only if required
- [ ] Domain
- [ ] Application
- [ ] Contracts
- [ ] Infrastructure
- [ ] Projections
- [ ] justified test projects
- [ ] no speculative projects

## Build governance

- [ ] `net10.0`
- [ ] C# 14
- [ ] nullable
- [ ] deterministic build
- [ ] analyzers
- [ ] warnings as errors in CI
- [ ] central package versions
- [ ] package source mapping if needed
- [ ] SDK pin and roll-forward
- [ ] build version metadata

## Architecture proof

- [ ] Domain provider/UI/framework isolation
- [ ] Application dependency direction
- [ ] Infrastructure adapter boundary
- [ ] client-safe contract boundary
- [ ] no forbidden project cycles
- [ ] public-surface policy
- [ ] one test proves a violation is detected

## Runtime

- [ ] PostgreSQL development resource
- [ ] migrations or bootstrap schema
- [ ] health
- [ ] OpenTelemetry
- [ ] safe logging
- [ ] configuration validation
- [ ] secret mechanism
- [ ] one-command local run

## Tests

- [ ] one repository-wide test platform
- [ ] unit/example test
- [ ] property test seed
- [ ] real PostgreSQL integration test
- [ ] API/health test
- [ ] architecture test
- [ ] schema/fixture contract test
- [ ] deterministic artifact test

## CI

- [ ] restore
- [ ] build
- [ ] test and results
- [ ] architecture
- [ ] format/analyzers
- [ ] schema
- [ ] dependency/license/security
- [ ] secret scan
- [ ] artifact retention
- [ ] same logical command as local
- [ ] branch and PR policy

## Dogfood

- [ ] Project Builder project purpose
- [ ] initial actors and outcome
- [ ] bootstrap episode/scenario
- [ ] architecture invariant
- [ ] evidence placeholders
- [ ] stable IDs and deterministic order
- [ ] schema-valid fixture

## Rehearsal

- [ ] clean clone
- [ ] exact README commands
- [ ] no machine-specific path
- [ ] database starts
- [ ] application healthy
- [ ] all checks pass
- [ ] failure diagnostics useful
- [ ] package licenses reviewed
- [ ] no secret or binary accident
- [ ] next session identified
