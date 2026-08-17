# Item-Scan Vertical Slice

## Slice identity

**Capability:** POS-CAP-010 Enter Merchandise  
**Episode:** POS-EP-010 Add Merchandise  
**Primary scenario:** POS-SCN-010 Known Sellable Product Is Scanned  
**Primary outcome:** POS-OUT-001 Accurate Item Representation

## Outcome statement

Given an authorized clerk operating an active transaction, when an associated scanner captures a token that represents a known, sellable product with an applicable store price, the system adds exactly one intended line, recalculates the transaction under one policy version, and presents the updated state.

## Starting facts

```text
OperatingContext
- StoreId = 104
- RegisterId = 7
- ClerkRole = Clerk
- SessionStatus = Active

Transaction
- Id = T-9001
- Status = Active
- Version = 12
- Lines = []
- Currency = USD
- PolicyVersion = POS-POLICY-2026-08

Scanner
- DeviceId = SCN-7-A
- AssociatedRegister = 7
- Status = Ready

Price authority
- ProductCode 012345678905 resolves to Product P-100
- Product P-100 is active and sellable
- Applicable Store 104 price = USD 3.49
```

Concrete values are examples. Definitions identify which fields are authoritative and which are observational.

## Outer interaction contract

### Input

`ScannedTokenIntent`

```text
AttemptId
OperatingContextId
TransactionId
ExpectedTransactionVersion
DeviceId
RawToken
CapturedAt
```

### Application results

```text
ItemAdded
AlternateTokenClassified
InvalidSignal
UnknownProduct
InactiveProduct
MissingPrice
SaleProhibited
OverrideRequired
DependencyUnavailable
Conflict
DuplicateIgnored
Cancelled
UnexpectedFailure
```

`UnexpectedFailure` is reserved for an unclassified defect or infrastructure failure. It must not absorb expected semantic results.

### Observation

`TransactionInteractionObservation`

```text
AttemptId
SemanticResult
TransactionRevision?
ChangedLine?
VisibleStatus
AllowedNextIntents
Correlation
```

## Scene detail

### POS-SCE-011: capture scanner signal

#### Responsibilities

- verify the device is associated with the operating context,
- impose size and character limits,
- assign or preserve attempt identity,
- normalize transport representation without interpreting business meaning,
- acknowledge capture to the interface if needed.

#### Invalid paths

- missing device identity,
- unassociated device,
- empty token,
- token above size limit,
- malformed transport frame,
- duplicate device delivery,
- scanner disconnected.

#### Boundary

Physical device and protocol. Device-level retries and delivery identity must be distinguished from intentional repeated scans.

### POS-SCE-012: classify token

#### Domain vocabulary

```csharp
public abstract record TokenClassification
{
    public sealed record Product(ProductCode Code) : TokenClassification;
    public sealed record Payment(PaymentToken Token) : TokenClassification;
    public sealed record ManufacturerCoupon(CouponCode Code) : TokenClassification;
    public sealed record CorporateCoupon(CouponCode Code) : TokenClassification;
    public sealed record Special(SpecialCommandCode Code) : TokenClassification;
    public sealed record Unrecognized(NormalizedToken Token) : TokenClassification;
}
```

#### Rule

Classification is deterministic for a policy version and normalized token.

#### Invariant

Classification does not mutate transaction state and does not call a payment or coupon provider.

#### Evidence

- table of representative patterns,
- property that every accepted normalized token produces exactly one classification,
- property that classification has no transaction effect,
- precedence tests for overlapping patterns.

### POS-SCE-013: resolve product and price

#### Port

```csharp
public interface IProductPriceAuthority
{
    Task<ProductPriceResolution> ResolveAsync(
        StoreContext store,
        ProductCode code,
        BusinessInstant at,
        CancellationToken cancellationToken);
}
```

#### Owned results

```text
Resolved(SellableProduct, StorePrice, Provenance)
UnknownProduct
InactiveProduct
MissingPrice
Unavailable
TimedOut
InvalidAuthorityResponse
```

Transport statuses, SDK exceptions, and provider error strings are mapped inside Infrastructure.

#### Property decisions required

- live versus replicated lookup,
- freshness tolerance,
- latency target,
- timeout,
- retry,
- cache key,
- store/day/context specificity,
- fallback authority,
- price-version provenance,
- behavior when data is internally inconsistent.

These remain explicit assumptions or ADR candidates until sourced.

### POS-SCE-014: add transaction line

#### Domain command

```text
AttemptAddPricedProduct
- TransactionId
- ExpectedVersion
- Product
- StorePrice
- Quantity = 1
- PricingProvenance
- ActorAuthority
- PolicyVersion
```

#### Decision outline

```csharp
public static AddPricedProductDecision Decide(
    ActiveTransaction transaction,
    PricedProduct product,
    Quantity quantity,
    SaleContext context,
    SalePolicy policy)
{
    if (transaction.Status is not TransactionStatus.Active)
        return new AddPricedProductDecision.Rejected(
            AddItemRejection.TransactionNotActive);

    var eligibility = policy.Evaluate(product, context);

    return eligibility switch
    {
        SaleEligibility.Allowed allowed =>
            BuildAddedState(transaction, product, quantity, allowed),

        SaleEligibility.OverrideRequired required =>
            new AddPricedProductDecision.OverrideRequired(required),

        SaleEligibility.Prohibited prohibited =>
            new AddPricedProductDecision.Prohibited(prohibited),

        _ => throw new UnreachableException()
    };
}
```

The actual implementation should use repository conventions and exhaustive types. The example shows responsibility, not a required API.

#### Atomicity

The line, total, transaction version, and durable event/outbox intent commit atomically. A provider lookup may happen before this transaction, but a stale expected version prevents overwrite.

