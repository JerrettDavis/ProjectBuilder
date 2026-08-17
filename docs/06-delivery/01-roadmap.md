# Product and Delivery Roadmap

## Purpose

The roadmap orders learning, product capability, and architectural commitment. It is not a promise that every listed capability will ship unchanged. Each horizon ends with an observable user outcome and a decision point. Later horizons remain options until evidence from earlier horizons justifies them.

Project Builder should grow from a trustworthy definition kernel into a visual studio, then into a specification and implementation workbench, and only then into a partially executable visual programming environment.

## Roadmap rules

1. Deliver complete vertical slices before broad editor coverage.
2. Prefer typed structured editors before unrestricted canvas behavior.
3. Keep one canonical semantic model and treat every screen, diagram, document, and generated file as a projection.
4. Preserve a complete human path before adding agent assistance.
5. Dogfood each shipped modeling capability against Project Builder itself.
6. Do not distribute business behavior into independently deployed services without measured operational need.
7. Do not generate production code until the model can generate trustworthy specifications and tests.
8. Every horizon must improve both model expressiveness and evidence quality.

## Horizon 0: repository as a proof-bearing system

### Goal

Create a repository in which architecture, build behavior, tests, evidence, and agent operation are deterministic enough for several contributors to work safely.

### User-visible outcome

A developer can clone the repository, run the application, execute all checks, and inspect the checked-in dogfood model.

### Capabilities

- .NET 10 solution and build governance.
- Modular-monolith boundaries.
- Local PostgreSQL orchestration.
- Health checks and OpenTelemetry.
- Test platform, architecture tests, and deterministic CI.
- JSON Schema for the initial model format.
- Dogfood fixture for project creation.
- Repository-level progressive-disclosure instructions.

### Exit evidence

- Clean-clone rehearsal.
- CI produces build, test, architecture, schema, and dependency evidence.
- Dogfood JSON validates and loads.
- One command starts the local system.
- No product project references a forbidden layer.

### Decision gate

Confirm the proposed aggregate boundaries, test platform, client render mode, and canonical serialization strategy before expanding the domain model.

## Horizon 1: definition kernel

### Goal

Capture a coherent, revisioned behavioral model without relying on a visual canvas.

### User-visible outcome

A modeler can create a project, identify actors and outcomes, describe a scenario, define state and invariants, record paths, and export the result.

### Capabilities

- Project, Context, Capability, Outcome.
- Actor and authority.
- Episode, Scenario, Scene, Interaction, Step.
- Intent, Observation, State, Fact, Rule, Invariant, Transition.
- Happy, alternate, exceptional, degraded, recovery, cancellation, and compensation paths.
- Typed relations and containment.
- Knowledge states: Known, Unknown, Assumed, Deferred, Disputed, Not Applicable.
- Append-only change sets and current revision.
- Structured editors.
- Validation findings and purpose-specific completeness.
- Deterministic import and export.
- Basic history and revision comparison.

### Dogfood target

Model the complete "Create Project" episode and use it to validate project creation, actor capture, scenario editing, change history, and export.

### Exit evidence

A new user can author the POS item-scan scenario through structured forms without editing JSON. The resulting project passes the Discovery and Interface Design purpose profiles.

### Decision gate

Evaluate whether the canonical model feels specific enough to guide behavior while remaining adaptable across software, human, and device-heavy systems.

## Horizon 2: guided studio

### Goal

Make rigorous modeling approachable without making the workflow simplistic or linear.

### User-visible outcome

A user can enter through a guided path, move freely into the studio, see gaps, answer or classify questions, and return to the guide without losing context.

### Capabilities

- Stable Studio shell.
- Explorer, work surface, Inspector, Guide Rail, and Problems/Evidence/History panels.
- Context-aware prompts.
- Suggested next action.
- Purpose-profile selection.
- Gap map and completeness explanation.
- Command palette and comprehensive keyboard path.
- Search and cross-reference navigation.
- Autosaved drafts plus explicit semantic commit.
- Reviewable change-set preview.

### Dogfood target

Run the Project Builder MVP discovery workshop inside the application. Record decisions, assumptions, open questions, and evidence against the modeled product.

