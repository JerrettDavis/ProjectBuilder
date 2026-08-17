# Project Builder Implementation Plan

## Purpose

This plan turns the product documents into an ordered sequence of vertical slices. It is intentionally model-first. The first usable release is not a drawing surface with generic boxes. It is a small but trustworthy modeling system that can capture one actor, one outcome, one episode, one scenario, one scene, one interaction, its state transition, its invariant, and its evidence.

The project must be able to describe its own development before the team declares the modeling kernel stable.

## Technical baseline

- .NET 10 and C# 14.
- `.slnx` solution format.
- ASP.NET Core Blazor Web App with an Interactive WebAssembly studio area.
- ASP.NET Core Minimal APIs as the stable application boundary.
- PostgreSQL through EF Core 10 for durable persistence.
- SignalR for presence, notifications, and later collaborative editing.
- Aspire AppHost for local development orchestration only.
- OpenTelemetry-compatible logs, metrics, and traces.
- Microsoft.Testing.Platform with one test framework selected repository-wide.
- Central Package Management through `Directory.Packages.props`.
- Modular monolith first. No independently deployed business services until measured evidence requires them.

## Delivery rule

Every session must produce a behavior a human can exercise, an automated proof of that behavior, and an update to the Project Builder model that describes what was built. The model and implementation are reviewed together.

## Phase 0: repository and truth sources

### Slice 0.1: bootstrap the solution
Deliver:

1. `ProjectBuilder.slnx`.
2. `global.json` pinned to the current approved .NET 10 SDK feature band.
3. `Directory.Build.props`, `Directory.Packages.props`, `.editorconfig`, analyzers, nullable reference types, deterministic builds, warnings as errors in CI.
4. AppHost, ServiceDefaults, Web, Web.Client, Domain, Application, Infrastructure, Contracts, and architecture-test projects.
5. PostgreSQL resource in the AppHost.
6. CI that restores, builds, formats or verifies formatting, tests, and publishes test evidence.
7. A repository root `AGENTS.md` based on [the supplied template](09-agent/AGENTS.md.template).

Acceptance:

- A clean clone can run with one documented command.
- CI and local commands use the same SDK and test platform.
- Dependency rules fail the build when Domain references infrastructure or presentation assemblies.
- The home page displays the build version and health status.

### Slice 0.2: encode the first Project Builder model
Deliver a checked-in `dogfood/project-builder-foundation.project-builder.json` containing:

- Project purpose.
- Initial actors.
- The "Create a project" episode.
- One happy scenario and at least three failure scenarios.
- Initial invariants and evidence links.
- The first architectural decisions.

Acceptance:

- The JSON validates against the repository schema.
- A test loads it and verifies all referenced identifiers resolve.
- The file is human-readable and deterministically ordered.

## Phase 1: canonical modeling kernel

### Slice 1.1: create and retrieve a project
A user creates a project with a name, intent, target outcome, and owning workspace.

Behavior:

- Invalid blank or duplicate names are rejected with domain-specific errors.
- Project identity uses a time-ordered GUID.
- Creation records a change set, revision number, actor, timestamp, and reason.
- Querying the project returns the canonical state and current revision.

### Slice 1.2: add actors and outcomes
The user records human, organizational, system, and device participants, then associates outcomes with beneficiaries.

Behavior:

- Actor is a role in a context, not merely a person record.
- Outcome states an observable result and success signal.
- Duplicate concepts are suggested, not silently merged.
- Every outcome must have at least one beneficiary or an explicit "unassigned" gap.

### Slice 1.3: capture the narrative spine
The user adds Episode, Scenario, Scene, Interaction, and Step elements.

Behavior:

- Containment is explicit and ordered.
- Semantic reuse is expressed through relations, not duplicate nesting.
- Every scenario identifies starting conditions, trigger, expected outcome, and path classification.
- Every interaction identifies initiator, receiver, intent, observable response, and authority.

### Slice 1.4: state, rules, and transitions
The user defines facts, presentation state, domain state, preconditions, transitions, invariants, and postconditions.

Behavior:

- Presentation state and domain state are separate types.
- A transition cannot directly mutate an undefined state concept.
- An invariant is attached to the smallest scope that owns it.
- The validator reports missing transitions, contradictory conditions, and unverifiable claims.

### Slice 1.5: change sets, history, and deterministic export
Deliver append-only change sets, current-state persistence, revision reads, project export, and import validation.

Acceptance:

- A user can inspect what changed and why.
- Exporting the same revision twice produces byte-identical canonical JSON.
- Import validates schema, identifiers, model rules, and format version before persistence.
- Corrupt or future-version content is rejected without partial writes.

## Phase 2: guided capture experience

### Slice 2.1: project dashboard
Build the dashboard with project intent, actors, outcomes, recent changes, unresolved gaps, and one clear next action.

### Slice 2.2: guidance rail
Implement the wizard as a context-aware drawer rather than a separate one-way form.

Behavior:

- It can be opened from any selected model element.
- It explains why a question matters.
- It supports Answer, Unknown, Not Applicable, Defer, and Link Existing.
- It never invents model facts from absence.
- Closing and reopening preserves place and unsaved draft safely.

