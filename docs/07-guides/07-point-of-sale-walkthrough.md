# Point-of-Sale Walkthrough

## Purpose

This walkthrough demonstrates the intended Project Builder flow using a retail point-of-sale system. It begins with a clerk's observable outcome and progressively opens the interface, domain, systems, boundaries, implementation, and evidence layers.

The example is illustrative. Real payment, coupon, pricing, tax, and regulated-product behavior varies by organization and jurisdiction and must be sourced rather than inferred.

## 1. Create the project

**Name:** Retail Point of Sale  
**Purpose:** Support accurate, resilient, and auditable retail transactions across store-operated checkout interfaces.  
**Initial outcome:** A clerk can add a sellable product to an active transaction and see the correct store price and updated total.

Select **Discovery**.

Record initial constraints:

- store operations must continue through selected dependency outages,
- financial values require auditable calculation,
- device inputs can be duplicated or malformed,
- accessibility applies to human-operated interfaces,
- corporate systems and store systems can fail independently.

Mark these as Assumed until authoritative sources are attached.

## 2. Identify actors

### Human roles

- Clerk.
- Customer.
- Manager.
- Support Technician.
- Price Administrator.
- Auditor.

### System and device roles

- POS Register.
- Barcode Scanner.
- Corporate Price Book.
- Store Configuration.
- Payment Provider.
- Coupon Clearing Provider.
- Tax Service or Tax Policy.
- Receipt Printer.

For the item-scan slice, the primary initiating actor is the Clerk acting through the Barcode Scanner and POS Register. The Customer is the beneficiary and affected participant. Corporate Price Book is an external system role at this abstraction.

## 3. Define the outcome

```text
Outcome: Product represented in active transaction

Beneficiaries:
- Clerk, who can continue checkout
- Customer, who receives the expected product and price
- Retail organization, which records an accurate sale

Success signals:
- one intended product line exists at the correct quantity
- the applicable store price is shown
- transaction total is recalculated
- any restriction or required action is clearly shown
- audit history identifies the accepted action
```

Do not define "database row inserted" as the outcome.

## 4. Create the episode

**Episode:** Add merchandise to an active transaction.

**Start:** A transaction is active and ready for merchandise input.  
**End:** The attempted input has a semantic result and the clerk can continue, recover, override, or cancel.  
**Primary outcome:** Product represented in active transaction.  
**Participants:** Clerk, Customer, POS Register, input device, product/price authority.

## 5. Create the first scenario

**Scenario:** Known sellable product is scanned.

### Starting facts

- Register 7 is operating for Store 104.
- Clerk is signed in and authorized to add merchandise.
- Transaction T-9001 is active.
- Scanner is associated with Register 7.
- token `012345678905` represents Product P-100.
- Product P-100 is sellable in the current context.
- Corporate/store price authority yields USD 3.49.
- Transaction does not already contain an indivisible duplicate restriction.

### Trigger

Scanner emits token `012345678905`.

### Expected result

One unit of Product P-100 is added at USD 3.49 and the transaction observation is updated.

## 6. Divide the scenario into scenes

### Scene 1: capture merchandise input

- setting: checkout lane,
- responsibility: scanner and POS input adapter,
- interaction: scanner emits token to register,
- observation: interface indicates input received.

### Scene 2: understand the token

- responsibility: token classification,
- intent: determine the handling category,
- result: ProductCode.

### Scene 3: resolve product and price

- responsibility: product and price resolution,
- interaction: request product/store price from price authority,
- result: SellableProductWithPrice.

### Scene 4: attempt transaction change

- responsibility: application and transaction domain,
- intent: add one unit,
- result: ItemAdded.

### Scene 5: present updated transaction

- responsibility: transaction interface,
- observation: new line, quantity, price, total, ready status.

The outer scenario stays readable. Each scene can be opened into a child context.

## 7. Model interactions

### Interaction I-001: capture scanner token

| Field | Value |
|---|---|
| Initiator | Barcode Scanner |
| Receiver | POS Register input adapter |
| Interface | Device signal |
| Intent at outer level | Submit scanned token |
| Input | raw token |
| Authority | scanner must be associated with current register |
| Results | AcceptedForClassification, InvalidSignal, DuplicateSignal |
| Observation | pending indicator or immediate result |
| Boundaries | physical device/protocol |

### Interaction I-002: classify token

| Field | Value |
|---|---|
| Initiator | POS input workflow |
| Receiver | Token Classification Policy |
| Intent | Classify normalized token |
| Results | ProductCode, PaymentToken, CorporateCoupon, ManufacturerCoupon, SpecialCode, Unrecognized |
| Invariant | classification alone does not mutate transaction |
| Evidence | classification examples and property tests |

