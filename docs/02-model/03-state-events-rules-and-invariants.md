# State, Events, Rules, and Invariants

## State is the bridge from story to system

A narrative says what participants experience. State and rules explain why a path is valid and what changes. Project Builder introduces formalism gradually so a user can begin with an example and later identify the facts underneath it.

## State categories

### Domain state

Facts whose meaning belongs to the modeled reality.

Point-of-sale examples:

- transaction is open,
- transaction contains a priced line,
- coupon is eligible for a line,
- tendered amount,
- balance due,
- sale is completed.

### Application workflow state

Facts required to coordinate a use case.

Examples:

- scan request correlation identifier,
- price lookup attempt count,
- pending authorization operation,
- idempotency record,
- compensation required,
- timeout deadline.

### Presentation state

Facts required to render or operate an interface.

Examples:

- selected transaction tab,
- focused input,
- scanner status indicator,
- coupon dialog open,
- draft manual-entry text,
- currently expanded receipt section.

### Infrastructure state

Mechanism and provider facts.

Examples:

- database connection pool health,
- payment provider endpoint,
- message offset,
- cache entry age,
- printer queue depth.

### External observed state

A fact obtained from another authority that the application does not own.

Examples:

- corporate price book reports item price,
- payment processor reports authorization approved,
- manufacturer coupon service reports campaign active.

The domain may use the observed fact. Infrastructure owns obtaining and translating it. Project Builder must make both parts visible.

## Facts

A fact definition includes:

- name,
- type,
- scope,
- source of authority,
- mutability,
- temporal validity,
- sensitivity,
- possible knowledge states,
- derivation or transition ownership.

Example:

```text
Fact: Transaction.BalanceDue
Type: Money
Scope: Transaction
Authority: Transaction pricing rules
Derived from: Sum(active lines) - applied discounts - accepted tender
Invariant: BalanceDue cannot be negative unless change due is represented separately
```

## Commands and intents

An actor expresses an Intent. The presentation or interface adapter maps it to a Command after surface-level parsing and authentication context are established.

Example:

```text
Intent: Add the scanned thing to the current sale
Command: AddScannedValueToTransaction
Inputs:
  TransactionId
  CapturedValue
  Symbology
  CapturedAt
  Operator
```

Commands are requests, not facts. A command can be rejected, denied, duplicated, or fail before changing domain state.

## Events

An Event states that something relevant occurred.

Examples:

- `ScanCaptured`.
- `ScannedValueClassifiedAsProduct`.
- `StorePriceResolved`.
- `TransactionLineAdded`.
- `ItemAddRejected`.
- `PriceLookupTimedOut`.

Event definitions include:

- meaning,
- source,
- temporal semantics,
- required facts,
- correlation,
- sensitivity,
- consumers,
- retention.

Not every internal method call deserves an event. Events exist when the occurrence has domain, integration, audit, projection, or coordination value.

## Transitions

A transition definition contains:

```text
Name
Scope owner
Source state predicate
Trigger command or event
Preconditions
Rules evaluated
State changes
Events produced
Effects requested
Postconditions
Invariants checked
Semantic results
Alternate and exceptional transitions
```

Example:

```text
Transition: Add priced product line
Source:
  Transaction.Status = Open
Trigger:
  ProductPriceResolved
Preconditions:
  Product is sellable at store
  Price is effective at scan time
  Operator may modify transaction
Change:
  Append active transaction line
  Recalculate merchandise subtotal
  Recalculate balance due
Emit:
  TransactionLineAdded
Ensure:
  Line quantity > 0
  Line extended price = unit price * quantity before discounts
  Transaction remains Open
```

## Rules

Rule kinds:

### Eligibility
Determines whether a subject qualifies.

Example: A coupon is eligible only when campaign, item, store, date, quantity, and prior-redemption conditions hold.

### Classification
Maps input to a domain category.

Example: A captured value is classified as payment, product, coupon, loyalty identifier, special command, or unknown.

### Derivation
Computes a fact from other facts.

Example: Balance due derives from line totals, taxes, discounts, and tender.

### Decision
Chooses among named outcomes based on facts and policy.

Example: Whether an age-restricted item can be sold.

### Calculation
Produces a quantity, amount, score, or schedule.

### Policy
Selects a rule or strategy based on context.

### Validation
Determines whether input can become a domain value.

Rules should be pure functions where practical. External information is obtained before rule evaluation and supplied as explicit facts.

## Invariants

An invariant belongs to the smallest scope that owns the truth.

Point-of-sale examples:

- A completed transaction cannot accept new lines.
- Every active merchandise line has a positive quantity.
- Applied tender plus remaining balance plus change due reconcile to the transaction total.
- A coupon redemption cannot exceed its permitted use count.
- A transaction identifier is unique within its authority.
- A line marked removed does not contribute to totals.

Invariants are not generic validation messages. They define valid state.

## Preconditions and postconditions

A precondition states what must be true before a behavior is valid.

A postcondition states what must be true after a successful transition.

Preconditions that routinely fail under normal operation may be better represented as decision rules with semantic results. For example, "price book is available" is not a domain precondition that callers can guarantee. It is an external condition with a modeled unavailable path.

## Properties

Properties generalize across examples.

Example:

```text
For any valid open transaction and any sellable item with a non-negative price,
adding one unit increases merchandise subtotal by exactly the line extended price,
unless an explicitly modeled promotion changes both values consistently.
```

Property definitions can generate test candidates, but the author or validator must choose generators, bounds, and oracles.

## Temporal semantics

Time can be:

- event occurrence time,
- processing time,
- effective business time,
- deadline,
- expiry,
- duration,
- recurrence,
- ordering relationship.

"Coupon is valid" is incomplete without the relevant time authority and effective window.

The model should distinguish:

```text
CapturedAt
ReceivedAt
ClassifiedAt
PriceEffectiveAt
TransactionBusinessDate
```

## Effects

Domain behavior can request effects without performing provider work:

```csharp
public abstract record Effect
{
    public sealed record ResolveStorePrice(
        StoreId StoreId,
        ProductCode ProductCode,
        Instant EffectiveAt) : Effect;

    public sealed record PrintReceipt(
        TransactionId TransactionId,
        ReceiptDocument Receipt) : Effect;
}
```

Application orchestration interprets effects through ports and adapters, returns facts or results, and continues the modeled flow.

## State tables

A transition table is a useful projection:

| Current state | Trigger | Condition | Next state | Result |
|---|---|---|---|---|
| Open transaction | Product scan captured | Product and price resolved | Open with line | Added |
| Open transaction | Product scan captured | Product unknown | Unchanged | Not found |
| Open transaction | Product scan captured | Price book unavailable | Pending or unchanged per policy | Temporarily unavailable |
| Completed transaction | Product scan captured | Any | Unchanged | Transaction closed |
| Open transaction | Duplicate request | Prior result known | Unchanged | Replay prior result |

## Validation findings

- state changed without a transition,
- transition has no owner,
- command directly contains provider-specific object,
- event name is imperative or future tense,
- invariant is only tested in UI validation,
- domain rule performs network or storage access,
- derived fact has multiple conflicting authorities,
- presentation state is used as a domain precondition,
- transition requests an effect but no failure path exists,
- temporal rule lacks time authority,
- rule depends on implicit global state,
- result conflates denial, invalidity, unavailability, and failure.
