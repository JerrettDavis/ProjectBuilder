# CI/CD and Release Engineering

## Pipeline principles

- Build once, promote immutable artifacts.
- Local and CI commands match.
- Every pipeline stage produces evidence.
- Generated output is reproducible.
- No production secret is available to pull-request code.
- Model schema and dogfood validation are release gates.
- Deployments are traceable to source, artifact, dependencies, and product-model baseline.

## Pull-request pipeline

Required jobs:

1. Repository policy.
2. Restore.
3. Build.
4. Format or style verification.
5. Unit and property tests.
6. Architecture tests.
7. Schema and dogfood model validation.
8. integration tests with PostgreSQL.
9. API and contract tests.
10. Client component tests.
11. Browser smoke tests.
12. security and dependency scans.
13. Generator determinism and compile tests.
14. Documentation link and Mermaid validation where tooling permits.

Parallelize after build outputs are available.

## Main pipeline

Adds:

- full end-to-end suite,
- migration tests,
- large fixture tests,
- performance smoke budgets,
- container build,
- SBOM,
- provenance,
- signed or attested artifacts according to deployment needs,
- vulnerability scan,
- preview or development deployment,
- dogfood scenario run,
- evidence manifest publication.

## Release candidate pipeline

Adds:

- staging deployment,
- production-like migration rehearsal,
- backup and restore check,
- selected load and resilience tests,
- accessibility review evidence,
- security review evidence,
- release model baseline approval,
- changelog and compatibility report,
- rollback or forward-recovery verification.

## GitHub Actions conceptual workflow

```yaml
name: pull-request

on:
  pull_request:

permissions:
  contents: read

jobs:
  build-test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          global-json-file: global.json
      - run: dotnet restore --locked-mode
      - run: dotnet build ProjectBuilder.slnx --no-restore -c Release
      - run: dotnet test ProjectBuilder.slnx --no-build -c Release
      - run: dotnet run --project tools/ProjectBuilder.ModelCli -- validate dogfood
```

The real workflow pins actions by full commit SHA according to supply-chain policy.

## Build outputs

- Web container.
- optional Worker container.
- CLI tool packages.
- project schemas.
- generated documentation.
- test results.
- coverage report.
- evidence manifest.
- SBOM.
- provenance.
- database migration bundle or manifest.
- release notes.
- compatibility matrix.

## Versioning

Use semantic product versions:

- major for breaking public product or format compatibility,
- minor for backward-compatible capability,
- patch for fixes.

Track independently:

- application version,
- model format version,
- public API version,
- projection generator versions,
- plugin SDK version.

Commit-based informational versions identify previews.

## Release channels

### Nightly
Automated main build for internal testing. No support promise.

### Preview
Named milestone or prerelease for collaborators.

### Stable
Supported release.

### LTS
Only after the product and support organization can maintain it. Do not label early releases LTS aspirationally.

## Database compatibility

A deployment matrix states:

```text
Application N can read schema N and N-1 during rollout.
Application N-1 can continue against expanded schema N until cutover.
Destructive cleanup occurs in N+1 or later.
```

The exact window depends on migration design.

## Model-format compatibility

Release notes identify:

- oldest import version,
- export version,
- automatic migrations,
- semantic migration warnings,
- downgrade limitations,
- extension compatibility.

## Release branching

Default:

- `main` is releasable.
- short-lived feature branches.
- tags mark releases.
- a release branch exists only when stabilizing or maintaining a supported line requires parallel fixes.
- hotfixes begin from the supported tag or branch and merge forward.

Avoid permanent develop and multiple environment branches.

## Deployment strategies

- rolling deployment for compatible changes,
- blue-green for higher-risk Web changes where platform supports it,
- maintenance window for incompatible migration,
- feature activation after schema readiness,
- canary for hosted production when traffic and observability justify it.

## Feature flags

Use for:

- controlled rollout,
- risky integration,
- incomplete UI path hidden from ordinary users,
- experiment with explicit cleanup.

Do not use flags to leave two permanent architectures in place. Each flag has owner, purpose, introduced version, removal condition, and test matrix.

## Release evidence packet

- approved model baseline,
- source and artifact identifiers,
- dependency and SBOM results,
- test and evidence summary,
- migration results,
- security and accessibility findings,
- performance comparison,
- known gaps and accepted risks,
- deployment and rollback plan,
- support notes.

## Rollback decision

Before deploy, classify:

- image rollback safe,
- schema backward compatible,
- model-format backward compatible,
- background jobs compatible,
- external side effects reversible or compensatable.

Do not advertise one-button rollback when data transformations make it false.

## Hotfix process

1. Reproduce and capture divergence.
2. Add failing evidence.
3. identify affected release and model claim.
4. branch from supported line.
5. implement smallest safe correction.
6. run complete relevant evidence.
7. update release model and changelog.
8. deploy.
9. merge or cherry-pick forward with conflict review.
10. conduct learning review for material incident.

## Dependency updates

Automated dependency PRs include:

- release notes,
- transitive changes,
- license changes,
- vulnerability context,
- compatibility evidence.

Patch automatically only under policy and after full tests. Framework and provider major updates require ADR or upgrade plan.

## Pipeline security

- least permissions,
- OIDC workload identity instead of long-lived cloud secrets,
- untrusted PRs cannot access deployment credentials,
- pin third-party actions,
- verify downloaded tools,
- protect release environments,
- require review for production,
- retain audit and provenance,
- sanitize test artifacts.

## Pipeline failure ownership

Each required job has:

- owning module or team,
- triage guide,
- flaky-test policy,
- artifact retention,
- escalation.

A broken main branch blocks new merges until restored or an explicit authorized exception is recorded.
