# Testing and Evidence Strategy

## Test platform

Use one repository-wide .NET test platform. The initial recommendation is Microsoft.Testing.Platform with NUnit, subject to a bootstrap compatibility spike for IDE, coverage, and CI requirements.

TinyBDD-style specifications can sit over the selected test framework to preserve stakeholder-readable behavior.

Do not mix VSTest- and MTP-based projects in one test invocation.

## Evidence pyramid is not enough

Project Builder needs an **evidence lattice**. Different proof types cover different claims.

```text
Static and type evidence
Pure rule examples
Property tests
State transition tests
Application integration
Adapter contract tests
Persistence and migration tests
API contract tests
Browser or device E2E
Accessibility and security review
Performance and resilience experiments
Operational rehearsal and observation
```

No layer automatically supersedes the others.

## Test project responsibilities

### Domain.Tests

- value parsing,
- rules,
- state transitions,
- invariants,
- semantic result exhaustiveness,
- properties,
- change-set folding,
- deterministic serialization where domain-owned.

No database or network.

### Application.Tests

- use-case orchestration,
- authorization policy,
- idempotency,
- concurrency results,
- effect ordering,
- transaction intent,
- domain result mapping.

Use fakes that model ports explicitly. Avoid mocks that assert every internal call.

### Infrastructure.Tests

- EF mappings,
- PostgreSQL queries,
- transactions,
- migrations,
- outbox,
- object storage,
- adapters and provider mapping.

Use real PostgreSQL in integration tests. Do not rely on EF InMemory as a relational substitute.

### Api.Tests

- routes,
- authentication and authorization,
- anti-forgery,
- contracts,
- Problem Details,
- ETag and `If-Match`,
- idempotency,
- limits,
- OpenAPI,
- SignalR authorization.

### Web.Tests

- component behavior,
- client state transitions,
- keyboard behavior,
- focus restoration,
- canvas command dispatch,
- rendering from lens projection.

### Contract.Tests

- project JSON schemas,
- import fixtures,
- export determinism,
- API compatibility,
- realtime messages,
- integration provider contracts,
- generated artifact contracts.

### EndToEnd.Tests

- real browser,
- real Web Host,
- real PostgreSQL,
- representative object storage,
- seeded models,
- primary and failure scenarios.

Use Playwright for .NET unless the bootstrap spike identifies a blocking need.

### Architecture.Tests

- dependency direction,
- public surface,
- namespace and module constraints,
- provider leakage,
- forbidden references,
- generator loading boundaries.

## Behavior specification

Example:

```csharp
[Scenario("A modeler adds an actor to a project")]
public sealed class AddActorScenario
{
    [Test]
    public async Task Actor_is_committed_and_visible_in_the_project_model()
    {
        await Given.ProjectExists(revision: 3);
        await And.UserCanEditProject();
        await When.UserAddsActor("Clerk", ActorKind.HumanRole);
        await Then.ProjectRevisionIs(4);
        await And.ActorExists("Clerk");
        await And.ChangeSetRecordsReason("Add checkout operator");
        await And.NoBlockingFindingsExist();
    }
}
```

The implementation may use the user's TinyBDD conventions. The scenario maps to model identifiers through metadata or a manifest.

## Example tests

Use examples to teach behavior and preserve regressions.

Each example states:

- given facts,
- action,
- expected semantic result,
- expected state,
- invariant checks,
- source model claim.

## Property tests

Candidate properties:

- applying a valid view-layout move never changes semantic model hash,
- canonical export-import-export is byte stable,
- containment remains acyclic after any accepted operation sequence,
- all accepted transitions preserve invariants,
- duplicate commit with same idempotency key returns same result,
- relation validation is symmetric or asymmetric according to descriptor,
- applying inverse draft operations returns original draft state,
- a stale expected revision never overwrites committed content,
- no tenant-scoped query returns another workspace's records.

Generators must produce valid and invalid domains deliberately. Shrunk failures should be captured as named examples.

## Model-based tests

The canonical model itself can drive tests:

1. Load POS project fixture.
2. Enumerate scenarios with executable example data.
3. Map supported interactions to test drivers.
4. Run through API and browser adapters.
5. attach results to claim identifiers.

MVP can start with explicit bindings. Fully generic execution comes later.

## Contract tests

### Project format
Validate current and migration fixtures.

### API
Snapshot OpenAPI and run compatibility checks for stable endpoints.

### Providers
Verify request mapping, response mapping, errors, retries, and version assumptions against sandbox or recorded contracts.

### Generated code
Compile and execute generated samples.

## Persistence tests

Use a real database to verify:

- constraints,
- concurrency,
- transaction rollback,
- JSONB queries,
- collation and case rules,
- timestamp precision,
- migration from supported prior version,
- outbox atomicity,
- backup or snapshot restoration where practical.

## E2E scenario set for MVP

1. Create project.
2. Add actor and outcome.
3. Build episode and scenario.
4. add interaction and failure path.
5. define state and invariant.
6. open Story and Flow lenses.
7. move canvas node without semantic revision.
8. commit semantic edit and inspect history.
9. create baseline.
10. export and import.
11. stale-revision conflict.
12. keyboard-only guide completion.
13. unauthorized project access denied.
14. malicious import rejected.
15. POS item scan walkthrough.

## Accessibility evidence

- automated browser rules,
- keyboard scripts,
- focus assertions,
- screen-reader manual test plan,
- high-contrast review,
- reduced-motion test,
- drag-alternative test,
- semantic canvas outline comparison.

## Security evidence

- authentication and authorization tests,
- cross-tenant tests,
- anti-forgery,
- import fuzzing,
- content sanitization,
- rate-limit behavior,
- secret scan,
- dependency scan,
- static analysis,
- threat-model review,
- ASVS mapping.

## Performance tests

Microbenchmarks:

- graph traversal,
- validation rules,
- canonical serialization,
- diff and impact analysis,
- lens projection,
- layout.

Integration benchmarks:

- load model scope,
- commit change set,
- search,
- import and export,
- projection generation.

Browser benchmarks:

- first load,
- lens switch,
- pan and zoom,
- selection,
- edit commit,
- graph size thresholds.

Performance evidence includes hardware and data profile.

## Resilience tests

- database transient error,
- worker restart,
- duplicate outbox delivery,
- SignalR disconnect and reconnect,
- object storage unavailable,
- projection cancellation,
- import interruption,
- stale client conflict,
- partial external evidence sync.

## Test data

- deterministic builders and fixtures,
- POS canonical fixture,
- Project Builder dogfood fixture,
- generated large graph fixtures,
- malicious import corpus,
- migration fixtures for every supported format,
- no production data.

## Flake policy

A flaky test is a defect.

- quarantine only with owner and expiration,
- preserve failure evidence,
- identify time, randomness, concurrency, environment, or external dependency,
- no blind retries that hide failure,
- deterministic clocks and identifiers,
- provider tests isolated from ordinary unit suite.

## Coverage

Use code coverage diagnostically. Do not set a single repository percentage as proof.

Track:

- model claim coverage,
- branch and result coverage for rules,
- invariant checks,
- path coverage,
- contract coverage,
- mutation testing selectively for high-value pure rules.

## Evidence manifest

CI publishes a machine-readable manifest:

```json
{
  "build": "...",
  "sourceRevision": "...",
  "modelBaseline": "...",
  "results": [
    {
      "claimId": "0191...",
      "evidenceType": "scenario-test",
      "testId": "ProjectBuilder.EndToEnd.CreateProject",
      "status": "passing",
      "artifact": "..."
    }
  ]
}
```

Project Builder can ingest this later through an integration.