### Interaction I-003: resolve product and store price

| Field | Value |
|---|---|
| Initiator | Add scanned product use case |
| Receiver | Product/price authority |
| Intent | Resolve product and applicable store price |
| Results | Resolved, UnknownProduct, InactiveProduct, MissingPrice, Unavailable, InvalidResponse |
| Boundary | corporate/store system boundary |
| Properties | latency, availability, freshness, version, fallback |

### Interaction I-004: add transaction line

| Field | Value |
|---|---|
| Initiator | Add scanned product use case |
| Receiver | Active Transaction |
| Intent | Attempt to add one unit at resolved price |
| Results | Added, Prohibited, OverrideRequired, Conflict, InvalidTransactionState |
| State | domain |
| Effects | transaction event and persistence |

### Interaction I-005: show result

| Field | Value |
|---|---|
| Initiator | application observation |
| Receiver | Clerk |
| Interface | graphical POS view |
| Observation | updated line/total or actionable semantic result |
| Accessibility | status announced without stealing focus; keyboard recovery path |

## 8. Separate state

### Domain state

```text
ActiveTransaction
- transaction id
- store id
- status
- lines
- adjustments
- taxes
- total
- version
```

### Application-workflow state

```text
ScanAttempt
- attempt id
- normalized token
- phase
- cancellation state
- correlation
```

The workflow may exist only transiently.

### Presentation state

```text
TransactionViewState
- selected line
- open help panel
- focus target
- expanded detail
- local animation state
```

### Infrastructure state

```text
PriceBookAdapterState
- circuit state
- retry attempt
- last successful synchronization
```

### Externally observed state

```text
PriceAuthorityObservation
- reported version
- availability
- timestamp
```

Only domain state determines transaction truth.

## 9. Define rules

### R-POS-001: token classification precedence

Reserved control and payment patterns are evaluated before general product-code recognition according to the active classification policy.

### R-POS-002: store price selection

A product line uses the price applicable to the transaction's store, channel, business date, product, customer or promotion context, and policy version.

This rule is intentionally broad until the organization supplies authoritative pricing behavior.

### R-POS-003: product eligibility

A product can be added only when active and eligible for sale in the transaction context or when a modeled override authorizes the specific restriction.

### R-POS-004: duplicate signal handling

A repeated device delivery with the same attempt identity must not create an additional transaction line.

This does not prohibit the clerk from intentionally scanning the same product twice as two distinct attempts.

## 10. Define invariants

### INV-POS-001: transaction total

For every committed active transaction state, the total equals the defined composition of line extensions, taxes, discounts, deposits, fees, and adjustments under one policy version.

### INV-POS-002: line price provenance

Every priced transaction line identifies the pricing decision or source version sufficient for audit and later explanation.

### INV-POS-003: atomic add

A successful item-add result commits the line and corresponding total change atomically.

### INV-POS-004: failed attempt isolation

Invalid, unknown, denied, unavailable, cancelled, duplicate, or conflicting attempts do not partially mutate the transaction.

### INV-POS-005: store context

Every product and price decision is evaluated against the active transaction's store context, not merely the register's last known store value.

## 11. Add paths

### Happy: known sellable product

Resolved and added.

### Alternate: known product requires manager override

No line is added. Interface shows restriction and exposes Request Override only to an allowed actor.

### Alternate: token is a coupon

Route to coupon episode. Classification does not mutate transaction.

### Exceptional: unknown product

No line is added. Clerk can retry, search manually, or request support according to policy.

### Degraded: price authority unavailable

Possible organization-specific responses:

- use verified local price snapshot,
- block item add,
- allow controlled manual price,
- queue for later reconciliation.

Project Builder records alternatives and requires an authorized decision. It does not choose.

### Recovery: retry after transient timeout

Retry reuses the attempt identity so a late first response cannot cause two adds.

### Cancellation: clerk cancels pending lookup

Cancellation result is shown. A late provider response is ignored or reconciled according to the use-case contract.

### Conflict: transaction changed concurrently

No overwrite. Refresh state and offer a safe retry.

### Duplicate delivery

Same attempt identity returns prior result or DuplicateIgnored without another line.

## 12. Design the graphical interface

### Primary frame

```text
┌────────────────────────────────────────────────────────────┐
│ Store 104  Register 7  Clerk: A. Rivera        Online      │
├───────────────────────────────────────────────┬────────────┤
│ Transaction                                   │ Summary    │
│                                               │ Subtotal   │
│ Qty  Item                         Price       │ Tax        │
│  1   Example Product              $3.49       │ Total      │
│                                               │            │
├───────────────────────────────────────────────┴────────────┤
│ Status: Ready for next item                                │
├────────────────────────────────────────────────────────────┤
│ Search Item  Remove  Quantity  Override  Help  Checkout    │
└────────────────────────────────────────────────────────────┘
```

