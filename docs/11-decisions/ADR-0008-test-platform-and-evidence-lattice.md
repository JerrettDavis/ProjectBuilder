# ADR-0008: Use One .NET Test Platform and an Evidence Lattice

## Status

Proposed pending bootstrap compatibility proof.

## Context

Project Builder needs ordinary unit tests, properties, real PostgreSQL integration, API and component tests, browser tests, architecture rules, generator compile tests, and operational evidence. Mixing test platforms complicates commands, tooling, coverage, and CI.

A traditional test pyramid does not express that accessibility, contracts, security, migrations, and recovery prove different claims.

## Proposed decision

Adopt Microsoft.Testing.Platform with NUnit repository-wide, subject to a bootstrap spike proving:

- supported .NET 10 execution,
- IDE integration,
- CI result output,
- code coverage,
- filtering,
- parallelization and isolation,
- Playwright integration,
- architecture/generator tests,
- local developer experience.

If a blocking incompatibility exists, choose one repository-wide alternative and record evidence. Do not mix MTP and VSTest in one normal invocation.

Use an evidence lattice:

- type/static analysis,
- examples,
- property tests,
- transition tests,
- application integration,
- adapter and contract tests,
- real persistence/migration,
- API,
- component/browser/device,
- accessibility/security,
- performance/resilience,
- operational rehearsal.

## Consequences

### Benefits

- one command model,
- clear test ownership,
- proof selected by claim,
- better release traceability,
- future model-to-test binding.

### Costs

- initial platform/tooling spike,
- more than one test layer is required,
- evidence metadata and staleness tracking,
- operational tests need environments and ownership.

## Rules

- EF InMemory is not relational proof,
- mocks do not substitute for owned contract tests,
- snapshots require semantic review,
- coverage percentage is not claim coverage,
- failed evidence remains visible,
- agent output is not proof.

## Acceptance proof

The bootstrap session must produce:

- a unit/example test,
- property test,
- real PostgreSQL integration,
- API/health test,
- architecture test,
- schema contract test,
- CI results and coverage,
- documented one-command invocation.

## Review triggers

- tooling cannot support required tests,
- test execution cost becomes prohibitive,
- model-driven execution needs a specialized runner,
- .NET test platform support changes.
