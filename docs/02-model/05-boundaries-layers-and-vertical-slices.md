# Boundaries, Layers, and Vertical Slices

## The classification model

Project Builder uses four primary implementation perspectives:

- **Domain**: the facts, language, rules, invariants, and transitions that model the relevant reality.
- **Application**: use-case-specific coordination of domain behavior, authority, workflow, and effects.
- **Infrastructure**: concrete external mechanisms, providers, protocols, stores, devices, and adapters.
- **Presentation**: interfaces through which people or systems express intents and receive observations.

These are semantic responsibilities, not folders inferred from technical types.

## Refining "reality" and "externality"

An external provider can be authoritative for a domain fact.

Example:

- The Corporate Price Book is external to the POS application.
- The item's effective store price is a domain fact used by the sale.
- The HTTP client, credentials, retry mechanism, endpoint, and payload mapping are infrastructure.
- The application coordinates price resolution and transaction behavior.
- The POS screen presents the result.

External location does not make meaning infrastructure. Concrete acquisition does.

## Domain

Domain content includes:

- concepts and refined values,
- entities and aggregates where identity and consistency require them,
- facts,
- rules and calculations,
- invariants,
- state transitions,
- domain events,
- semantic results,
- domain services when a rule has no natural entity owner.

Domain code must not depend on:

- EF Core,
- ASP.NET Core,
- Blazor,
- HTTP,
- database providers,
- message brokers,
- file systems,
- clocks or random generators without explicit abstractions,
- logging frameworks.

A domain model can accept explicit facts such as current time, exchange rate, or authorization decision. It should not fetch them.

## Application

Application content includes:

- use-case handlers,
- authorization and policy coordination,
- workflow state,
- idempotency,
- transaction boundaries,
- orchestration of domain transitions and external effects,
- ports,
- mapping semantic results to application contracts,
- outbox coordination,
- evidence and audit hooks.

Application logic should remain explicit. A mediator library is optional, not the architecture. A feature can call a handler directly through a stable application port.

## Infrastructure

Infrastructure content includes:

- EF Core mappings and repositories,
- PostgreSQL,
- file and object storage,
- HTTP and message clients,
- identity provider adapters,
- payment and price-book adapters,
- clocks and identifier providers,
- telemetry exporters,
- email, printing, scanning, and device integration,
- source-control and CI integrations.

Infrastructure maps provider semantics into application or domain semantics. Provider DTOs do not leak inward.

## Presentation

Presentation includes:

- Blazor components,
- HTTP endpoints,
- CLI commands,
- MCP tools,
- message consumers,
- webhooks,
- device adapters at the interaction boundary,
- human procedure documents where the application exposes a process.

Presentation responsibilities:

- parse and validate transport shape,
- establish actor and authority context,
- map inputs to intents or application commands,
- invoke the application,
- map semantic results to observations,
- maintain presentation state,
- satisfy accessibility and protocol requirements.

Presentation must not own business invariants.

## Boundary types

### Ownership boundary
Responsibility moves between teams, organizations, or vendors.

### Trust boundary
Data or commands move between security principals or trust levels.

### Transaction boundary
Atomic consistency ends.

### Process boundary
Execution moves to another process or runtime.

### Deployment boundary
Independent release, scaling, or operation becomes possible.

### Protocol boundary
Representation or communication semantics change.

### Data residency boundary
Jurisdiction, tenancy, or storage policy changes.

### Human responsibility boundary
Work moves between roles or requires manual intervention.

A single interaction can cross several boundaries. Each boundary adds questions.

## Vertical slice

A vertical slice is a projection of one cohesive behavior.

Example slice: `Add scanned product to an open transaction`.

### Presentation
- Scanner input adapter.
- Manual fallback input.
- Visible pending, success, not-found, and unavailable states.
- Operator authorization.
- Accessibility and keyboard behavior.

### Application
- Command identity and correlation.
- Transaction lookup.
- captured-value classification orchestration.
- Store price resolution.
- Domain transition invocation.
- persistence and outbox.
- result mapping.

### Domain
- Captured value classification vocabulary.
- Product code.
- sellability facts.
- transaction line and totals.
- Add line rule.
- transaction invariants.
- semantic outcomes.

### Infrastructure
- Price Book adapter.
- transaction persistence.
- audit storage.
- telemetry.
- scanner browser or device bridge.

### Evidence
- scenario examples,
- classification properties,
- transaction invariant properties,
- price-book contract tests,
- integration test for add and persistence,
- end-to-end scan flow,
- timeout and duplicate-request tests,
- accessibility test.

## Slice readiness

A slice is implementation-ready when:

- initiating and receiving actors are known,
- interface and authority are defined,
- start and expected final state are explicit,
- rules and invariants are owned,
- material paths are closed,
- boundary contracts are at least draft,
- quality requirements are stated,
- fixed and open decisions are separated,
- evidence plan is approved,
- no blocker gaps remain.

## Architecture emergence

Project Builder should ask architecture questions when the model reveals a need:

- A transaction crosses a boundary: how is consistency handled?
- A dependency can be unavailable: what degradation or recovery is allowed?
- Multiple actors edit shared state: what concurrency semantics apply?
- Data is sensitive: what trust and retention controls apply?
- A behavior has high frequency: what latency and throughput are required?
- Rules vary by context: what strategy or policy mechanism owns selection?
- A long-running path persists: what workflow state and replay behavior are needed?

The answer can become a Decision, Port, Component, Contract, or DeploymentUnit.

## Anti-corruption

When external models differ from domain language, an adapter translates:

```text
Provider payload:
  itemNbr
  retail
  statusCode = "A"

Domain facts:
  ProductCode
  StorePrice
  Sellability = Sellable
```

Project Builder should preserve mapping decisions and contract tests. It should not copy provider vocabulary into the domain because translation feels inconvenient.

## Cross-cutting concerns

Security, telemetry, validation, caching, transactions, and retries are not generic layers that absorb domain meaning. They are policies and mechanisms applied at explicit boundaries.

Use decorators or pipelines when the behavior is truly orthogonal and ordered. Keep the core use-case flow readable.

## Modular monolith mapping

A bounded context can become a module with internal Domain, Application, Infrastructure, and Presentation areas. The host composes modules. Modules communicate through explicit contracts or in-process events, not direct access to internal persistence.

Project Builder itself should begin with bounded modules such as:

- Identity and Workspaces.
- Projects and Revisions.
- Modeling.
- Guidance and Validation.
- Views and Canvases.
- Interfaces.
- Projections.
- Collaboration.
- Administration.
