# Bootstrap `/goal` Prompt

Copy the prompt below into the first implementation session after the documentation folder is added to a fresh repository.

```text
/goal Bootstrap the Project Builder repository and deliver the first executable, evidence-producing vertical foundation.

You are working in a fresh repository for Project Builder, a definition-first visual domain and system modeling studio. Project Builder will ultimately model its own development. This session is limited to repository and runtime foundation. Do not implement speculative product screens, a generic graph engine, agent features, or production code generation.

OUTCOME

A human contributor can clone the repository, run one documented command, open a healthy Project Builder shell, and execute the same deterministic build and test checks used by CI. The repository enforces the initial modular-monolith dependency direction and contains a schema-valid dogfood model stub.

REQUIRED CONTEXT

Read only these files before implementation:

1. docs/README.md
2. docs/00-foundation/02-principles-and-non-negotiables.md
3. docs/00-foundation/03-definition-validated-development.md
4. docs/04-architecture/01-system-context-and-containers.md
5. docs/04-architecture/02-dotnet-solution-and-repository-structure.md
6. docs/04-architecture/03-module-and-dependency-architecture.md
7. docs/05-engineering/01-engineering-standards.md
8. docs/05-engineering/03-testing-and-evidence-strategy.md
9. docs/06-delivery/04-session-sized-implementation-plan.md, Sessions A01 through A05
10. docs/09-agent/AGENTS.md.template

Load other documents only when a concrete conflict requires them. Do not preload the entire docs folder.

TECHNICAL BASELINE

- C# 14 and .NET 10.
- ProjectBuilder.slnx.
- ASP.NET Core Blazor Web App.
- Interactive WebAssembly only for the Studio area when client interactivity is needed; do not create speculative Studio code in this session.
- Minimal APIs as the stable server boundary.
- PostgreSQL and EF Core 10.
- Aspire AppHost for local development orchestration only.
- OpenTelemetry-compatible service defaults.
- Microsoft.Testing.Platform with NUnit is the starting recommendation. Perform a small compatibility proof before applying it repository-wide. If a blocking incompatibility is found, record evidence and choose one repository-wide alternative rather than mixing platforms.
- Central Package Management.
- nullable enabled.
- deterministic builds.
- warnings as errors in CI.
- modular monolith.
- Domain has no EF Core, ASP.NET, UI, provider SDK, or infrastructure dependency.

REPOSITORY DELIVERABLES

Create the repository structure described in the architecture document, including only justified projects:

- src/ProjectBuilder.Domain
- src/ProjectBuilder.Application
- src/ProjectBuilder.Contracts
- src/ProjectBuilder.Infrastructure
- src/ProjectBuilder.Projections
- src/ProjectBuilder.Web
- src/ProjectBuilder.Web.Client when required by the selected Blazor template
- src/ProjectBuilder.AppHost
- src/ProjectBuilder.ServiceDefaults
- tests/ProjectBuilder.Domain.Tests
- tests/ProjectBuilder.Application.Tests
- tests/ProjectBuilder.Infrastructure.Tests
- tests/ProjectBuilder.Api.Tests
- tests/ProjectBuilder.Architecture.Tests
- tests/ProjectBuilder.Contract.Tests

Defer Worker, generators, analyzers, browser E2E, and extra adapter projects until a behavior requires them, unless template/build semantics require a minimal placeholder that is clearly documented.

Add:

- global.json with an approved installed .NET 10 SDK and deliberate roll-forward policy.
- Directory.Build.props.
- Directory.Packages.props.
- .editorconfig.
- ProjectBuilder.slnx.
- README.md with exact restore/build/test/run/verify commands.
- AGENTS.md derived from the supplied template, shortened to repository invariants and disclosure routes.
- CONTRIBUTING.md.
- SECURITY.md.
- CODEOWNERS or a documented placeholder if repository ownership is not yet known.
- eng scripts that work from repository root on Windows PowerShell and POSIX shell where practical.
- CI workflow that invokes the same logical verification entry point.
- build version/commit visible in the application shell.
- health endpoints.
- PostgreSQL development resource and configuration.
- OpenTelemetry service defaults.
- architecture tests.
- schema validation for docs/schemas.
- a minimal dogfood/project-builder-foundation.project-builder.json that validates against the project schema and represents the Create Project outcome at the depth currently supported by the schema.

BEHAVIOR DELIVERED

The running application should show a simple, accessible foundation page with:

- Project Builder name and purpose.
- build version/commit.
- application health.
- links to documentation or repository commands where appropriate.
- no invented dashboard, canvas, or wizard behavior.

ARCHITECTURE RULES TO ENFORCE

- Domain references only approved BCL and explicitly approved domain packages.
- Application can reference Domain and Contracts it owns.
- Infrastructure implements application ports and can reference EF/provider packages.
- Web uses Application/Contracts and composition-root Infrastructure registration.
- Web.Client receives only client-safe contracts.
- Projections consume immutable model snapshots/contracts and do not become a second canonical model.
- AppHost is development orchestration, not a production runtime dependency.
- no generic Services, Managers, Helpers, or Utils dumping grounds.
- no MediatR or equivalent mediator dependency unless a concrete benefit is justified; direct feature handlers are acceptable.
- no base repository abstraction.
- no event sourcing.
- no microservices.
- no agent SDK.

DOGFOOD STUB

Model only enough to represent:

- Project Builder project purpose.
- actors: Modeler, Contributor, Reviewer.
- outcome: contributor can run and verify the repository.
- episode: Bootstrap Repository.
- scenario: clean clone is built and run.
- one invariant: Domain cannot depend on Infrastructure or Presentation.
- evidence placeholders for build, architecture test, and health smoke.

Use stable identifiers, deterministic ordering, explicit format version, and Unknown/Assumed where the current schema permits. Do not expand the schema merely to make the fixture look complete. Record schema gaps.

EVIDENCE

At minimum run and report:

- dotnet --info or exact SDK version.
- restore.
- build in CI-equivalent configuration.
- all tests.
- architecture tests with one proof that a violation would be caught.
- schema validation.
- application startup with PostgreSQL.
- health smoke.
- deterministic repeat of any generated schema/artifact.
- git diff review for local paths, secrets, accidental binaries, or formatting churn.

UI AND MACHINE SAFETY

- Do not steal mouse or keyboard focus.
- Do not move or resize the user's windows.
- Do not interact with windows on other monitors.
- Prefer headless test execution.
- If headed browser execution is required, keep it isolated and do not use global input injection.
- Do not install global software, change OS settings, or modify files outside the repository.
- Do not access production systems or credentials.

STOP AND RECORD A FINDING INSTEAD OF INVENTING WHEN

- installed .NET SDK cannot satisfy .NET 10.
- selected test platform cannot meet IDE/CI/coverage requirements.
- Blazor template creates an unclear client/server contract.
- a package license or compatibility is questionable.
- schema and documented meta-model materially disagree.
- a dependency rule cannot be expressed reliably.
- credentials or an external production resource would be required.

Do not ask for confirmation merely because a reasonable reversible implementation detail is unspecified. Make the narrowest conventional choice, record it, and continue. Do not choose product policy or weaken an invariant without authority.

DEFINITION OF DONE

- clean clone path is documented and rehearsed.
- ProjectBuilder.slnx restores and builds.
- all tests and architecture rules pass.
- local application and PostgreSQL become healthy through the documented command.
- CI invokes repository verification.
- dogfood stub validates and loads in a contract test.
- no speculative product abstractions were added.
- docs and model reflect delivered behavior.
- handoff contains exact commands, evidence, decisions, risks, and next entry point.

HANDOFF FORMAT

## Modeled outcome
## Behavior delivered
## Repository structure
## Model/schema changes
## Architecture rules
## Evidence and exact commands
## Decisions and rationale
## Assumptions and findings
## Known gaps
## Exact next session

The exact next session should normally be B01, strongly typed identity and domain primitives, unless the evidence identifies a foundation correction.
```

## Expected result

This prompt should create a trustworthy launch point, not the appearance of product progress. The next worker should be able to begin the canonical model without repairing build, package, test, orchestration, or repository instruction drift.
