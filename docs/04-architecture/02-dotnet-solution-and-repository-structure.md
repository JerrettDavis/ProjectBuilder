# .NET Solution and Repository Structure

## Repository shape

```text
/
├── ProjectBuilder.slnx
├── global.json
├── Directory.Build.props
├── Directory.Build.targets
├── Directory.Packages.props
├── NuGet.config
├── .editorconfig
├── .gitattributes
├── .gitignore
├── README.md
├── AGENTS.md
├── LICENSE
├── SECURITY.md
├── CONTRIBUTING.md
├── docs/
├── dogfood/
├── eng/
│   ├── scripts/
│   ├── pipelines/
│   └── containers/
├── src/
│   ├── ProjectBuilder.AppHost/
│   ├── ProjectBuilder.ServiceDefaults/
│   ├── ProjectBuilder.Web/
│   ├── ProjectBuilder.Web.Client/
│   ├── ProjectBuilder.Contracts/
│   ├── ProjectBuilder.Domain/
│   ├── ProjectBuilder.Application/
│   ├── ProjectBuilder.Infrastructure/
│   ├── ProjectBuilder.Modeling.Runtime/
│   ├── ProjectBuilder.Projections/
│   └── ProjectBuilder.Worker/
├── tests/
│   ├── ProjectBuilder.Domain.Tests/
│   ├── ProjectBuilder.Application.Tests/
│   ├── ProjectBuilder.Infrastructure.Tests/
│   ├── ProjectBuilder.Api.Tests/
│   ├── ProjectBuilder.Web.Tests/
│   ├── ProjectBuilder.Architecture.Tests/
│   ├── ProjectBuilder.Contract.Tests/
│   ├── ProjectBuilder.EndToEnd.Tests/
│   └── ProjectBuilder.ModelFixtures/
├── tools/
│   ├── ProjectBuilder.ModelCli/
│   └── ProjectBuilder.SchemaGenerator/
└── artifacts/
    └── .gitkeep
```

Source generators and analyzers are added when a proven need exists:

```text
src/
├── ProjectBuilder.Modeling.Generators/
└── ProjectBuilder.Modeling.Analyzers/
tests/
├── ProjectBuilder.Modeling.Generators.Tests/
└── ProjectBuilder.Modeling.Analyzers.Tests/
```

## Project responsibilities

### ProjectBuilder.AppHost
Aspire code-first local orchestration. Declares Web, Worker when separate, PostgreSQL, object storage emulator or adapter, and observability resources.

It contains no product logic.

### ProjectBuilder.ServiceDefaults
Common telemetry, health, service discovery, resilience defaults, and environment configuration used by hosted processes. Keep the package small and mechanism-focused.

### ProjectBuilder.Web
Composition root and ASP.NET Core host:

- server-rendered shell,
- authentication,
- Minimal APIs,
- SignalR,
- OpenAPI,
- static assets,
- module registration.

### ProjectBuilder.Web.Client
Interactive WebAssembly studio:

- components,
- state and command clients,
- lenses and rendering,
- structured editors,
- accessibility behavior,
- thin browser interop.

### ProjectBuilder.Contracts
Versioned transport contracts and client abstractions:

- API requests and responses,
- semantic error contracts,
- realtime messages,
- generated JSON context,
- stable enums and identifiers appropriate for the boundary.

Contracts are not domain entities.

### ProjectBuilder.Domain
Pure domain model:

- project and model element semantics,
- value objects,
- rules,
- invariants,
- transitions,
- change-set application,
- revision and baseline semantics,
- domain results and events.

References only the .NET base class libraries and explicitly approved small abstractions.

### ProjectBuilder.Application
Use cases:

- commands and queries,
- handlers,
- authorization policies,
- ports,
- orchestration,
- transactions and unit-of-work boundaries,
- result mapping,
- outbox messages,
- validation coordination.

References Domain and approved Contracts abstractions where appropriate.

### ProjectBuilder.Infrastructure
Adapters:

- EF Core DbContext and mappings,
- repositories and query implementations,
- PostgreSQL,
- object storage,
- identity and integrations,
- outbox dispatch,
- telemetry enrichers,
- file format IO.

References Application and Domain. Domain never references it.

### ProjectBuilder.Modeling.Runtime
Client-safe, platform-neutral modeling services:

- lens projection abstractions,
- immutable graph algorithms,
- layout-neutral geometry models,
- schema metadata,
- deterministic serialization helpers,
- validation result types.

This project must not become a second domain model. It either shares canonical domain records that are client-safe or consumes explicit read contracts.

