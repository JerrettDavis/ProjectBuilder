# Application Command, Query, and Event Model

## Objectives

The application layer must:

- expose intention-revealing use cases,
- enforce authorization and concurrency,
- coordinate domain behavior and infrastructure effects,
- produce semantic results,
- record one atomic change set,
- remain understandable without framework indirection.

## Commands

A command requests a state change.

Example:

```csharp
public sealed record CommitProjectChangeSet(
    ProjectId ProjectId,
    Revision ExpectedRevision,
    DraftChangeSet Draft,
    CommitReason Reason) : ICommand<CommitProjectChangeSetResult>;
```

Command requirements:

- named for user or system intent,
- immutable,
- carries required operation identity,
- contains values, not service dependencies,
- does not expose provider DTOs,
- has explicit expected revision when concurrency matters,
- supports cancellation at application boundaries,
- returns a semantic result.

## Command handler

```csharp
internal sealed class CommitProjectChangeSetHandler(
    IProjectModelStore store,
    IAuthorizationEvaluator authorization,
    IClock clock,
    IOutbox outbox)
{
    public async ValueTask<CommitProjectChangeSetResult> HandleAsync(
        CommitProjectChangeSet command,
        ActorContext actor,
        CancellationToken cancellationToken)
    {
        var access = await authorization.CanEditAsync(
            actor, command.ProjectId, cancellationToken);

        if (!access.Allowed)
            return new CommitProjectChangeSetResult.Denied(access.Reason);

        var current = await store.LoadForChangeAsync(
            command.ProjectId, command.Draft.Scope, cancellationToken);

        var committed = ProjectModelTransition.TryCommit(
            current,
            command.ExpectedRevision,
            command.Draft,
            actor,
            clock.GetCurrentInstant(),
            command.Reason);

        if (committed is ProjectModelTransitionResult.Rejected rejected)
            return Map(rejected);

        var accepted = (ProjectModelTransitionResult.Accepted)committed;

        await store.CommitAsync(accepted.Commit, cancellationToken);
        await outbox.AddRangeAsync(accepted.Events, cancellationToken);

        return new CommitProjectChangeSetResult.Committed(
            accepted.Revision,
            accepted.ChangedElements,
            accepted.Findings,
            accepted.Impact);
    }
}
```

The real persistence transaction wraps state, history, and outbox.

## Semantic results

Results are closed hierarchies or discriminated records.

```csharp
public abstract record CommitProjectChangeSetResult
{
    public sealed record Committed(
        Revision Revision,
        ImmutableArray<ElementId> Changed,
        ImmutableArray<ModelFinding> Findings,
        ChangeImpact Impact) : CommitProjectChangeSetResult;

    public sealed record Conflict(
        Revision Expected,
        Revision Actual,
        ImmutableArray<SemanticConflict> Conflicts)
        : CommitProjectChangeSetResult;

    public sealed record Invalid(
        ImmutableArray<ModelFinding> Findings)
        : CommitProjectChangeSetResult;

    public sealed record Denied(
        AuthorizationReason Reason)
        : CommitProjectChangeSetResult;

    public sealed record ProjectNotFound(ProjectId ProjectId)
        : CommitProjectChangeSetResult;
}
```

Unexpected failures are logged, traced, and mapped at the presentation boundary to a safe problem response.

## Queries

A query retrieves data without changing semantic state.

Examples:

- `GetProjectOverview`.
- `GetModelScope`.
- `SearchWorkspaceModel`.
- `GetLensProjection`.
- `GetRevisionDiff`.
- `GetEvidenceMatrix`.
- `GetReadinessReport`.

Queries return purpose-built read models. They do not return tracked EF entities or full aggregate internals.

## Query consistency

A query response includes:

- project revision,
- read-model or projection version,
- optional ETag,
- generated-at timestamp for asynchronous projections,
- stale indicator where applicable.

Clients can decide whether to refresh or continue editing against their base revision.

## Events

### Domain events

Created by domain transitions when an occurrence matters inside the model:

- `ElementAdded`.
- `RelationChanged`.
- `BaselineEstablished`.
- `GapResolved`.

These can remain rich internal records and be folded into application events.

### Application events

Communicate committed outcomes across modules:

- `ProjectRevisionCommitted`.
- `EvidenceBecameStale`.
- `ProjectionRequested`.
- `ReviewRequested`.

### Integration events

Versioned external contracts:

- `ProjectBaselinePublished`.
- `EvidenceStatusChanged`.

Do not expose internal domain events directly to external consumers without an anti-corruption projection.

## Event envelope

```csharp
public sealed record EventEnvelope<T>(
    EventId EventId,
    string EventType,
    int SchemaVersion,
    Instant OccurredAt,
    CorrelationId CorrelationId,
    CausationId? CausationId,
    WorkspaceId WorkspaceId,
    ProjectId? ProjectId,
    ActorReference Actor,
    T Data);
```

## Outbox

Events and background tasks that depend on committed state use a transactional outbox.

Requirements:

- outbox record stored in same database transaction,
- idempotent dispatcher,
- attempt count and next attempt,
- dead-letter or manual intervention state,
- correlation,
- telemetry,
- retention,
- consumer deduplication where needed.

## Pipelines and decorators

Cross-cutting behavior can wrap handlers:

1. input contract validation,
2. authentication context,
3. authorization,
4. idempotency,
5. concurrency,
6. transaction,
7. handler,
8. outbox commit,
9. telemetry and audit.

Order is explicit and tested. Domain validation remains inside domain transitions.

A custom lightweight dispatcher can be added when it reduces repetition. Avoid making handlers discoverable only through runtime reflection or obscure registration.

## Validation layers

### Transport validation
Shape, required syntax, size, and safe parsing.

### Application validation
Actor scope, project existence, authorization, command prerequisites, expected revision.

### Domain validation
Rules, invariants, allowed transitions, semantic relationships.

### Integration validation
Provider contract and response mapping.

Each layer returns the most meaningful result available.

## Idempotency

State-changing API commands can include:

```text
Idempotency-Key
```

The application records:

- key,
- actor or client scope,
- command type,
- request hash,
- result reference,
- expiry.

Reusing a key with different semantic input is a conflict.

Canvas move drafts do not need server idempotency until committed. Change-set commit does.

## Concurrency

Commands identify:

- project expected revision,
- optionally element expected versions,
- draft operation identities.

The domain returns field- or relationship-level conflicts, not only HTTP 409.

## Cancellation

Cancellation tokens stop work that has not committed. Once a transaction commits, cancellation does not pretend the command did not happen. The API returns or allows retrieval by operation identity.

Long-running projections are explicit jobs with cancellation state and cleanup behavior.

## APIs as presentation

Minimal APIs map transport to use cases:

```csharp
group.MapPost("/{projectId}/change-sets", CommitChangeSetEndpoint.Handle)
    .RequireAuthorization(ProjectPolicies.Edit)
    .RequireRateLimiting("model-writes")
    .WithName("CommitProjectChangeSet")
    .Produces<CommitChangeSetResponse>(StatusCodes.Status200OK)
    .Produces<ConflictResponse>(StatusCodes.Status409Conflict)
    .Produces<ValidationProblemDetails>(StatusCodes.Status422UnprocessableEntity);
```

Endpoint code stays thin and exhaustive over semantic results.

## No generic CRUD

The API should not expose arbitrary `POST /elements` and `PATCH /elements/{id}` as the only model behavior. Typed change-set operations and use cases preserve invariants and produce meaningful conflicts.

Administrative bulk import may accept broader documents but runs schema and semantic validation as one governed operation.
