# Scope, Releases, and Non-Goals

## Release vocabulary

### Prototype
A disposable or partially durable experiment used to answer a named question. Prototype output is not automatically production architecture.

### Internal alpha
Used by the Project Builder team to model Project Builder. Data migrations can be manual, but data loss is still unacceptable.

### Private alpha
Used by invited collaborators on real but controlled projects. Import, export, audit, access control, and recovery become release gates.

### Beta
Supports complete modeled slices, interface design, boundary mapping, projections, and collaboration for external teams. Format and API compatibility policies begin.

### Version 1
A supportable product with stable model format, documented extension boundaries, enterprise-ready identity options, migration guarantees, and proven dogfooding.

## MVP scope

The MVP includes:

- workspace and project creation,
- actors, outcomes, capabilities,
- episodes, scenarios, scenes, interactions, and steps,
- domain and presentation state,
- commands, events, transitions, rules, and invariants,
- happy, alternate, exceptional, degraded, and recovery paths,
- boundaries, systems, interfaces, and contracts,
- structured editors,
- guidance rail and gap engine,
- Story, Scenario Flow, State, System, Traceability, and basic Interface lenses,
- SVG canvas with accessible alternatives,
- revision history and deterministic import/export,
- comments, review, baselines, and optimistic concurrency,
- behavioral and implementation-slice projections,
- POS tutorial,
- dogfood model.

## Post-MVP scope

- richer graphical interface designer and component libraries,
- real-time multi-user semantic merge,
- offline authoring,
- executable simulation,
- generated C# solution scaffolding,
- source-control and CI evidence ingestion,
- plugin SDK,
- enterprise SSO and policy packs,
- existing-system reverse modeling,
- agent-assisted suggestions,
- marketplace or shared template ecosystem.

## Explicit non-goals for MVP

### General-purpose vector design
Project Builder does not compete with every illustration, typography, or prototyping feature in Figma. It provides enough layout and component capability to connect interface design to behavior and state.

### Full IDE replacement
The product does not initially provide arbitrary code editing, debugging, package management, or production deployment from the canvas.

### Automatic truth discovery
It cannot determine business truth from prose, code, or logs without human authority. Importers and agents produce candidates and findings.

### Complete project management
It can generate work packages and link delivery artifacts, but it does not initially replace an issue tracker, sprint planner, time tracker, or portfolio system.

### Universal formal verification
The model may support increasingly formal rules and properties, but MVP does not prove arbitrary programs mathematically.

### Microservice runtime
The model can describe distributed systems. The Project Builder product itself remains a modular monolith until evidence justifies extraction.

### Arbitrary user code in process
Extensions cannot execute untrusted code inside the primary application process.

### Round-trip editing of every projection
Generated prose, diagrams, C#, and schemas are generally one-way outputs. Round-trip behavior is supported only where identity and conflict semantics are defined.

## Scope discipline

A feature enters a release only when it supports a real modeled scenario and has:

- a named beneficiary,
- a measurable outcome,
- a minimal complete interaction,
- failure behavior,
- security and accessibility consideration,
- evidence,
- dogfood use.

A compelling canvas demonstration without semantic identity does not qualify.

## Compatibility policy targets

Before beta:

- Model format versions may change with migration tools.
- Public API is experimental.
- Projection output can change with explicit generator version.

At beta:

- Published model schemas receive documented migration paths.
- Stable model identifiers persist across migrations.
- API breaking changes require versioning and notice.
- Export preserves unknown safe extension content where possible.

At version 1:

- Supported model-format window is declared.
- Upgrade and downgrade limitations are documented.
- Generated artifact compatibility is versioned independently.
- Plugin contracts use explicit compatibility ranges.