### ProjectBuilder.Projections
Artifact generation:

- behavioral specification,
- state tables,
- traceability,
- schemas,
- implementation plans,
- C# scaffold descriptors.

Generators consume immutable revision snapshots and produce deterministic outputs.

### ProjectBuilder.Worker
Optional separately hosted background execution. Initially, the same handlers can run through hosted services in Web.

## Feature folders

Inside Domain and Application, organize by bounded module and behavior, not generic technical buckets.

Example:

```text
ProjectBuilder.Application/
└── Modeling/
    ├── CommitChangeSet/
    │   ├── Command.cs
    │   ├── Handler.cs
    │   ├── Authorization.cs
    │   ├── Validation.cs
    │   └── Result.cs
    ├── GetProjectModel/
    ├── ValidateProject/
    └── ExportProject/

ProjectBuilder.Domain/
└── Modeling/
    ├── Elements/
    ├── Relations/
    ├── ChangeSets/
    ├── Validation/
    └── Revisions/
```

Avoid repository-wide `Services`, `Managers`, `Helpers`, and `Utils` folders.

## Project-count rule

A new assembly boundary is justified when at least one is true:

- dependency direction must be compiler-enforced,
- code has a distinct deployment or packaging target,
- the artifact is client-safe versus server-only,
- source-generator or analyzer loading semantics require it,
- a module has stable ownership and needs independent tests,
- a public extension contract must be versioned.

Do not create four projects per feature by default.

## Build configuration

`Directory.Build.props` should establish:

```xml
<Project>
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <LangVersion>14.0</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <TreatWarningsAsErrors Condition="'$(CI)' == 'true'">true</TreatWarningsAsErrors>
    <AnalysisLevel>latest-recommended</AnalysisLevel>
    <Deterministic>true</Deterministic>
    <ContinuousIntegrationBuild Condition="'$(CI)' == 'true'">true</ContinuousIntegrationBuild>
    <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
  </PropertyGroup>
</Project>
```

Pin exact SDK and package versions in the real repository. The documentation intentionally does not freeze patch versions.

## Central package management

Use one root `Directory.Packages.props`.

Policies:

- projects omit package versions,
- version overrides are disabled unless an ADR grants an exception,
- transitive dependency changes are reviewed,
- package source mapping is configured if more than one source exists,
- lock files are considered for deployable applications if the team needs stronger restore repeatability,
- dependency updates run through automated PRs and full evidence.

## Solution format

Use `ProjectBuilder.slnx`. .NET 10 creates SLNX by default. Keep solution folders aligned with repository areas:

```xml
<Solution>
  <Folder Name="/src/">
    ...
  </Folder>
  <Folder Name="/tests/">
    ...
  </Folder>
  <Folder Name="/tools/">
    ...
  </Folder>
</Solution>
```

Do not hand-maintain project GUID noise or rely on IDE-only configuration.

## Repository-wide files

### global.json
Pins approved .NET 10 SDK and roll-forward policy.

### .editorconfig
Defines formatting, naming, analyzer severity, file headers only if truly required, and generated-code treatment.

### .gitattributes
Normalizes text line endings, identifies generated outputs, and configures large binary handling.

### NuGet.config
Uses explicit feeds, package source mapping, signature policy where supported, and no checked-in secrets.

### SECURITY.md
Documents reporting, supported versions, and security contact.

### dogfood/
Stores canonical Project Builder models and fixtures used by tests and internal reviews.

### artifacts/
Local and CI outputs only. No source files depend on this directory.

## Namespace policy

Namespaces follow product and module boundaries:

```text
ProjectBuilder.Domain.Modeling
ProjectBuilder.Application.Modeling.CommitChangeSet
ProjectBuilder.Infrastructure.Persistence.Modeling
ProjectBuilder.Web.Client.Studio.ScenarioFlow
```

Avoid namespace mirroring that creates meaningless depth.

## Internal visibility

- Types are `internal` by default.
- Public types exist for deliberate assembly or package contracts.
- Tests use `InternalsVisibleTo` only where behavior cannot be tested through public boundaries, with an explicit rationale.
- Domain value objects and results can be public within the solution when they form stable semantic contracts.

## Generated files

Generated files:

- live under `obj/` by default,
- use clear `.g.cs` names,
- include generator name and source identifiers,
- are deterministic,
- do not require checkout unless a published artifact specifically needs committed output,
- are inspected in CI snapshot tests.

## Bootstrap commands

Target developer experience:

```bash
dotnet restore
aspire run
dotnet test
```

Additional repository scripts may wrap these commands but must not hide required environment setup or diverge from CI.
