# Definition of Ready and Definition of Done

## Purpose

Ready and Done are evidence gates, not administrative checklists. A work item is ready when the team understands enough to build a bounded slice safely. It is done when the intended behavior is observable, its claims are proven to the agreed standard, and the product model, implementation, and operations agree.

Unknowns are allowed. Hidden unknowns are not.

## Definition of Ready for discovery

A discovery activity is ready when:

- the scope or question is named,
- affected stakeholders and decision owner are identified,
- current sources and known constraints are available or explicitly missing,
- the intended artifact or decision is stated,
- a time box and parking-lot mechanism exist,
- sensitive information handling is understood.

Discovery is done when:

- findings are recorded as decisions, assumptions, unknowns, disputes, constraints, or evidence,
- actors, outcomes, and material scenarios have owners,
- contradictions and missing sources are visible,
- next modeling or research actions are bounded.

## Definition of Ready for a product slice

A slice is ready when the following are represented in the Project Builder model or an approved temporary equivalent:

### Outcome and scope

- actor or system beneficiary,
- intended observable outcome,
- included and excluded scope,
- parent capability and episode,
- reason the slice is valuable now.

### Behavior

- starting facts,
- trigger,
- happy path,
- material alternate and failure paths,
- recovery, cancellation, or compensation where relevant,
- semantic results,
- externally observable response.

### State and rules

- state read and changed,
- preconditions,
- transition,
- invariants,
- rules and policy authority,
- unknown and assumed values.

### Interfaces and boundaries

- initiating interface,
- affected interface states,
- system and organizational boundaries,
- external contracts,
- authorization and privacy expectations,
- operational properties that materially constrain behavior.

### Evidence

- acceptance examples,
- property or invariant proof plan,
- integration or contract evidence,
- manual or end-to-end evidence where needed,
- traceability identifier.

### Delivery

- dependencies,
- migration impact,
- rollout and rollback expectation,
- named owner,
- review participants,
- session-sized or explicitly decomposed.

A slice is not blocked solely because every future detail is unknown. It is blocked when an unknown can change the correctness, safety, data compatibility, or boundedness of the proposed work and has no explicit spike or decision path.

## Definition of Ready for implementation

In addition to product readiness:

- application command/query boundary is named,
- domain result vocabulary is agreed,
- provider interactions are expressed as ports or owned adapters,
- persistence transaction intent is known,
- concurrency and idempotency are considered,
- API or interface contract is drafted,
- authorization is server-enforceable,
- telemetry and audit needs are defined,
- test fixtures and environments are available,
- no unresolved ADR is silently embedded in the task.

## Definition of Ready for an agent-dispatched session

- exact repository and base branch are known,
- permitted scope and prohibited areas are listed,
- required context files are listed,
- expected files or modules are named without overprescribing implementation,
- validation commands are executable,
- completion and stop conditions are explicit,
- no credential or destructive action is required,
- UI automation constraints are stated,
- expected handoff format is included.

## Definition of Done for a code slice

### Model agreement

- canonical model changed where product truth changed,
- element and relation identities are stable,
- decisions and assumptions are recorded,
- purpose-profile findings are resolved, deferred, or accepted with authority,
- generated artifacts identify source revision.

### Behavior

- intended happy and failure paths are observable,
- semantic results are explicit,
- no invalid intermediate state is externally committed,
- cancellation, timeout, retry, duplicate, stale, and partial outcomes are handled where applicable,
- UI includes loading, empty, invalid, denied, failed, and degraded states where relevant.

### Architecture

- dependency direction passes,
- domain remains provider and UI independent,
- transaction boundary matches the modeled invariant,
- external effects are isolated and observable,
- public contracts are versioned,
- no needless distributed boundary was introduced.

### Data

- migration is reviewed and tested,
- old supported data can be read or migrated,
- rollback or forward-fix strategy is documented,
- import/export behavior remains compatible or the format version changes,
- concurrency and idempotency are proven,
- data retention and deletion impact is known.

### Security and privacy

- authentication and authorization tests pass,
- tenant isolation is proven,
- input and import limits exist,
- secrets are absent from source and artifacts,
- logging avoids prohibited content,
- threat-model deltas are reviewed,
- dependency and static scans pass or findings are accepted.

### Accessibility and experience

- keyboard path exists,
- focus behavior is tested,
- no pointer-only action is required,
- accessible names and semantics exist,
- responsive and zoom behavior is checked,
- guidance and error messages explain corrective action,
- no unexpected focus or window stealing occurs.

### Evidence

- focused unit, property, integration, contract, component, and end-to-end tests pass as applicable,
- evidence maps to modeled claims,
- failed evidence is not hidden,
- snapshots are reviewed semantically,
- coverage changes are explained rather than optimized as a number,
- manual scenario evidence is attached when automation is not credible.

### Operations

- logs, metrics, traces, and audit events are useful and safe,
- health behavior is correct,
- performance is measured for changed hot paths,
- failure and recovery behavior is exercised,
- configuration and feature-flag defaults are documented,
- deployment and rollback are viable,
- runbook changes are included.

### Repository quality

- build and full evidence commands pass,
- generated files are deterministic,
- no local paths or machine state are required,
- documentation and training are updated,
- PR explains model, behavior, evidence, decisions, and risks,
- branch is rebased or restacked according to repository policy.

## Definition of Done for a release

A release is done when:

- release baseline pins model revision, rule-set versions, projection versions, code commit, database migration, and evidence,
- all release-blocking findings are resolved or explicitly accepted by authorized owners,
- security, accessibility, performance, resilience, backup, and restore evidence meet the release profile,
- upgrade and rollback or forward-fix paths are rehearsed,
- release notes explain behavior and compatibility changes,
- support and incident ownership are active,
- deployment is observed through the defined stabilization period,
- Project Builder's dogfood model describes the released behavior,
- the release packet is retained and independently inspectable.

## Definition of Done for documentation

- intended audience and decision purpose are clear,
- terminology matches the ubiquitous language,
- examples are non-trivial and traceable,
- links resolve,
- decisions are distinguished from options and assumptions,
- procedures have prerequisites, commands or actions, expected results, and recovery,
- current-version claims cite an authoritative source,
- no document duplicates a canonical table without naming the synchronization mechanism.

## Exceptions

An exception must record:

- unmet criterion,
- why it cannot reasonably be met now,
- affected claims and users,
- risk level,
- compensating control,
- owner,
- expiration or review date,
- authority accepting the risk.

Exceptions never redefine Done retroactively. They produce an accepted-risk state attached to the release or slice.
