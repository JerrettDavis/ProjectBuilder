# Engineer and Validator Guide

## Purpose

This guide explains how to implement from a Project Builder definition and return trustworthy evidence to it. The engineer is not handed a frozen diagram. The model is a versioned definition whose claims can be questioned and refined. The validator selects evidence appropriate to each claim rather than relying on one testing layer.

## Begin from a baseline or revision

Record:

- project identifier,
- model revision,
- purpose profile,
- selected capability, episode, scenario, and interaction,
- projection version,
- unresolved findings,
- accepted risks,
- applicable decisions.

Do not implement from an exported document whose source revision is unknown.

## Read the slice in this order

1. Outcome and beneficiary.
2. Scenario starting facts and trigger.
3. Semantic results and observations.
4. State transition.
5. Rules and invariants.
6. Authority and constraints.
7. Interfaces.
8. boundaries and contracts.
9. operational properties.
10. evidence requirements.
11. architecture decisions.

This order protects the domain language from accidental adaptation to a preferred framework.

## Translate the model into code responsibilities

### Domain types

Create types that make invalid states difficult or impossible.

```csharp
public readonly record struct TransactionId(Guid Value);

public sealed record ActiveTransaction(
    TransactionId Id,
    StoreId StoreId,
    ImmutableArray<TransactionLine> Lines,
    Money Total,
    TransactionVersion Version);

public abstract record AddScannedItemResult
{
    private AddScannedItemResult() { }

    public sealed record Added(
        TransactionLine Line,
        ActiveTransaction Transaction) : AddScannedItemResult;

    public sealed record UnknownProduct(ProductCode Code) : AddScannedItemResult;

    public sealed record Prohibited(
        ProductId ProductId,
        ProhibitionReason Reason) : AddScannedItemResult;

    public sealed record PriceUnavailable(ProductId ProductId) : AddScannedItemResult;

    public sealed record Conflict(TransactionVersion Actual) : AddScannedItemResult;
}
```

Use records, discriminated-result patterns, smart constructors, and immutable collections where they improve truthfulness. Do not create stringly typed dictionaries for central concepts.

### Pure domain behavior

```csharp
public static AddLineDecision DecideAddLine(
    ActiveTransaction transaction,
    SellableProduct product,
    StorePrice price,
    Quantity quantity,
    SalePolicy policy)
{
    // Pure decision. No database, clock, network, logging, or UI.
}
```

A decision can produce:

- next state,
- semantic result,
- domain events,
- requested effects.

The application layer decides how and when external effects occur.

### Application use case

The application use case:

1. authorizes the actor and intent,
2. validates request shape,
3. resolves needed state through ports,
4. invokes domain decisions,
5. coordinates transaction and concurrency,
6. schedules or performs external effects,
7. maps to an application result,
8. emits telemetry and audit safely.

Avoid a generic handler pipeline that obscures transaction, authorization, or semantic result ownership.

### Infrastructure adapters

An adapter translates an external mechanism into owned concepts.

```csharp
public interface IStorePriceBook
{
    Task<StorePriceLookupResult> FindPriceAsync(
        StoreId storeId,
        ProductCode productCode,
        CancellationToken cancellationToken);
}
```

Provider-specific SDK types remain inside the adapter. Map transport failures, timeouts, missing records, invalid data, and version issues into owned result types.

### Presentation adapter

Presentation accepts interface input, constructs an application intent, and renders semantic results. It does not infer success from an HTTP status alone or mutate persistence directly.

## Preserve semantic result exhaustiveness

Every modeled result must be:

- represented in code,
- deliberately combined with another result through an approved model change,
- or reported as unsupported.

Use compiler-supported exhaustiveness where practical. An analyzer can flag non-exhaustive result handling.

## Implement state and invariants

### State transition checklist

- source state is explicit,
- command or event trigger is typed,
- precondition failure returns a semantic result,
- next state is constructed atomically,
- all invariants are checked by construction or decision,
- events describe occurrences rather than imperative instructions,
- effects do not occur before the state decision is durable unless the protocol requires a modeled saga or compensation.

### Invariant proof strategies

| Claim | Suitable evidence |
|---|---|
| value cannot be empty | smart constructor and unit examples |
| total always equals line calculation | property tests plus calculation examples |
| stale revision cannot overwrite | database concurrency integration test |
| duplicate request has one effect | idempotency integration and adapter contract tests |
| unauthorized role cannot mutate | application/API authorization tests |
| error is keyboard accessible | component and browser accessibility evidence |
| provider timeout degrades safely | adapter integration plus end-to-end failure injection |
| backup can restore | operational rehearsal |

No single test layer proves all of these.

## Use Definition-Validated Delivery

### Define

Confirm model claims and unresolved questions. Refine only what implementation reveals.

### Delegate

Select the smallest slice and make responsibilities explicit. Human or agent work uses the same definition.

### Validate