### Exit evidence

A facilitator and domain expert can complete a ninety-minute modeling session without developer assistance, and every unresolved point is visibly classified rather than silently omitted.

### Decision gate

Assess comprehension, prompt quality, user fatigue, terminology, and whether the hierarchy needs domain-specific aliases.

## Horizon 3: synchronized lenses and visual modeling

### Goal

Render the same semantic truth through purpose-built visual lenses.

### User-visible outcome

A modeler can inspect and edit the model as a story map, scenario flow, state model, system context, interface flow, and traceability graph.

### Capabilities

- Shared lens projection contract.
- Accessible SVG canvas.
- Selection, connection, containment, framing, layout, pan, zoom, and keyboard alternatives.
- Drilldown and breadcrumbs.
- Scenario path overlays.
- Saved semantic filters and private/team layouts.
- Semantic outline equivalent to visual canvas.
- Undo and redo over draft commands.
- Impact preview before semantic changes.
- Layout persistence separate from semantic revisions.

### Dogfood target

Map Project Builder's own project-creation and scenario-modeling workflows through every initial lens, then compare whether each view preserves the same claims.

### Exit evidence

Moving or styling a node never changes semantic model hashes. Editing a relation through one lens is immediately and correctly reflected in every other lens.

### Decision gate

Use measured model sizes and rendering profiles to decide whether SVG remains sufficient or a WebGL renderer should be added behind the abstraction.

## Horizon 4: interface and interaction design

### Goal

Bind behavior to observable interfaces without collapsing interface state into domain state.

### User-visible outcome

A designer can model graphical, command-line, API, event, MCP, device, document, and human-procedure interfaces, then play a scenario over the selected interface.

### Capabilities

- Interface classification and specialized editors.
- Graphical interface frames and reusable controls.
- Visible state and read-model binding.
- Intent binding and validation.
- API operations, request/response contracts, events, tools, resources, prompts, device signals, and human handoffs.
- Scenario-on-interface mapping.
- Interface states, transitions, loading, empty, denied, failed, partial, and degraded conditions.
- Accessibility and operability constraints.
- Prototype playback using model examples.

### Dogfood target

Design Project Builder's own project creation, actor editor, scenario editor, Problems panel, and guided path from their modeled interactions.

### Exit evidence

The POS item-scan happy path and core failure paths can be played from scanner input through application response and rendered transaction state.

### Decision gate

Determine which interface types need native renderers, which need structured tables, and which should remain diagram and contract projections.

## Horizon 5: systems, boundaries, and architecture

### Goal

Carry the behavioral model through organizational and technical boundaries into implementation-ready vertical slices.

### User-visible outcome

An architect can drill into an interaction, define inner actors and systems, mark boundaries, attach contracts and operational properties, and project the result into Presentation, Application, Domain, and Infrastructure responsibilities.

### Capabilities

- System, component, datastore, provider, queue, file, device, and human boundary modeling.
- Ownership, trust, transaction, process, deployment, residency, vendor, and failure-domain boundaries.
- Contract and version modeling.
- Latency, availability, consistency, security, privacy, retry, idempotency, and recovery properties.
- Vertical-slice projection.
- Architecture-decision prompts.
- Threat and operational overlays.
- Dependency and responsibility validation.
- C4-like context and container views without losing behavioral linkage.

### Dogfood target

Model Project Builder's web client, application boundary, model kernel, persistence, collaboration channel, projection worker, and external identity/object-storage adapters.

### Exit evidence

Every implementation-ready POS interaction traces from actor goal to interface, application use case, domain transition, infrastructure effect, contract, test claim, and operational expectation.

### Decision gate

Confirm whether any module has earned independent deployment and whether a plugin boundary is justified by actual third-party extension needs.

## Horizon 6: specification and evidence workbench

### Goal

Turn the canonical model into executable or reviewable development definitions.

### User-visible outcome

An engineer can select a vertical slice and receive behavioral specifications, state tables, contracts, property candidates, test manifests, traceability, and an implementation outline.

### Capabilities

