# Module and Dependency Architecture

## Dependency rule

Dependencies point inward toward semantic truth:

```text
Presentation ─┐
              ├──> Application ───> Domain
Infrastructure┘
```

Contracts and platform-neutral runtime abstractions sit at deliberate boundaries. They must not become dumping grounds.

## Initial modules

### Identity and Workspaces

Owns:

- user profile,
- workspace,
- membership,
- role assignment,
- policy,
- invitation,
- external identity mapping.

Does not own provider authentication protocol implementation, which is infrastructure.

### Projects and Revisions

Owns:

- project lifecycle,
- scope and purpose,
- project revision,
- baselines,
- imports and exports,
- project deletion and restore,
- project-level authorization context.

### Modeling

Owns:

- canonical elements and relations,
- containment,
- change-set operations,
- semantic model validation,
- element lifecycle,
- model query abstractions.

### Guidance and Validation

Owns:

- purpose profiles,
- deterministic prompt rules,
- findings,
- gap creation and disposition,
- readiness evaluation,
- explanations and repair actions.

It consumes Modeling contracts but does not change model state except through Modeling commands.

### Views and Canvases

Owns:

- lens definitions,
- view definitions,
- layout,
- selection mappings,
- canvas command descriptors,
- personal and shared views.

It does not own semantic elements.

### Interfaces

Owns:

- target interface definitions,
- interface state mapping,
- controls and intent bindings,
- component definitions,
- graphical and non-graphical interface semantics.

### Evidence and Traceability

Owns:

- claims,
- evidence requirements,
- evidence metadata,
- freshness,
- traceability graph,
- implementation references,
- waivers.

### Projections

Owns:

- projection requests,
- generator registry,
- deterministic artifact metadata,
- generated output provenance,
- projection lifecycle.

### Collaboration and Review

Owns:

- comments,
- review requests,
- approvals,
- discussions,
- presence policy,
- conflict presentation metadata.

Project revision commits still go through Projects and Modeling.

### Administration and Integrations

Owns:

- workspace templates,
- validation profiles,
- extension registration,
- integration connections,
- retention jobs,
- audit queries.

Concrete provider clients belong to Infrastructure.

## Module communication

Use direct in-process contracts for commands and queries when the caller requires a result.

Use internal domain or application events when:

- the originating transaction can commit independently,
- multiple modules need notification,
- ordering and failure semantics are explicit,
- eventual work is acceptable.

Do not publish an event merely to avoid a method call.

## Domain event example

```csharp
public sealed record ProjectRevisionCommitted(
    ProjectId ProjectId,
    Revision Revision,
    ChangeSetId ChangeSetId,
    ImmutableArray<ElementId> ChangedElements,
    ImmutableArray<ClaimId> PotentiallyImpactedClaims);
```

Consumers:

- Guidance recalculates findings.
- Evidence marks impacted proof potentially stale.
- Projections invalidates cached outputs.
- Collaboration notifies connected clients.
- Integrations schedules external synchronization.

The event is persisted to an outbox with the project transaction.

## Internal contracts

A module exposes:

- application commands,
- queries,
- event contracts,
- stable read models,
- ports required from infrastructure.

It does not expose:

- DbContext,
- EF entities,
- internal aggregate mutable collections,
- provider DTOs,
- arbitrary service locator,
- generic repository for all entities.

## Functional core and imperative shell

The canonical model and rules form a functional core where practical:

```csharp
public static ApplyChangeSetResult Apply(
    ProjectModel current,
    ChangeSet changeSet,
    ModelRegistry registry)
{
    // Validate expected revision.
    // Fold typed operations.
    // Evaluate invariants.
    // Return new immutable state, events, findings, and impact.
}
```

The application shell:

- loads state,
- obtains time and actor context,
- calls the pure transition,
- persists,
- dispatches effects,
- records telemetry.

This division makes change-set behavior replayable and property-testable without requiring full event sourcing.

## Aggregate boundaries

Candidate aggregates:

### Project Definition
Purpose, status, current revision, policies, baseline references.

### Model Scope
A bounded group of elements and relations committed atomically for an operation. The exact aggregate boundary is validated through load and contention experiments.

### Change Set
Immutable committed operation group and metadata.

### View Definition
Layout and lens state, separate from semantic model.

### Review
Review request, participants, dispositions, and scope.

### Evidence Record
Evidence metadata, claim coverage, and freshness state.

Avoid a single in-memory aggregate containing an entire enterprise project for every edit. Also avoid independently mutable nodes that can violate cross-node invariants. The command handler should load the minimum consistent scope and invoke project-level rules where needed.

## Registries

The meta-model registry defines element and relationship behavior:

```csharp
public sealed record ElementDescriptor(
    ElementKind Kind,
    ImmutableArray<ElementKind> AllowedParents,
    ImmutableArray<FieldDescriptor> Fields,
    ImmutableArray<ValidationRuleId> Rules,
    InspectorDescriptor Inspector,
    ImmutableArray<GuidanceRuleId> GuidanceRules);
```

Core descriptors are code-defined and compile-time checked. Extensions add namespaced descriptors through a constrained package or data contract.

## Dependency testing

Architecture tests verify:

- Domain references only approved assemblies.
- Application does not reference Web, EF Core, or provider packages.
- Web.Client does not reference server-only Infrastructure.
- Infrastructure implementations satisfy Application ports.
- modules do not access another module's EF entities or schema directly.
- public surface remains within approved contracts.
- generator projects have no runtime dependency leakage.

## Cross-module transactions

A use case that modifies multiple modules:

1. Has one owning application handler.
2. Calls domain transitions explicitly.
3. Persists through one database transaction where modules share the monolith and atomicity is required.
4. Emits outbox events after commit.
5. Does not fake service independence inside the monolith.

If modules later become services, the model must add transaction boundaries, partial completion, and recovery behavior.

## Extraction criteria

A module can become a service when evidence shows:

- independent scaling is material,
- release cadence or ownership requires isolation,
- security boundary requires process separation,
- failure isolation provides measurable value,
- data ownership can be independent,
- consistency can be relaxed or coordinated explicitly,
- operational cost is accepted.

Extraction is a model and product decision, not a refactoring fashion.