#### Idempotency

`AttemptId` is recorded or otherwise resolved so the same device/application attempt cannot commit twice. A second intentional scan has a different attempt identity.

### POS-SCE-015: present result

The interface receives an application observation. It:

- updates the transaction read model or applies a confirmed result,
- renders semantic status,
- announces necessary change,
- maintains operational focus,
- exposes only allowed next intents,
- does not infer a line was added from an optimistic animation.

## State transition table

| Source | Trigger | Guard/result | Target | Mutation |
|---|---|---|---|---|
| Active v12 | valid known product attempt | allowed | Active v13 | add line, recalculate |
| Active v12 | valid known product attempt | override required | Active v12 | none |
| Active v12 | valid known product attempt | prohibited | Active v12 | none |
| Active v12 | unknown product | not found | Active v12 | none |
| Active v12 | price unavailable | no approved fallback | Active v12 | none |
| Active v12 | commit | expected version is stale | current server version | none by this attempt |
| Active v13 | same AttemptId | prior success known | Active v13 | none |
| Active v12 | cancellation before commit | cancelled | Active v12 | none |

Application workflow state can move through Captured, Classifying, Resolving, Committing, Completed, Cancelled, or Failed without changing domain state until commit.

## Invariant details

### POS-INV-001: total composition

```text
Transaction.Total ==
  Sum(Line.Extension)
  + Taxes
  + Fees
  + Deposits
  - Discounts
  + Adjustments
```

The exact formula and rounding sequence are policy-owned. All terms use one currency.

### POS-INV-002: one attempt, at most one committed add

For an `AttemptId`, there is at most one successful mutation of the transaction.

### POS-INV-003: line provenance

Each line records enough identity to explain:

- product,
- quantity,
- unit price,
- pricing source/version,
- applied policy,
- actor/attempt,
- time or business instant.

### POS-INV-004: rejection isolation

Every non-success semantic result leaves the transaction unchanged by this attempt.

### POS-INV-005: result truthfulness

`ItemAdded` is returned only after the transaction mutation is durably accepted according to the use-case contract.

## Vertical implementation projection

```text
Presentation
  ScannerSignalEndpoint
  TransactionView
  ItemScanResultPresenter

Application
  AttemptScannedToken.Command
  AttemptScannedToken.Handler
  Authorization
  Idempotency
  Transaction coordination
  Result mapping

Domain
  NormalizedToken
  TokenClassificationPolicy
  ProductCode
  SellableProduct
  StorePrice
  SaleEligibilityPolicy
  ActiveTransaction.DecideAdd
  AddItemResult
  Invariants

Infrastructure
  Scanner protocol adapter
  ProductPriceAuthority adapter or local read model
  TransactionRepository
  Attempt/idempotency store
  Outbox publisher
  Audit writer

Contracts
  ScannedTokenIntent
  Product/price boundary contract
  Transaction observation
  Audit event

Evidence
  classification examples/properties
  domain examples/properties
  provider contract
  PostgreSQL concurrency/idempotency
  component result matrix
  browser/device E2E
  observability review
```

## Suggested code feature folders

```text
ProjectBuilder reference projection:

src/
  Pos.Domain/
    Merchandise/
      TokenClassification/
      AddPricedProduct/
  Pos.Application/
    Checkout/
      AttemptScannedToken/
  Pos.Infrastructure/
    Pricing/
    Persistence/
    Devices/
  Pos.Web/
    Checkout/
      Transaction/
tests/
  Pos.Domain.Tests/
  Pos.Application.Tests/
  Pos.Infrastructure.Tests/
  Pos.Web.Tests/
  Pos.EndToEnd.Tests/
```

Project Builder itself should generate a descriptor, not assume all target repositories use this exact folder structure.

## Acceptance examples

### Known product

```gherkin
Given transaction T-9001 is active at version 12 for Store 104
And product code 012345678905 resolves to an allowed product at USD 3.49
When associated scanner SCN-7-A submits attempt A-100 with that token
Then the result is ItemAdded
And transaction T-9001 is at version 13
And it contains one line for that product at USD 3.49
And the total invariant holds
```

### Duplicate delivery

```gherkin
Given attempt A-100 already added the product
When the same attempt is delivered again
Then no additional line is added
And the result refers to the previously accepted outcome or DuplicateIgnored
```

### Stale transaction

```gherkin
Given the clerk observed transaction version 12
And another accepted action commits version 13
When the scan attempt tries to commit against version 12
Then the result is Conflict
And version 13 is not overwritten
```

### Price authority unavailable

```gherkin
Given no approved local price fallback is available
When product-price resolution times out
Then the result is DependencyUnavailable
And the transaction is unchanged
And the clerk sees a recoverable action according to policy
```

## Telemetry

Trace span sequence:

```text
pos.scan.capture
pos.scan.classify
pos.product.resolve
pos.transaction.add
pos.result.present
```

Safe attributes can include:

- attempt correlation,
- store/register pseudonymous identifiers according to policy,
- token classification result,
- semantic result,
- duration,
- provider route,
- retry count,
- transaction version conflict.

Do not log raw payment/coupon/token content by default.

Metrics:

- scan outcome count by semantic result,
- product resolution latency,
- transaction commit latency,
- conflict rate,
- duplicate-delivery rate,
- dependency-unavailable rate,
- override-required rate,
- end-to-end time to actionable clerk state.

## Open decisions

- whether product/price data is synchronous, replicated, or hybrid,
- source and semantics of business instant,
- handling of price changes during a transaction,
- manual price authority,
- weighted item representation,
- restricted-product policy,
- late provider response,
- whether attempted scans are durably audited before transaction mutation.

Each requires source, owner, alternatives, and evidence before an Implementation Ready baseline.
