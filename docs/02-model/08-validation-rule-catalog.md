# Validation Rule Catalog

## Rule structure

Each rule has:

- stable code,
- title,
- category,
- severity by purpose profile,
- applicable element kinds,
- predicate,
- explanation,
- suggested resolutions,
- suppressibility,
- impact,
- version.

Example code: `PB-MODEL-SCENARIO-001`.

## Categories

1. Structural.
2. Referential.
3. Semantic.
4. Narrative.
5. State.
6. Path.
7. Interface.
8. Boundary.
9. Quality.
10. Traceability.
11. Evidence.
12. Governance.
13. Accessibility.
14. Security.
15. Operational.
16. Compatibility.

## Structural rules

| Code | Finding |
|---|---|
| PB-STRUCT-001 | Element identifier is duplicated. |
| PB-STRUCT-002 | Containment parent does not exist. |
| PB-STRUCT-003 | Containment cycle exists. |
| PB-STRUCT-004 | Element kind is not permitted under parent kind. |
| PB-STRUCT-005 | Ordered children contain duplicate or invalid order values. |
| PB-STRUCT-006 | Required typed payload is missing. |
| PB-STRUCT-007 | Extension schema or version is unavailable. |

## Referential rules

| Code | Finding |
|---|---|
| PB-REF-001 | Relation source or target is missing. |
| PB-REF-002 | Relation type does not permit the source and target kinds. |
| PB-REF-003 | Cardinality constraint is violated. |
| PB-REF-004 | Relation references a deprecated element without explicit compatibility. |
| PB-REF-005 | Cross-context reference lacks an explicit context relationship. |

## Narrative rules

| Code | Finding |
|---|---|
| PB-NARR-001 | Episode has no beneficiary. |
| PB-NARR-002 | Episode has no observable completion criterion. |
| PB-NARR-003 | Scenario has no trigger. |
| PB-NARR-004 | Scenario has no starting facts. |
| PB-NARR-005 | Scenario terminal outcome does not contribute to episode outcome. |
| PB-NARR-006 | Interaction has no initiator. |
| PB-NARR-007 | Interaction has no receiver or interface. |
| PB-NARR-008 | Intent is phrased only as an implementation mechanism. |
| PB-NARR-009 | Observation is not visible to any participant. |
| PB-NARR-010 | Child behavior does not explain how parent abstraction is satisfied. |

## State rules

| Code | Finding |
|---|---|
| PB-STATE-001 | Transition changes an undefined fact. |
| PB-STATE-002 | Transition has no source or target predicate. |
| PB-STATE-003 | Invariant has no owning scope. |
| PB-STATE-004 | Derived fact has multiple incompatible authorities. |
| PB-STATE-005 | Presentation state is used as domain truth without explicit mapping. |
| PB-STATE-006 | Rule depends on implicit external state. |
| PB-STATE-007 | Temporal condition has no time authority. |
| PB-STATE-008 | Command is modeled as a fact or event. |
| PB-STATE-009 | Event name is not past tense or does not describe an occurrence. |
| PB-STATE-010 | Semantic result is represented only as a generic error. |

## Path rules

| Code | Finding |
|---|---|
| PB-PATH-001 | Path does not terminate in a result, transition, recovery, or gap. |
| PB-PATH-002 | External effect has no failure path. |
| PB-PATH-003 | Retry exists without idempotency analysis. |
| PB-PATH-004 | Compensation is described as rollback across a non-transactional boundary. |
| PB-PATH-005 | Partial completion has no recovery owner. |
| PB-PATH-006 | Cancellation leaves state unspecified. |
| PB-PATH-007 | Degraded path lacks exit or reconciliation condition. |
| PB-PATH-008 | Failure is not represented at the initiating interface. |

## Interface rules

| Code | Finding |
|---|---|
| PB-UI-001 | Control mutates domain state without a modeled intent. |
| PB-UI-002 | Visible value has no source state or read model. |
| PB-UI-003 | Modeled semantic result has no observation. |
| PB-UI-004 | Loading or pending state is missing for asynchronous behavior. |
| PB-UI-005 | Destructive action has no confirmation or recovery analysis. |
| PB-UI-006 | Interface contract omits error semantics. |
| PB-UI-007 | CLI or API command has ambiguous input ownership. |
| PB-UI-008 | MCP tool lacks authorization or side-effect classification. |

