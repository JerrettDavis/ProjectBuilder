# ADR-0002: Begin as a Modular Monolith

## Status

Accepted.

## Context

Project Builder has evolving domain boundaries, strong consistency needs for semantic commits, a small initial team, and no measured requirement for independent business-service deployment. Premature distribution would add network contracts, eventual consistency, deployment coordination, observability, and recovery complexity.

The product still needs internal boundaries that can be tested and, if justified later, extracted.

## Decision

Build the initial product as one deployable ASP.NET Core application with compiler-enforced modules and clear ports for real external systems.

Allow a separately hosted projection/background worker only when long-running work creates a measured need. Local Aspire orchestration does not define production topology.

## Module expectations

- Domain is framework/provider independent.
- Application owns use cases and ports.
- Infrastructure implements external mechanisms.
- Web/API and client are presentation.
- Projections consume immutable snapshots.
- PostgreSQL is the initial transactional authority.
- outbox supports asynchronous follow-up after commit.

## Consequences

### Benefits

- atomic model commits,
- simpler debugging and deployment,
- lower operational overhead,
- faster refactoring while language evolves,
- ordinary in-process calls with explicit ownership.

### Costs

- module discipline must be enforced without network boundaries,
- scale is initially at application/database granularity,
- long-running work requires careful isolation,
- extraction later requires stable contracts.

## Service extraction criteria

A module earns independent deployment when several are true:

- distinct scaling profile,
- failure isolation materially protects user outcomes,
- independent release cadence,
- separate data ownership and consistency model,
- stable versioned contract,
- operational team ownership,
- evidence that in-process deployment is a constraint.

## Rejected alternatives

- microservices by domain noun,
- one service per bounded context before boundaries stabilize,
- serverless functions as primary internal architecture,
- full distributed event sourcing.

## Validation

Architecture tests, module dependency rules, load tests, failure experiments, and periodic review of measured extraction drivers.

## Review triggers

- projection workload harms interactive latency,
- tenant isolation needs separate infrastructure,
- one module has independently owned release/scale requirements,
- reliability evidence shows shared-process failure is unacceptable.