- Behavioral specification projection.
- Example tables and test-data definitions.
- State-transition and decision tables.
- OpenAPI, AsyncAPI-like, MCP, CLI, device, and document contract projections where appropriate.
- Property and invariant test candidates.
- Contract-test manifests.
- Evidence capture and verification runs.
- Claim-to-test linkage.
- Coverage and contradiction reports.
- Baselines and approval packets.
- Source generation and analyzers for the Project Builder SDK.

### Dogfood target

Generate and execute the acceptance definitions for Project Builder's own modeling kernel.

### Exit evidence

At least one production vertical slice is built from a Project Builder definition, and the shipped implementation evidence is attached back to its claims without manual traceability spreadsheets.

### Decision gate

Evaluate model-to-code fidelity. Do not proceed to broad code generation until generated definitions remain understandable and stable across several materially different slices.

## Horizon 7: scaffolded and executable modeling

### Goal

Evolve from definition and projection into a modern visual programming environment while preserving explicit decisions and ordinary source-code ownership.

### User-visible outcome

A team can scaffold selected modules, contracts, tests, adapters, and interface structures from a reviewed baseline, then round-trip supported changes safely.

### Capabilities

- Typed scaffold plans.
- Generated domain types, command/result unions, contracts, tests, registration, and adapters.
- User-owned versus generated-file boundaries.
- Regeneration previews and semantic diffs.
- Protected extension points.
- Runtime scenario simulation.
- Configurable execution adapters.
- Debugger-style step, inspect, breakpoint, and invariant view.
- Deployment and operational model projections.
- Extension SDK and signed packages.

### Dogfood target

Generate selected Project Builder registries, validators, contracts, and tests from its own model. Handwritten core behavior remains deliberate until generated output proves superior.

### Exit evidence

Regeneration is deterministic, never overwrites user-owned code, and is proven through compile, behavior, compatibility, and migration evidence.

### Decision gate

Decide which portions can become executable source of truth and which must remain specification-only.

## Horizon 8: optional agentic assistance

### Goal

Add intelligence as a reviewable collaborator, never as an opaque substitute for definition.

### User-visible outcome

A user can request suggestions for gaps, paths, rules, boundaries, interface states, tests, or architectural alternatives and receive an uncommitted proposal with rationale and provenance.

### Capabilities

- Context-bounded model retrieval.
- Structured proposal change sets.
- Missing-case suggestions.
- Contradiction explanation.
- Specification drafting.
- Alternative comparison.
- Evidence summarization.
- Human approval and selective application.
- Evaluation suites for suggestion quality, grounding, privacy, and regression.
- Provider-neutral model gateway.

### Non-negotiable limits

- Agent output is never evidence by itself.
- Suggestions cannot silently become committed truth.
- The human workflow remains complete.
- Private project data is not sent to a provider without explicit policy and consent.
- Every accepted proposal records origin, provider/model metadata when applicable, reviewer, and edits.

## Release train

### Internal developer previews

Ship continuously after Horizon 0. These builds may change storage and model formats with explicit migrations.

### Design-partner alpha

Begins during Horizon 2 with selected teams. Focus on language, facilitation, and structured capture.

### Public preview

Begins after Horizon 4 exit criteria. Format compatibility and migration support become published promises.

### Version 1.0

Requires the implementation-ready vertical slice and specification/evidence workflow from Horizons 5 and 6, plus security, accessibility, backup, restore, and operational readiness.

### Post-1.0

Executable modeling, extension ecosystem, and agents evolve behind capability flags and compatibility contracts.

## Roadmap scorecard

Each horizon is evaluated on:

| Dimension | Question |
|---|---|
| Human usability | Can a motivated non-agent user complete the workflow? |
| Semantic integrity | Does one canonical truth survive every projection and edit path? |
| Definition quality | Are unknowns, assumptions, paths, state, rules, and evidence explicit? |
| Traceability | Can a user move from outcome to operation and proof? |
| Accessibility | Can the workflow be completed without pointer-only interaction? |
| Performance | Does the model remain responsive at representative scale? |
| Safety | Are authorization, tenant isolation, imports, content, and supply chain controlled? |
| Operability | Can the product be deployed, observed, backed up, and restored? |
| Dogfood depth | Has Project Builder modeled and validated the capability itself? |