## Boundary rules

| Code | Finding |
|---|---|
| PB-BOUND-001 | Boundary crossing has no interface. |
| PB-BOUND-002 | External interface has no contract. |
| PB-BOUND-003 | Trust boundary has no authorization or data classification. |
| PB-BOUND-004 | Transaction boundary crossing assumes atomic rollback. |
| PB-BOUND-005 | Provider-specific vocabulary leaks into domain without mapping. |
| PB-BOUND-006 | Availability dependency lacks degraded or failure behavior. |
| PB-BOUND-007 | Data residency boundary lacks storage and transfer policy. |
| PB-BOUND-008 | Human handoff lacks responsibility and acknowledgment. |

## Traceability and evidence rules

| Code | Finding |
|---|---|
| PB-TRACE-001 | Material element contributes to no outcome. |
| PB-TRACE-002 | Implementation reference is not tied to a definition baseline. |
| PB-EVID-001 | Material claim has no evidence requirement. |
| PB-EVID-002 | Evidence is linked to no claim. |
| PB-EVID-003 | Evidence is stale after impacted definition change. |
| PB-EVID-004 | Passing evidence does not cover the claimed scope. |
| PB-EVID-005 | Gap is resolved without a resolving change set. |
| PB-EVID-006 | Waiver is expired or lacks authority. |
| PB-EVID-007 | Generated artifact does not identify source revision. |
| PB-EVID-008 | Test count is supplied without claim mapping. |

## Accessibility rules

| Code | Finding |
|---|---|
| PB-A11Y-001 | Core canvas action has no keyboard equivalent. |
| PB-A11Y-002 | Drag is the only method to perform an operation. |
| PB-A11Y-003 | Focus order is undefined for a designed interface. |
| PB-A11Y-004 | Status change lacks programmatic announcement. |
| PB-A11Y-005 | Color is the only carrier of semantic status. |
| PB-A11Y-006 | Target size or spacing constraint is unresolved. |
| PB-A11Y-007 | Interface state lacks accessible name or role. |
| PB-A11Y-008 | Background update steals or resets focus. |

## Security rules

| Code | Finding |
|---|---|
| PB-SEC-001 | Sensitive data has no classification. |
| PB-SEC-002 | Privileged intent lacks authorization owner. |
| PB-SEC-003 | Audit-required transition has no evidence plan. |
| PB-SEC-004 | External content can render active code without isolation. |
| PB-SEC-005 | Agent operation lacks least-privilege scope. |
| PB-SEC-006 | Export omits sensitivity warning or policy. |
| PB-SEC-007 | Replay-sensitive command lacks operation identity. |
| PB-SEC-008 | Error observation exposes protected detail. |

## Operational rules

| Code | Finding |
|---|---|
| PB-OPS-001 | Long-running behavior lacks timeout and cancellation. |
| PB-OPS-002 | Background work lacks idempotency and retry policy. |
| PB-OPS-003 | Critical dependency lacks health or observability requirement. |
| PB-OPS-004 | Recovery path lacks rehearsal evidence. |
| PB-OPS-005 | Migration lacks rollback or forward-recovery plan. |
| PB-OPS-006 | Data retention and deletion are undefined. |
| PB-OPS-007 | Manual intervention lacks queue, owner, and escalation. |
| PB-OPS-008 | Correlation identity is lost across a boundary. |

## Severity profiles

A rule can vary by purpose:

| Rule | Discovery | Interface design | Implementation ready | Release ready |
|---|---|---|---|---|
| Missing recovery evidence | info | warning | warning | blocker |
| Missing interface error state | info | error | error | blocker |
| Missing actor authority | warning | warning | error | blocker |
| Stale implementation evidence | info | info | warning | blocker |
| No keyboard alternative | info | error | error | blocker |

## Suppression

A rule can be suppressed only when:

- the rule permits suppression,
- rationale is recorded,
- scope is explicit,
- authority is sufficient,
- expiration or review condition is set where relevant.

Suppressions are model elements or governed records. They are never hidden configuration comments.
