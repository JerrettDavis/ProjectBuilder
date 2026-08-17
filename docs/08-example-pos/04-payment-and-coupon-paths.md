# Payment and Coupon Paths

## Purpose

Payments and coupons demonstrate that a top-down model must account for external authority, temporal uncertainty, idempotency, compensation, and user-visible ambiguity. This document intentionally stops short of claiming one universal retail policy.

## Manufacturer coupon episode

### Outcome

An eligible manufacturer coupon reduces the active transaction exactly once, and the adjustment can be explained and settled.

### Actors

- Customer presents coupon.
- Clerk submits coupon.
- Transaction evaluates local eligibility.
- Coupon Authority may validate or settle.
- Manager may review according to policy.
- Auditor inspects provenance.

### State

```text
CouponCandidate
- code
- source
- effective period
- policy/version
- required products/quantities
- usage limits

AppliedCoupon
- identity
- covered transaction lines
- adjustment
- validation provenance
- settlement status
```

### Invariants

- one coupon instance cannot apply more than once,
- adjustment cannot exceed the policy-defined limit,
- required eligible products remain represented while coupon is applied,
- removing covered products triggers re-evaluation,
- failed or denied coupon does not change financial totals,
- applied adjustment is attributable to policy and actor.

### Paths

#### Happy: valid eligible coupon

1. token is classified as ManufacturerCoupon,
2. policy and coupon identity are resolved,
3. transaction facts satisfy eligibility,
4. coupon adjustment is decided,
5. transaction commits coupon and new totals atomically,
6. interface shows adjustment and explanation,
7. clearing effect is scheduled if required.

#### Denied: required product absent

No mutation. Interface explains the missing eligibility condition without exposing unnecessary internal detail.

#### Conflict: another promotion excludes it

The rule engine returns a typed `PromotionConflict` with allowed choices or no allowable combination.

#### Degraded: authority unavailable

Possible policies:

- do not accept,
- accept below controlled risk threshold,
- queue for later validation,
- require manager approval.

The organization chooses and records reconciliation behavior.

#### Compensation: clearing later rejects

The system needs a modeled business response. Technical retry alone cannot decide whether to absorb loss, contact store, adjust accounting, or create an exception case.

## Corporate coupon episode

Corporate coupons may be locally authoritative and can involve:

- customer segment,
- campaign,
- store/channel,
- effective window,
- usage limit,
- product basket,
- combinability,
- policy version.

The same code pattern can mean different campaigns over time. Identity includes campaign and version, not only displayed token.

### Rules

- eligibility is evaluated against a stable transaction snapshot or explicit re-evaluation policy,
- application ordering among promotions is deterministic,
- removal or quantity change re-evaluates dependent adjustments,
- audit can explain which rules applied and why alternatives did not.

### Evidence

- decision tables,
- combinatorial properties,
- golden examples from pricing/marketing,
- policy-version compatibility,
- concurrency test for transaction changes,
- UI result and explanation states,
- settlement/reconciliation integration evidence.

## Card payment episode

### Outcome

A customer-authorized card payment settles the intended amount exactly once, and uncertain provider status is resolved without duplicate capture.

### Actors and interfaces

- Clerk through POS.
- Customer through payment terminal.
- POS Payment Application.
- Terminal/device.
- Payment Provider.
- Store/financial reconciliation.
- Support and audit.

### Payment intent state

```text
PaymentAttempt
- AttemptId
- TransactionId
- TransactionVersion
- Amount
- Currency
- ProviderRoute
- Status
- ProviderReference?
- CreatedAt
- LastObservedAt
```

Possible statuses:

```text
Created
AwaitingCustomer
Submitted
Authorized
Captured
Declined
Cancelled
Unknown
ReversalPending
Reversed
Failed
```

These are application/domain distinctions that require ownership decisions. Provider statuses are mapped, not copied blindly.

### Core invariants

- amount and currency cannot change within one attempt,
- one attempt cannot produce more than one net capture,
- a completed transaction cannot be marked paid without accepted tender state,
- unknown provider status is not equivalent to decline or failure,
- retry with same idempotency identity cannot create a second capture,
- sensitive account data is not stored outside the approved boundary,
- local and provider references remain correlatable for reconciliation.

### Happy path

1. Clerk initiates card tender for current amount due.
2. POS creates durable payment attempt.
3. Terminal displays amount and obtains customer action.
4. Provider authorizes/captures according to contract.
5. application observes definitive success.
6. transaction records accepted tender and reaches paid or partially paid state atomically with local payment record.
7. interface shows success.
8. receipt and downstream effects proceed.

### Decline

