# ADRs and Governance

## Decision model

Architecture and product decisions are first-class Project Builder elements and checked-in ADR projections.

The canonical decision contains:

- identifier,
- title,
- status,
- context,
- decision drivers,
- options,
- selected option,
- rationale,
- evidence,
- consequences,
- risks,
- affected elements,
- owner,
- review or supersession trigger.

Markdown ADRs are generated or synchronized projections.

## Statuses

- Proposed.
- Investigating.
- Accepted.
- Rejected.
- Deferred.
- Superseded.
- Deprecated.
- Reversed.

## When an ADR is required

- new persistent storage model,
- new independently deployed component,
- public API or format decision,
- security or identity architecture,
- third-party foundational dependency,
- framework or UI architecture change,
- collaboration merge strategy,
- source-generation architecture,
- plugin execution model,
- data residency or tenancy topology,
- significant performance tradeoff,
- deviation from a repository non-negotiable.

Routine local implementation choices do not need ADRs.

## ADR workflow

1. Link the scenario or quality need.
2. State current constraint.
3. list viable options.
4. define decision criteria.
5. Run spike or gather evidence if needed.
6. Select and record consequences.
7. review by relevant authority.
8. accept.
9. update model, implementation plan, and docs.
10. define reassessment trigger.

## Initial ADR backlog

- ADR-0001: Modular monolith first.
- ADR-0002: Blazor Web App with Interactive WebAssembly studio.
- ADR-0003: PostgreSQL relational plus JSONB model storage.
- ADR-0004: Append-only change sets plus current state, not full event sourcing.
- ADR-0005: SVG-first canvas with renderer abstraction.
- ADR-0006: Optimistic concurrency before CRDT.
- ADR-0007: Deterministic human-completable guidance engine.
- ADR-0008: Open canonical JSON project format.
- ADR-0009: MTP and selected test framework.
- ADR-0010: Cookie authentication for same-origin browser client.
- ADR-0011: Out-of-process executable plugins.
- ADR-0012: Model-to-code reference mechanism.

## Governance layers

### Repository governance
Formatting, analyzers, dependencies, branch rules, CI, releases.

### Model governance
Element registry, relation semantics, validation rules, format versions, migrations.

### Product governance
Scope, release profiles, UX principles, dogfood gates.

### Workspace governance
Roles, templates, validation profiles, retention, integrations, classification.

### Evidence governance
Required proof by claim category, freshness, waiver authority.

## Rule changes

Changing a core validation rule requires:

- examples of valid and invalid models,
- profile severity impact,
- migration or newly introduced findings,
- user explanation,
- tests,
- dogfood run,
- release note if user-visible.

A rule must not be weakened because current implementation violates it without reviewing the underlying model.

## Ubiquitous-language changes

Renaming or redefining a canonical term requires:

- reason,
- impacted model types,
- user-facing aliases,
- serialized compatibility,
- migration,
- docs,
- projection changes,
- training updates.

## Package governance

A foundational package requires:

- active maintenance evidence,
- compatible license,
- security history,
- .NET 10 support,
- trimming or browser implications where relevant,
- transitive dependency review,
- exit strategy,
- ADR when architectural.

Prefer BCL and first-party framework capabilities before adopting broad packages.

## Exception records

An exception contains:

- violated policy or rule,
- scope,
- rationale,
- authority,
- risk,
- mitigation,
- expiration or removal trigger,
- evidence,
- owner.

Exceptions are reviewed. They do not become silent precedent.

## Documentation governance

Authoritative sources:

1. canonical Project Builder dogfood model for behavior and decisions as capability matures,
2. checked-in docs and ADR projections,
3. code and tests for implementation,
4. generated artifacts tied to revision,
5. issue tracker for work status, not enduring truth.

A conflicting lower source creates a gap.

## Review cadence

- architecture decision review as needed,
- meta-model review each milestone,
- security threat review each external release,
- accessibility review each major interaction release,
- dependency review monthly or automated,
- dogfood model review every release slice,
- format compatibility review before beta and version 1.

## Governance principle

Governance should make important decisions visible and reversible. It should not turn every edit into ceremony. The stricter the gate, the clearer the risk it controls and the evidence it requires.
