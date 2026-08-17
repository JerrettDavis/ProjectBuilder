# ADR-0001: Use One Canonical Typed Model with Multiple Projections

## Status

Accepted.

## Context

Project Builder must represent narrative behavior, state, interfaces, systems, architecture, specifications, tests, and evidence. Separate documents or editor-specific models would drift and make it impossible to answer whether two views refer to the same claim.

A universal untyped graph would avoid duplication but would weaken validation and generation.

## Decision

Use one versioned canonical semantic model composed of typed elements, typed relations, containment, claims, evidence, and explicit extension points.

Every story map, flow diagram, state table, interface design, system context, specification, implementation plan, and generated artifact is a projection of a model revision. Editors mutate canonical content through commands and change sets.

Project Builder canvas layout, filters, selection, and personal display preferences are separate view state.

## Consequences

### Benefits

- stable identity across views,
- semantic diff and impact analysis,
- purpose-specific validation,
- deterministic projections,
- model-to-evidence traceability,
- less duplicate authoring.

### Costs

- the meta-model requires disciplined versioning,
- projection diagnostics must report unsupported content,
- editors cannot store hidden semantic fields,
- extension governance is necessary.

## Rejected alternatives

### Independent document models

Rejected because synchronization would be manual and contradictory truth inevitable.

### Generic node-edge graph as the primary domain

Rejected because arbitrary payloads and labels cannot reliably support exhaustive relations, validation, generation, or accessible editors.

### Code as the only source of truth

Rejected because Project Builder begins before implementation and must include human, organizational, interface, and operational behavior.

## Validation

- property: moving a canvas node never changes semantic hash,
- edit in one lens appears in every relevant lens,
- export-import preserves identifiers and claims,
- projections identify source revision and coverage,
- unsupported extension content produces findings rather than silent loss.

## Review triggers

- a legitimate domain repeatedly requires unsupported semantics,
- projection performance cannot meet the reference envelope,
- extension payloads become the majority of normal content,
- round-trip editing of a projection is proposed.