Produce evidence tied to claims. A passing test without a claim link can still be useful, but it does not close model coverage automatically.

### Refine

Unexpected behavior, ambiguity, and design discoveries return to the model. Do not patch around a false definition and leave it unchanged.

## Test design

### Example specifications

Use concrete examples to teach behavior.

```csharp
[Test]
public async Task Known_sellable_product_is_added_at_the_store_price()
{
    var scenario = await Given.ActiveTransactionAt(StoreId.From("104"));
    await And.PriceBookContains(ProductCode.Parse("012345678905"), Money.Usd(3.49m));

    var result = await When.Scan("012345678905");

    await Then.ResultIsItemAdded(result);
    await And.TransactionContains("012345678905", quantity: 1);
    await And.TotalIs(Money.Usd(3.49m));
    await And.InvariantHolds(TransactionInvariants.TotalMatchesLines);
}
```

Attach scenario and claim identifiers through attributes, metadata, generated manifests, or test result enrichment.

### Property tests

Generate broader evidence:

- valid add preserves transaction invariants,
- invalid or unrecognized tokens never mutate transaction,
- canonical model round-trip is stable,
- view-state moves never alter semantic hash,
- every accepted model operation preserves acyclic containment,
- duplicate idempotency key produces at most one committed effect,
- authorization is monotonic according to policy,
- stale evidence is detected when a covered claim changes.

### Contract tests

Verify:

- project JSON schema and migrations,
- API problem types and concurrency,
- provider mapping,
- event compatibility,
- MCP tool schemas and side-effect classification,
- generated code compilation,
- import limits and unsafe content rejection.

### Integration tests

Use real PostgreSQL for relational behavior. Test:

- constraints,
- transactions,
- JSONB queries,
- concurrency,
- migrations,
- outbox atomicity,
- idempotency,
- tenant filtering,
- backup fixtures where practical.

### Browser and device tests

Use the real interface for:

- keyboard route,
- focus behavior,
- visual and announced results,
- loading and degraded states,
- device simulator input,
- stale conflict,
- revision history,
- scenario playback.

Do not automate every visual detail. Automate high-value actor outcomes and semantic states.

## Evidence lifecycle

Evidence states:

- Planned.
- Produced.
- Passed.
- Failed.
- Stale.
- Superseded.
- AcceptedRisk.

An evidence record includes:

- covered claim identifiers,
- model revision,
- code revision,
- environment,
- producer,
- started and completed time,
- result,
- artifact URI or digest,
- tool and version,
- summary,
- limitations.

Evidence becomes stale when:

- covered claim changes,
- relevant interface or contract changes,
- projection logic changes,
- test is disabled,
- environment no longer represents the target,
- policy defines an expiration.

## Handling model defects discovered during implementation

1. Stop treating the current wording as authoritative.
2. Record a finding against the claim.
3. Add a concrete example or counterexample.
4. involve the domain or decision owner.
5. commit a model revision.
6. rebase the implementation projection.
7. update tests and code.
8. retain the history of why the definition changed.

Do not silently reinterpret a requirement in code comments.

## Code review using the model

A reviewer should inspect:

- model diff,
- code diff,
- generated projection diff,
- evidence,
- decisions and risks.

Questions:

- Does the code preserve the modeled result vocabulary?
- Is the domain decision pure enough to test directly?
- Are provider failures translated?
- Is interface behavior truthful for every result?
- Does the transaction boundary match the invariant?
- Are retries safe and modeled?
- Did a new state, rule, or boundary emerge without a model update?
- Are tests proving claims or merely call sequences?
- Is generated code readable and deterministic?
- Does the implementation narrow future choices without an ADR?

## Release validation

Before baseline approval:

- execute all claim-linked tests,
- review stale and failed evidence,
- run migrations from every supported version,
- rehearse backup and restore,
- exercise degraded dependencies,
- review security and accessibility evidence,
- compare performance to the reference envelope,
- inspect observability under happy and failure paths,
- confirm dogfood traceability.

## Validator anti-patterns

### Treating acceptance criteria as the full test plan

Acceptance examples do not replace properties, contracts, security, accessibility, and operational evidence.

### Mocking every collaborator

Call-order tests can freeze implementation without proving outcome. Fake owned ports and use real adapters at contract/integration layers.

### Using EF InMemory as a database proof

It does not reproduce PostgreSQL transactions, constraints, queries, concurrency, or collation.

### Marking an agent response as evidence

An agent can propose a test or summarize evidence. It does not prove the claim.

### Ignoring failed test artifacts after a rerun

Retain the final release evidence and enough failure history to support diagnosis and audit according to policy.

## Completion handoff

```markdown
## Source definition
- project:
- revision:
- slice:
- purpose profile:

## Behavior implemented
...

## Model refinements
...

## Evidence
| Claim | Evidence | Result | Artifact |
|---|---|---|---|

## Decisions
...

## Risks and limitations
...

## Operational notes
...

## Recommended next slice
...
```
