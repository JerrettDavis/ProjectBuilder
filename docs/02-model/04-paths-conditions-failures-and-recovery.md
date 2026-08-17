# Paths, Conditions, Failures, and Recovery

## Purpose

A model becomes useful when it explains what happens outside the ideal example. Project Builder prompts for non-happy behavior at every meaningful interaction and boundary, but it does not force authors to enumerate imaginary edge cases without consequence.

## Path categories

### Happy path
The ordinary successful route under expected valid conditions.

### Alternate path
A valid route to the intended outcome using different facts or interactions.

Example: entering a product code manually instead of scanning.

### Exceptional path
A path where the intended operation cannot proceed because input, authority, state, dependency, or execution is invalid or failed.

### Degraded path
A reduced but safe service path.

Example: operate from a permitted cached price snapshot when the current corporate price service is unavailable.

### Recovery path
Behavior that restores a safe state, retries, resumes, or guides intervention after failure.

### Cancellation path
Behavior when a participant intentionally stops work.

### Compensation path
Behavior that semantically counteracts a completed effect when true rollback is not possible.

## Failure taxonomy

Project Builder uses a shared taxonomy to improve prompts and reporting.

### Input
- malformed,
- incomplete,
- unsupported,
- ambiguous,
- out of range,
- stale,
- duplicate.

### Authority
- unauthenticated,
- unauthorized,
- approval missing,
- segregation-of-duty conflict,
- policy denied.

### State
- precondition not met,
- already completed,
- conflicting edit,
- invariant would be violated,
- resource not found,
- version stale.

### Dependency
- unavailable,
- timeout,
- throttled,
- incompatible contract,
- invalid response,
- partial response,
- stale response.

### Execution
- unexpected exception,
- resource exhaustion,
- deadlock or contention,
- process restart,
- data corruption,
- deployment mismatch.

### Human and operational
- operator abandons work,
- device disconnected,
- paper or cash unavailable,
- training gap,
- manual override,
- escalation not answered.

### Security and abuse
- replay,
- tampering,
- injection,
- enumeration,
- privilege escalation,
- fraud pattern,
- data exfiltration.

## Path record

A path records:

- classification,
- entry condition,
- source step or transition,
- ordered segments,
- terminal result,
- state at termination,
- observations,
- recovery or escalation,
- retry and idempotency policy,
- evidence requirement,
- owner,
- accepted unresolved gaps.

## Branching questions

For each interaction:

1. Can the input be absent, malformed, duplicated, stale, or ambiguous?
2. Can the initiator lack authority?
3. Can current state make the intent invalid?
4. Can the receiver be unavailable or slow?
5. Can the receiver return a valid but negative result?
6. Can work complete partially?
7. Can the operation be retried safely?
8. What does the participant observe while waiting?
9. Can the participant cancel?
10. What state remains after failure?
11. Is manual intervention required?
12. What evidence proves recovery?

The guidance engine selects relevant questions based on element kinds and boundaries. It should not dump the full list every time.

## Semantic results

Avoid a single `Success/Failure` boolean. A use case can return:

```csharp
public abstract record AddScannedValueResult
{
    public sealed record Added(TransactionView View) : AddScannedValueResult;
    public sealed record NotRecognized(CapturedValue Value) : AddScannedValueResult;
    public sealed record NotSellable(ProductId ProductId, Reason Reason) : AddScannedValueResult;
    public sealed record PriceUnavailable(RetryAdvice Advice) : AddScannedValueResult;
    public sealed record TransactionClosed(TransactionId Id) : AddScannedValueResult;
    public sealed record Denied(AuthorizationReason Reason) : AddScannedValueResult;
    public sealed record Conflict(Revision Expected, Revision Actual) : AddScannedValueResult;
}
```

Unexpected infrastructure or programming failures still exist. They map to safe external error behavior and operational evidence, not to vague domain results.

## Retry

A retry policy needs:

- retryable failure classification,
- maximum attempts or deadline,
- backoff,
- jitter if applicable,
- idempotency key,
- duplicate result behavior,
- user-visible status,
- cancellation,
- exhausted result,
- telemetry.

"Retry three times" without semantic analysis is not a complete model.

## Idempotency

The model asks:

- What identifies the logical request?
- Where is the idempotency decision owned?
- What result is replayed?
- How long is the key valid?
- What happens when payload differs under the same key?
- Can downstream effects also be deduplicated?
- How is the user informed?

Examples:

- A scanner may emit the same barcode twice because the item was physically scanned twice. Those are two intended actions and must not be deduplicated merely because values match.
- A network retry of the same `AddScannedValue` command should not add the line twice. It needs an operation identity distinct from the barcode value.

## Rollback and compensation

Rollback restores a prior technical state within a transaction.

Compensation creates new business behavior that counteracts an earlier result.

Example:

- Rolling back an uncommitted line insert is technical rollback.
- Voiding a captured payment is compensation.
- Printing a correction receipt is an additional effect.
- Inventory or loyalty consequences may need their own compensation.

Project Builder should prompt authors not to use "rollback" as a generic promise.

## Partial completion

A path must state which facts and effects completed.

Example:

- Payment authorization succeeded.
- Local transaction commit failed.
- The system now owns a recovery obligation.
- The customer should not be asked to pay again without checking the prior authorization.
- Operations need correlation identifiers and a reconciliation view.

The model can represent a saga or process manager only after the partial-completion behavior requires one.

## Degraded operation

A degraded path records:

- quality reduced,
- affected actors,
- permitted duration,
- stale-data limits,
- prohibited actions,
- banner or observation,
- audit requirement,
- exit condition,
- reconciliation.

For POS price lookup, offline pricing may be allowed for ordinary items but forbidden for regulated or rapidly changing products.

## Failure presentation

Every externally meaningful result maps to interface behavior.

A failure representation should answer:

- What happened in user language?
- What remains safe?
- What can the actor do?
- Will retry create duplicates?
- Is support or escalation needed?
- What reference can be communicated?
- Which detail must remain hidden for security?

## Path closure

A path is structurally closed when it ends in:

- a semantic result,
- a transition to another modeled scenario,
- an explicit unresolved gap,
- a controlled terminal state,
- a recovery or compensation obligation with an owner.

A path that simply stops at "service error" is incomplete.

## Evidence

Failure and recovery evidence can include:

- example tests for each semantic result,
- property tests for idempotency or invariant preservation,
- contract tests for provider errors,
- integration tests for partial completion,
- fault injection,
- timeout tests,
- restart and replay tests,
- backup and recovery rehearsal,
- human procedure walkthrough,
- production simulation or chaos experiment where proportionate.
