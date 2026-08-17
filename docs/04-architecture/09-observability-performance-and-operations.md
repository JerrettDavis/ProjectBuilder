# Observability, Performance, and Operations

## Observability objective

The system should explain:

- what a user attempted,
- which project and revision were involved without leaking sensitive content,
- where time was spent,
- which model rule rejected the action,
- whether persistence committed,
- whether background work completed,
- which dependencies contributed to failure,
- how customer-visible impact relates to system behavior.

## OpenTelemetry

Use .NET logging, metrics, and activity APIs with OpenTelemetry collection and OTLP export. Keep application instrumentation vendor-neutral.

### Traces

Activity sources:

```text
ProjectBuilder.Web
ProjectBuilder.Application
ProjectBuilder.Modeling
ProjectBuilder.Persistence
ProjectBuilder.Projections
ProjectBuilder.Integrations
ProjectBuilder.Collaboration
```

Representative spans:

- `project.create`
- `model.scope.load`
- `change-set.validate`
- `change-set.apply`
- `change-set.persist`
- `project.validate`
- `lens.project`
- `projection.generate`
- `export.build`
- `integration.evidence.sync`

Tags:

- operation name,
- element kind, not title,
- project size band,
- revision distance,
- finding count and category,
- projection kind,
- result category,
- cache outcome,
- retry count.

Avoid raw model text, actor names, contract payloads, tokens, or full project identifiers in externally shared telemetry unless policy permits.

### Metrics

#### Application
- command count and latency by result.
- query count and latency.
- change-set operation count.
- validation duration and finding count.
- model import and export duration.
- projection duration and output size.
- concurrency conflict rate.
- stale evidence count.
- review and baseline activity.

#### Client
- boot duration,
- route and lens load,
- render duration,
- input latency,
- visible node and edge count,
- memory,
- dropped frames,
- SignalR reconnects,
- local draft recovery.

#### Persistence
- query duration,
- transaction duration,
- connection pool,
- lock wait,
- deadlock,
- rows and bytes read,
- outbox lag,
- snapshot age.

#### Worker
- queue depth,
- job age,
- attempts,
- failure and dead-letter,
- cancellation,
- throughput.

#### Reliability
- availability,
- error budget,
- recovery time,
- backup age,
- restore rehearsal success.

### Logs

Structured events include:

- stable event identifier,
- severity,
- correlation and trace,
- actor identity reference where authorized,
- workspace and project pseudonymous reference,
- result category,
- rule or problem code,
- revision,
- duration.

Do not log request or model bodies by default.

## Health checks

### Liveness
Process can run and respond.

### Readiness
Critical dependencies required for safe traffic are available.

### Startup
Migrations and initialization completed.

### Degraded health
Optional integrations can be unavailable without making the core application unready. Their state appears separately.

Health endpoints require appropriate exposure policy. Detailed dependency information is not public.

## Service-level objectives

Initial hypotheses:

- Core authenticated application availability: 99.9% for hosted production.
- Acknowledged committed change durability: no loss.
- Interactive command latency p95 under 300 ms for ordinary changes.
- project overview p95 under 500 ms.
- SignalR revision notification p95 under 2 seconds.
- background standard projection completed within 60 seconds.
- recovery point and time objectives set by deployment tier.

These become commitments only after operational validation.

## Performance model

Performance depends on:

- total project elements,
- elements in loaded scope,
- relation density,
- visible lens nodes,
- validation rule count,
- change-set size,
- concurrent editors,
- historical depth,
- projection output size.

Benchmarks report all dimensions.

## Client performance

### Canvas
- cull outside viewport,
- reduce detail by zoom,
- batch geometry and DOM updates,
- avoid per-node .NET/JS interop calls,
- use pointer-event coalescing where available,
- cache text measurements,
- defer non-visible inspector data,
- avoid full graph re-projection for layout-only changes.

### WebAssembly
- lazy-load studio assemblies,
- publish trimming cautiously with tests,
- evaluate AOT only after startup and runtime profiles are measured,
- keep client contracts compact,
- compress static assets,
- use service worker only when offline behavior is defined.

### State management
Use explicit immutable client state and reducers or command handlers for Studio state. Avoid global mutable component services that make updates unpredictable.

## Server performance

- query purpose-built projections,
- paginate,
- use compiled serialization metadata,
- avoid N+1 queries,
- batch relation reads,
- cache immutable revisions and projection artifacts,
- limit command scope,
- move large generation to background worker,
- measure before adding distributed cache.

## Validation performance

Rules declare:

- scope,
- dependencies,
- incremental invalidation keys,
- severity profile.

After a change, reevaluate:

1. local structural rules,
2. directly dependent semantic rules,
3. broader readiness and traceability asynchronously where safe.

A full-project validation remains available and is required for baselines.

## Load and scale tests

Data profiles:

- Small: 100 elements, 200 relations.
- Medium: 5,000 elements, 15,000 relations.
- Large: 50,000 elements, 200,000 relations.
- Dense lens: 5,000 visible nodes.
- Long history: 100,000 change operations.
- Collaboration: 50 active editors on one project.
- Workspace search: 1,000,000 elements.

Tests identify practical limits. The UI should prevent accidental rendering of an unreadable dense view even if the backend can return it.

## Operational jobs

- outbox dispatch,
- projection generation,
- snapshot creation,
- evidence synchronization,
- search indexing,
- retention and purge,
- stale-evidence recalculation,
- integration health,
- backup verification,
- audit integrity check.

Every job supports:

- idempotency,
- retry classification,
- cancellation,
- progress,
- correlation,
- dead-letter or intervention,
- safe resumption.

## Deployment operations

A release needs:

- migration plan,
- compatibility matrix,
- health verification,
- smoke tests,
- rollback or forward-fix strategy,
- observability dashboard,
- alert changes,
- known-risk list,
- model baseline.

## Alerts

Page-worthy:

- unavailable core write path,
- data corruption or invariant breach,
- sustained high error rate,
- database unavailable,
- authentication failure spike suggesting attack,
- backup failure beyond threshold,
- outbox age threatening correctness,
- cross-tenant access detection.

Ticket-worthy:

- optional integration failure,
- growing projection queue within tolerance,
- elevated conflict rate,
- stale snapshot schedule,
- performance budget regression.

Avoid alerts for every individual user validation error.

## Runbooks

Required runbooks:

- database unavailable,
- object storage unavailable,
- SignalR degradation,
- stuck outbox,
- projection worker backlog,
- failed migration,
- accidental project deletion,
- restore project or workspace,
- compromised integration credential,
- malicious import,
- agent-provider outage,
- high client memory use,
- version incompatibility.

Runbooks are modeled human procedures and dogfood examples.
