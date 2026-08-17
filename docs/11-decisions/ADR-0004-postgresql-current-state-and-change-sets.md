# ADR-0004: Use PostgreSQL Current-State Storage with Append-Only Semantic Change Sets

## Status

Accepted.

## Context

Project Builder needs current model queries, strong revision consistency, history, semantic diff, audit, import/export, and flexible typed payloads. Full event sourcing would add replay, upcasting, temporal-query, debugging, and projection obligations before those benefits are proven.

A document-only store would make relation constraints, tenancy, concurrency, and query indexing harder. A rigid table per every element kind would make meta-model evolution expensive.

## Decision

Use PostgreSQL through EF Core 10.

Persist:

- normalized workspace/project identity and ownership,
- normalized elements and relations with kind/version/index fields,
- typed payload JSONB at explicit versioned boundaries,
- append-only semantic change sets and operations,
- current-state rows,
- revision snapshots at measured intervals,
- separate view layout state,
- claims, evidence, findings, comments, baselines, and audit,
- transactional outbox.

A semantic commit atomically writes current state, revision history, and outbox. It uses expected project and element versions. Last-write-wins is prohibited.

## Consequences

### Benefits

- strong relational constraints and transactions,
- efficient current-state reads,
- flexible typed payload evolution,
- deterministic history,
- queryable relations,
- no mandatory replay for ordinary reads.

### Costs

- state and change-set consistency must be maintained,
- JSONB indexing requires query discipline,
- semantic migrations differ from database migrations,
- historical reconstruction is bounded by operation support and snapshots.

## Data rules

- domain types do not carry EF attributes,
- provider-specific types remain in Infrastructure,
- portable project format is independent of table layout,
- imports validate completely before persistence,
- view moves do not increment semantic revision,
- evidence has its own lifecycle and can become stale.

## Rejected alternatives

- full event sourcing at launch,
- generic graph database as primary store,
- one JSON document per project as the only database shape,
- table per concrete element kind.

## Validation

- transaction rollback,
- stale revision conflict,
- idempotent commit,
- JSONB query profile,
- export-import determinism,
- migration fixtures,
- backup/restore rehearsal,
- property that view-state operations preserve semantic content.

## Review triggers

- temporal queries or branch/merge require event replay as a first-class capability,
- graph traversal cannot meet measured workloads,
- JSONB payloads become ungoverned,
- independent service extraction changes data ownership.
