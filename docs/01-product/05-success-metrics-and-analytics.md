# Success Metrics and Product Analytics

## Measurement philosophy

The product should measure whether it improves understanding, delivery, and validation. It must not reward users for filling fields, creating diagrams, or maximizing element counts.

Analytics must be privacy-aware, transparent, configurable, and separable from the customer's model content. Self-hosted and enterprise deployments may disable product analytics entirely while retaining local operational telemetry.

## North-star outcome

**A team can move from an intended outcome to an implementation-ready, evidence-linked vertical slice with fewer material surprises and less rework than its prior process.**

This outcome cannot be represented by one usage metric. It is evaluated through a family of leading and lagging indicators.

## Product value metrics

### Time to first coherent slice
Elapsed active time from project creation to the first slice that passes its selected readiness profile.

Interpretation:
- Falling time with stable review quality suggests better guidance.
- Falling time with rising post-implementation gaps suggests premature completion.

### Pre-implementation gap discovery
Material gaps identified before implementation begins, categorized by actor, state, rule, path, boundary, quality, and evidence.

The goal is not to maximize gaps. It is to move discovery earlier.

### Clarification churn
Number and severity of definition changes after an implementation session begins.

Separate healthy learning from avoidable ambiguity.

### Traceability coverage
Material claims with linked implementation references and current evidence.

Report by category, not only aggregate.

### Evidence freshness
Time and revision distance between a model change and revalidation of impacted evidence.

### Scenario completion
Percentage of observed real workflows that can be represented without escaping to unstructured notes or external diagrams.

### Dogfood coverage
Shipped Project Builder interactions represented at equivalent depth in its own model.

## User-experience metrics

- Tutorial completion by persona.
- Time spent resolving first blocking gap.
- Frequency of Unknown, Assumed, Deferred, and Not Applicable selections.
- Reopened guidance position accuracy.
- Keyboard-only task completion.
- Canvas task success at representative graph sizes.
- Search success without manual tree traversal.
- Rate of accidental semantic changes caused by visual actions, target zero.
- Undo and conflict recovery success.
- User-reported understanding before and after model review.

## Delivery metrics

- Lead time from approved baseline to passing evidence.
- PRs linked to a model slice.
- Review comments caused by missing context.
- Defects traced to absent or incorrect model claims.
- Generated artifact adoption.
- Percentage of generated files edited manually, which signals projection design problems.
- Model-to-code divergence findings.
- Migration failures and rollback success.

## Reliability and performance metrics

- Command success and failure rate by type.
- p50, p95, and p99 command latency.
- query latency by project size.
- SignalR connection stability.
- conflict rate and data-loss incidents.
- import/export duration and failures.
- projection queue age and retry count.
- database lock and contention metrics.
- client frame time and interaction latency.
- memory consumption by visible and total graph size.
- backup and restore rehearsal results.

## Security and governance metrics

- denied authorization attempts.
- privilege changes.
- stale credentials and integrations.
- exports of sensitive projects.
- unresolved high-severity security findings.
- approval bypass attempts.
- agent proposals accepted, modified, or rejected.
- model content included in external agent calls by policy category.
- audit integrity verification.

## Anti-metrics

Do not optimize:

- raw number of nodes,
- number of diagrams,
- number of wizard questions answered,
- percentage completion without purpose context,
- number of generated tests,
- time in application,
- agent suggestions accepted,
- model size.

These can be diagnostic counts, but they are not product success.

## Analytics event design

Events describe product interaction without copying model text by default.

Example:

```json
{
  "eventName": "guidance.finding.resolved",
  "schemaVersion": 1,
  "occurredAt": "2026-08-15T12:00:00Z",
  "workspaceTier": "self-hosted",
  "projectSizeBand": "100-999",
  "findingCategory": "failure-path",
  "resolutionKind": "deferred",
  "lens": "scenario-flow",
  "durationMs": 84231
}
```

Identifiers should be pseudonymous, rotated where appropriate, and excluded from product analytics when not necessary.

## Research program

Each major release should include qualitative research:

1. Observe at least one non-technical discovery session.
2. Observe one designer mapping a scenario to an interface.
3. Observe one architect tracing a cross-system path.
4. Observe one engineer consuming a generated work packet.
5. Observe one validator challenging evidence.
6. Review dogfood friction.

Recorded findings become model gaps and roadmap input.