- provider reports definitive decline,
- no accepted tender is recorded,
- customer-facing terminal and clerk interface show appropriate next action,
- sensitive decline details are handled according to contract,
- alternate tender can begin with a new attempt.

### Customer cancellation

Cancellation can occur:

- before submission,
- while awaiting customer input,
- after provider submission but before definitive local status.

Each timing has different semantics. A cancellation request is not proof that no authorization occurred.

### Timeout and unknown status

The provider call times out. The system records `Unknown`, not `Failed`.

Recovery can include:

- status query by attempt/provider reference,
- provider idempotent replay,
- asynchronous callback,
- reconciliation queue,
- operator guidance that prevents immediate duplicate tender.

The interface must distinguish "payment declined" from "payment status unknown."

### Local commit failure after provider success

This is a distributed consistency problem. Options include:

- durable local attempt before provider call plus status recovery,
- outbox/inbox and provider status reconciliation,
- compensating reversal,
- controlled pending-payment state.

An ADR selects the strategy. The model records the financial invariant and observable behavior first.

### Duplicate callback

Provider callbacks carry deduplication identity. Reprocessing produces the same accepted result without a second tender or transaction transition.

## QR payment episode

### Distinct interactions

1. POS creates payment intent.
2. QR code communicates provider/intent reference.
3. Customer uses external device/application.
4. Provider reports status by callback, polling, or both.
5. POS resolves status and updates transaction.

### Additional risks

- displayed code expires,
- customer scans stale code,
- callback belongs to another transaction,
- callback and poll race,
- customer pays wrong amount,
- status changes after local timeout,
- malicious replay.

### Invariants

- QR intent binds transaction, amount, currency, and expiration,
- only trusted provider observations can settle it,
- duplicate observation is idempotent,
- stale or mismatched intent cannot settle current transaction,
- cancellation and late success have a defined reconciliation path.

## Cash payment episode

Cash has no remote provider but still has domain and operational concerns.

### State

- amount due,
- amount tendered,
- change due,
- rounding policy,
- drawer state,
- clerk/session,
- audit.

### Rules

- cash amount parses under currency denomination policy,
- change is derived deterministically,
- over/under thresholds and limits are policy-owned,
- drawer opening is an effect, not proof of payment,
- completion and drawer audit have explicit order.

### Failure paths

- drawer does not open,
- clerk entered wrong tender before commit,
- transaction changes before tender commit,
- change cannot be represented under denomination or rounding policy,
- cash acceptance is restricted,
- receipt fails after payment.

## Mixed tender

Mixed tender turns the transaction into a sequence of partial settlements.

Questions:

- Is each tender committed independently?
- Can earlier tender be reversed when later tender fails?
- What is the cancellation boundary?
- How are partial authorizations represented?
- Can amount due change after first tender?
- What happens when a coupon or item changes?
- Which actor can abandon a partially paid transaction?
- What does offline recovery do?

This likely merits its own aggregate or saga-like application model. The decision must follow invariants and provider contracts.

## Cross-cutting result vocabulary

```text
Accepted
PartiallyAccepted
Declined
Invalid
Ineligible
Expired
AlreadyApplied
Conflict
Cancelled
UnknownStatus
Unavailable
TimedOut
DuplicateIgnored
ReversalPending
Reversed
ReconciliationRequired
Failed
```

Each episode chooses a narrower exhaustive union. Do not expose one untyped status enum across all tender and discount behavior.

## Interface requirements

For payment and coupon flows, show:

- amount or adjustment,
- current phase,
- who must act,
- cancellation availability,
- timeout and unknown status,
- safe retry instruction,
- provider or policy explanation appropriate to role,
- receipt/audit reference when final,
- accessibility announcement,
- privacy-safe support reference.

Never imply payment success before definitive accepted state.

## Evidence strategy

### Domain

- amount and adjustment properties,
- invariant preservation,
- promotion decision tables,
- mixed-tender state transitions.

### Application

- idempotency,
- unknown status,
- cancellation race,
- reconciliation scheduling,
- authorization and override.

### Provider contract

- request mapping,
- idempotency,
- callback authentication,
- status mapping,
- duplicate and out-of-order observations,
- sandbox and recorded fixtures.

### Persistence

- unique attempt constraints,
- transaction/payment atomicity,
- outbox/inbox,
- concurrent callbacks,
- migration.

### End to end

- terminal simulator,
- provider timeout,
- late callback,
- duplicate callback,
- UI unknown-state behavior,
- keyboard cancellation and recovery,
- safe logs and traces.

### Operational

- reconciliation rehearsal,
- provider outage procedure,
- key/certificate rotation,
- alerting on unknown attempts,
- audit and support workflow.
