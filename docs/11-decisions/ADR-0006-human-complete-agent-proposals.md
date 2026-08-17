# ADR-0006: Keep Human Workflows Complete and Treat Agent Output as Proposals

## Status

Accepted.

## Context

Agent assistance can suggest actors, paths, invariants, interfaces, tests, and architecture. It can also invent facts, obscure provenance, expose sensitive models, or create a workflow that humans cannot reproduce.

Project Builder's purpose is to make decisions and gaps explicit, not to hide them behind automation.

## Decision

Every essential product workflow must be complete for a human actor.

Agent assistance, when added, uses a provider-neutral gateway and returns a structured, uncommitted change-set proposal with:

- source model revision,
- bounded context,
- provider/model/request provenance when applicable,
- citations or source references,
- confidence or uncertainty where available,
- proposed operations,
- diagnostics and limitations.

The proposal passes ordinary schema, semantic, security, and authorization checks. A human can edit, partially apply, or reject it. Accepted operations commit through the normal change-set path.

Agent statements are never executable evidence by themselves.

## Consequences

### Benefits

- no provider lock-in for core workflows,
- inspectable and auditable assistance,
- consistent undo/review/authorization,
- product remains useful offline from agent services,
- safer handling of uncertainty.

### Costs

- less magical one-click automation,
- proposal UI and provenance storage,
- evaluation and policy infrastructure,
- users remain responsible for accepted truth.

## Prohibited behavior

- silent commits,
- marking unknown as known without source,
- fabricating evidence,
- bypassing purpose-profile findings,
- sending restricted content without policy and consent,
- hidden agent-only endpoints for essential actions.

## Validation

- disable agent and run all primary workflows,
- policy denial tests,
- proposal schema/model validation,
- selective apply,
- provenance retention,
- provider-unavailable behavior,
- evaluation against missing-case and false-claim suites.

## Review triggers

- a deterministic automation can replace an agent task,
- organizations require local/private providers,
- regulation changes automated-decision obligations,
- proposal review cost exceeds delivered value.