### Visible state bindings

- header store/register/clerk from session observation,
- lines and totals from transaction read model,
- status from semantic result/workflow state,
- connectivity from infrastructure observation,
- action availability from authority and current state.

### Result matrix

| Semantic result | Visual response | Next action |
|---|---|---|
| ItemAdded | line and total update; concise status | scan next item |
| UnknownProduct | no line; identify token; explain options | retry, manual search, support |
| OverrideRequired | restriction and reason | authorized override or cancel |
| PriceUnavailable | degraded-state message | retry or approved fallback |
| Conflict | state changed message | refresh and retry |
| DuplicateIgnored | non-alarming status | continue |
| InvalidSignal | device/input guidance | rescan or inspect scanner |
| Cancelled | ready state restored | continue |

The status region uses an appropriate live announcement. Focus remains at the operational scan context unless an action is required.

## 13. Model the external boundary

### Corporate Price Book boundary

Record:

- ownership: corporate merchandising/pricing,
- interface: API, event-distributed local snapshot, or both,
- contract version,
- product and store identifiers,
- price and eligibility data,
- freshness,
- latency and timeout,
- availability,
- consistency,
- authorization,
- audit,
- fallback,
- vendor or internal ownership,
- recovery and reconciliation.

An architecture decision compares live lookup, local replicated read model, and hybrid strategies against these properties.

## 14. Project the implementation slice

### Presentation

- scanner device adapter,
- POS transaction component,
- semantic-result renderer,
- manager-override interaction.

### Application

`AttemptScannedToken` use case:

1. authorize clerk/session,
2. validate register and transaction context,
3. establish attempt/idempotency identity,
4. normalize and classify token,
5. resolve product and price through port,
6. invoke transaction domain behavior,
7. commit with expected version,
8. produce application observation,
9. publish allowed effects.

### Domain

- ProductCode.
- ActiveTransaction.
- TransactionLine.
- StorePrice.
- TokenClassification.
- SaleEligibility.
- AddLineDecision.
- transaction invariants.
- semantic results.

### Infrastructure

- scanner protocol adapter if server-side,
- PostgreSQL transaction store,
- corporate price-book adapter or local projection,
- outbox/event publisher,
- clock and identity implementations.

### Contracts

- scanner signal envelope,
- price-book request/response or replicated event,
- application API intent/result,
- transaction observation,
- audit event.

## 15. Define evidence

| Claim | Evidence |
|---|---|
| valid product is added at store price | domain example, application integration, E2E scenario |
| classification does not mutate transaction | property test |
| duplicate delivery cannot double add | idempotency and concurrency integration test |
| failed attempts do not partially mutate | transaction integration and property tests |
| stale version cannot overwrite | PostgreSQL concurrency test |
| every result is represented | compile/analyzer plus component matrix test |
| clerk can recover without pointer | keyboard browser scenario |
| price-book timeout degrades as decided | adapter failure injection and E2E |
| line price is auditable | persistence/contract test |
| telemetry diagnoses latency without sensitive token logging | observability integration review |

## 16. Review purpose profiles

### Discovery

Expected to require:

- actors,
- outcome,
- episode,
- at least one scenario,
- knowledge states and sources,
- material failure paths.

### Interface Design

Also requires:

- visible state,
- intents and semantic results,
- interface states,
- accessibility constraints,
- scenario mapping.

### Architecture

Also requires:

- systems and boundaries,
- contracts,
- operational properties,
- decisions and risks.

### Implementation Ready

Also requires:

- precise state transitions,
- invariants,
- result exhaustiveness,
- vertical slice,
- migration/concurrency/idempotency,
- evidence plan.

### Release Ready

Also requires:

- produced and current evidence,
- approved baseline,
- operational readiness,
- accepted risks,
- trace to deployed version.

## 17. Commit and baseline

Commit the scenario model with a reason such as:

> Define known-product scan and material failure paths for POS item-add reference slice.

After review, create an Implementation Ready baseline only when unresolved findings are non-blocking or explicitly accepted.

## 18. Continue the POS model

Next episodes can include:

- manual item entry,
- quantity change,
- item removal,
- manufacturer coupon,
- corporate coupon,
- age-restricted sale,
- cash payment,
- card payment,
- QR payment,
- mixed tender,
- refund,
- void,
- suspended transaction,
- offline operation,
- receipt delivery,
- end-of-day reconciliation.

Each begins again at actor outcome. Do not copy implementation structure into every episode unless shared domain concepts or interfaces are genuinely reused.