### Slice 2.3: structured editors
Before a freeform canvas, provide reliable editors for actor, outcome, episode, scenario, state, invariant, and boundary records.

Acceptance:

- The complete first POS item-scan slice can be authored without manipulating a canvas.
- Keyboard-only operation is possible.
- Validation appears near the field and in the global Problems panel.

## Phase 3: lenses and canvas

### Slice 3.1: shared lens engine
Create a projection interface that turns the canonical model into lens nodes, edges, groups, ports, and inspector fields.

Initial lenses:

1. Story Map.
2. Scenario Flow.
3. State and Rule.
4. System Context.
5. Traceability.

### Slice 3.2: SVG canvas foundation
Deliver pan, zoom, selection, marquee selection, keyboard movement, connectors, frames, alignment, undo, redo, and persisted layout.

Rules:

- Canvas positions are view state, never domain state.
- Commands, not pointer events, mutate the model.
- Every visual operation has a keyboard equivalent.
- Large graphs use viewport culling and batched rendering.
- The renderer is replaceable so WebGL can be introduced later without changing model semantics.

### Slice 3.3: drilldown and overlays
Double-click, Enter, or an explicit Open command drills into a child context. Breadcrumbs preserve hierarchy. Scenario overlays can animate or step through state and interactions without modifying the model.

## Phase 4: interface designer

### Slice 4.1: interface classification
Support graphical UI, CLI, HTTP API, event/message, MCP, device, document/form, and human procedure interfaces.

Each interface kind receives a specialized editor over common concepts:

- visible or exposed state,
- accepted intents,
- emitted observations,
- authorization,
- validation,
- error representation,
- accessibility or operability constraints,
- contracts and evidence.

### Slice 4.2: graphical interface surface
Add frames, layout regions, text, data display, input, action controls, navigation, status, dialogs, and reusable components.

Controls bind to intents and read models. They do not directly edit domain entities.

### Slice 4.3: scenario-on-interface mapping
Place scenario interactions over a selected interface state. Step through:

1. starting state,
2. actor action,
3. validation,
4. intent dispatch,
5. domain transition,
6. external effects,
7. observation and rendered state,
8. alternate and failure paths.

## Phase 5: boundaries, implementation slices, and evidence

### Slice 5.1: boundary modeling
Add ownership, trust, process, transaction, deployment, data residency, and vendor boundaries. Every crossing can carry a contract, risk, latency expectation, retry policy, and evidence requirement.

### Slice 5.2: implementation projection
Project selected interactions into a vertical slice:

- Presentation adapter.
- Application use case.
- Domain command, facts, rules, transition, and outcomes.
- Infrastructure ports and adapters.
- Contracts.
- Tests and evidence.

The projection is descriptive before it is generative.

### Slice 5.3: specification projection
Generate reviewable artifacts:

- Gherkin-like or TinyBDD-style behavioral specifications.
- State transition tables.
- API or message contracts.
- Acceptance criteria.
- Property test candidates.
- Contract test manifests.
- Traceability matrices.
- Architecture decision prompts.

Generated files are projections. The canonical model remains authoritative.

## Phase 6: collaboration and review

### Slice 6.1: presence and review
SignalR provides presence, selection awareness, comments, review requests, and change notifications.

### Slice 6.2: safe concurrent editing
Begin with optimistic concurrency and node-level conflict reports. Do not implement general CRDT semantics until real collaborative-editing failures have been measured.

### Slice 6.3: baselines and comparisons
Support named baselines, revision comparison, approval, supersession, and release snapshots.

## Phase 7: dogfooding and extensibility

### Slice 7.1: model the full Project Builder MVP
Project Builder must contain a complete internal model for all shipped MVP behavior. Missing coverage becomes visible in the same Problems and Evidence panels available to customers.

### Slice 7.2: meta-model registry
Extract a versioned registry for element kinds, relationship rules, prompts, validators, inspectors, and projections. The registry remains code-defined until an administrator-facing schema editor has its own validated use cases.

### Slice 7.3: source generators and analyzers
Generate strongly typed registries, serializers, exhaustive visitors, schema artifacts, diagnostics, and code fixes. Generated code must be readable, deterministic, reflection-free where practical, and testable with snapshots plus behavior tests.

### Slice 7.4: optional agent assistance
Add an assistant only after the human workflow is complete. Assistance may:

- suggest missing actors, paths, boundaries, or invariants,
- draft descriptions,
- explain validation findings,
- compare alternatives,
- propose tests,
- create an uncommitted change set.

It may not silently commit model changes, manufacture evidence, or make an unverifiable claim appear resolved.

## First public MVP exit criteria

A first public MVP is ready when a new user can model the complete POS item-scan interaction from actor intent through UI, domain transition, external price-book lookup, failure paths, and evidence without editing JSON or writing code.

The system must also:

- preserve revision history,
- export and import deterministically,
- show unresolved gaps,
- generate a behavioral specification and implementation-slice outline,
- support keyboard-only operation for the modeled flow,
- survive concurrent stale edits without data loss,
- provide a documented backup and restore procedure,
- expose health, telemetry, and audit information,
- contain its own MVP model at equivalent depth.
