# Model Review Checklist

## Review metadata

| Field | Value |
|---|---|
| Project | |
| Revision/baseline | |
| Purpose profile | |
| Scope | |
| Reviewers | |
| Date | |

The review should produce findings with severity, owner, and disposition. A checklist mark alone is not evidence of correctness.

## Purpose and outcomes

- [ ] purpose is understandable without implementation jargon.
- [ ] outcomes identify beneficiaries and observable success.
- [ ] outputs are not mislabeled as outcomes.
- [ ] included and excluded scope are explicit.
- [ ] competing outcomes and harms are visible.
- [ ] success measures have sources or knowledge status.

## Actors and authority

- [ ] actors are contextual roles rather than incidental names.
- [ ] human, organization, system, device, automated, and provider roles are distinguished.
- [ ] initiator, receiver, beneficiary, approver, supporter, and affected parties are represented.
- [ ] authority is explicit for mutating or sensitive intents.
- [ ] delegation and override are scoped.
- [ ] personas do not substitute for authority.

## Narrative behavior

- [ ] episodes are outcome-bearing and bounded.
- [ ] scenarios have starting facts, trigger, path classification, and expected result.
- [ ] scenes mark meaningful context, responsibility, interface, or boundary changes.
- [ ] interactions identify intent and observation.
- [ ] ordered steps do not smuggle in unmodeled state.
- [ ] reuse is modeled by relation rather than duplicated truth.

## State and logic

- [ ] domain, application-workflow, presentation, infrastructure, and externally observed state are separated.
- [ ] facts and sources are explicit.
- [ ] commands, events, and effects are not conflated.
- [ ] transitions state source, trigger, conditions, result, target, and effects.
- [ ] rules identify kind, scope, authority, and version.
- [ ] invariants are falsifiable and attached to the smallest owning scope.
- [ ] calculations define units, currency, precision, rounding, and policy order as applicable.
- [ ] temporal and concurrency behavior is represented when material.

## Paths and semantic results

- [ ] happy path is concrete.
- [ ] invalid, denied, unavailable, timeout, conflict, duplicate, cancellation, and recovery are considered.
- [ ] partial success and compensation are considered for external effects.
- [ ] unreachable and unhandled branches are absent or findings exist.
- [ ] semantic results are narrower than generic success/failure.
- [ ] every non-success path states whether domain state changes.
- [ ] late and out-of-order observations are considered where applicable.

## Interfaces

- [ ] interface kind is explicit.
- [ ] controls, operations, tools, signals, or steps bind to intents.
- [ ] visible/exposed state has an authoritative source.
- [ ] every material semantic result has a representation.
- [ ] loading, empty, invalid, denied, failed, degraded, stale, conflict, and recovery states are considered.
- [ ] focus and keyboard behavior are defined.
- [ ] no pointer-only or drag-only essential behavior exists.
- [ ] interface does not imply success before durable acceptance.
- [ ] contract versioning and limits are represented.

## Boundaries and architecture

- [ ] ownership, trust, transaction, process, deployment, protocol, vendor, residency, failure-domain, and human handoff boundaries are considered.
- [ ] crossings identify contracts and owners.
- [ ] latency, availability, consistency, ordering, throughput, idempotency, retry, recovery, retention, and cost are specified only when material.
- [ ] values have sources or Assumed status.
- [ ] Domain, Application, Infrastructure, and Presentation responsibilities follow behavior.
- [ ] external provider data is translated into owned concepts.
- [ ] no distributed boundary is justified only by fashion.
- [ ] architectural decisions record options and evidence.

## Data and compatibility

- [ ] identity and lifecycle are defined.
- [ ] ownership and deletion behavior are explicit.
- [ ] format/API/event compatibility is considered.
- [ ] migration and old-data behavior are considered.
- [ ] concurrency and idempotency are defined.
- [ ] sensitive data classification, retention, export, and deletion are represented.
- [ ] import limits and unsafe content are considered.

## Security and privacy

- [ ] authentication and authorization are server-enforceable.
- [ ] tenant/workspace isolation is represented.
- [ ] trust boundaries trigger threat review.
- [ ] input validation, rate/size limits, and active content are considered.
- [ ] secrets and prohibited data are excluded from logs.
- [ ] audit is proportionate and access-controlled.
- [ ] agent/provider data routing is policy-controlled.
- [ ] support and break-glass access are modeled.

## Evidence and traceability

- [ ] every implementation-ready claim has planned proof.
- [ ] evidence type fits the claim.
- [ ] model revision and code/environment can be correlated.
- [ ] stale evidence rules are defined.
- [ ] outcome traces through scenario, interaction, state/rule, implementation, and evidence.
- [ ] generated artifacts identify source revision and projection version.
- [ ] failed evidence and accepted risks remain visible.
- [ ] an agent statement is not treated as evidence.

## Knowledge and governance

- [ ] decisions, invariants, assumptions, options, unknowns, disputes, and deferrals are distinguishable.
- [ ] Not Applicable has rationale.
- [ ] waivers and accepted risk have authority and review date.
- [ ] sources have authority and effective period.
- [ ] terminology matches the ubiquitous language.
- [ ] current/future states are not distinguished only by layout or color.
- [ ] Project Builder dogfood coverage is updated where applicable.

## Review disposition

- Approved.
- Approved with non-blocking findings.
- Changes requested.
- Rejected for current purpose.
- Accepted risk.

### Blocking findings

...

### Non-blocking findings

...

### Decisions required

...

### Evidence required

...

### Next review trigger

...
