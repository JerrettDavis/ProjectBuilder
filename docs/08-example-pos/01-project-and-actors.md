# POS Project, Outcomes, Actors, and Authority

## Project definition

**Project:** Retail Point of Sale  
**Alias:** POS-PROJECT  
**Purpose:** Define store checkout behavior so that transactions are accurate, resilient, understandable to operators, auditable, and implementable through explicit vertical slices.

## Scope

### Included

- clerk-operated store checkout,
- active sales transaction,
- merchandise entry,
- coupons,
- selected payment forms,
- manager override,
- store/corporate system interactions,
- core failure, degradation, recovery, and audit behavior.

### Excluded from the first baseline

- full inventory management,
- procurement,
- workforce scheduling,
- e-commerce checkout,
- accounting ledger implementation,
- complete tax-jurisdiction rules,
- complete chargeback/dispute lifecycle,
- loyalty program beyond interfaces needed by modeled scenarios,
- every hardware protocol.

## Outcomes

### POS-OUT-001: accurate item representation

A clerk and customer can see that the intended product, quantity, and applicable price are represented exactly once in the active transaction.

Success signals:

- correct product identity,
- correct quantity,
- price provenance,
- total recalculation,
- understandable restriction or failure state,
- no unintended duplicate mutation.

### POS-OUT-002: valid discount representation

An eligible coupon or promotion changes the transaction according to its policy and the adjustment is explainable.

### POS-OUT-003: authorized settlement

The customer provides an accepted tender and the transaction reaches the correct paid state without duplicate capture.

### POS-OUT-004: operational continuity

Store staff can continue, degrade safely, or receive actionable guidance during selected dependency and device failures.

### POS-OUT-005: auditability

Material transaction decisions, overrides, pricing, discounts, and payments can be reconstructed from durable records and versioned policy provenance.

### POS-OUT-006: accessible operation

The clerk can understand state and perform all essential actions through supported keyboard and assistive-technology paths without pointer-only dependence.

## Actor catalog

### POS-ACT-001: Clerk

**Kind:** HumanRole  
**Goals:**

- process the customer's intended purchase accurately,
- understand the next required action,
- recover from ordinary problems quickly.

**Responsibilities:**

- initiate merchandise, coupon, and tender interactions,
- verify prompts that require human observation,
- request authorized assistance,
- avoid bypassing controls.

**Authority:**

- create or operate an active transaction while signed in,
- add ordinary merchandise,
- remove or change quantity within policy,
- accept permitted tenders,
- not automatically authorized for restricted overrides.

**Constraints:**

- high-throughput environment,
- divided attention,
- device noise,
- varying training and accessibility needs.

### POS-ACT-002: Customer

**Kind:** HumanRole  
**Goals:**

- receive intended products and prices,
- apply eligible discounts,
- pay with an accepted tender,
- receive proof of purchase.

**Authority:**

- present items, coupons, and tender,
- approve or cancel selected payment interactions,
- not directly mutate store transaction state.

### POS-ACT-003: Manager

**Kind:** HumanRole  
**Goals:**

- resolve exceptions while enforcing policy,
- reduce loss and improper denial,
- support clerk continuity.

**Authority:**

- approve specifically modeled overrides,
- void or authorize selected transaction changes,
- review reasons and evidence.

Authority must be scoped by action, store, session, and policy. "Manager" is not a universal bypass.

### POS-ACT-004: Support Technician

**Kind:** HumanRole  
**Goals:**

- restore devices and services,
- diagnose incidents without exposing prohibited data,
- preserve transaction integrity.

**Authority:**

- inspect operational health and safe diagnostics,
- not silently alter financial transaction truth.

### POS-ACT-005: Price Administrator

**Kind:** OrganizationRole  
**Goals:**

- publish correct products, prices, and effective periods,
- manage corrections and policy versions.

**Authority:**

- modify corporate/store price authority through governed process,
- not directly edit a committed store transaction without a separate correction process.

### POS-ACT-006: Auditor

**Kind:** OrganizationRole  
**Goals:**

- verify that financial and policy behavior can be explained,
- identify unauthorized or anomalous behavior.

**Authority:**

- read approved audit evidence within scope,
- not operate checkout by virtue of audit access.

### POS-ACT-010: POS Register

**Kind:** SystemRole  
**Role:** Coordinate checkout interface and application behavior for one lane or operating session.

The register is not the business beneficiary. It participates on behalf of human actors and the retail organization.

### POS-ACT-011: Barcode Scanner

**Kind:** DeviceRole  
**Role:** Capture and emit physical token observations.

The scanner observes a token. It does not determine that the token is a product or authorize a transaction change.

### POS-ACT-012: Corporate Price Authority

**Kind:** ExternalProviderRole or SystemRole according to ownership  
**Role:** Provide product and pricing observations and policy provenance for a store context.

### POS-ACT-013: Payment Provider

**Kind:** ExternalProviderRole  
**Role:** Authorize, capture, cancel, or report status for selected electronic tenders.

### POS-ACT-014: Coupon Authority

**Kind:** ExternalProviderRole or SystemRole  
**Role:** Validate or settle selected coupon types.

### POS-ACT-015: Receipt Device/Service

**Kind:** DeviceRole or SystemRole  
**Role:** Produce a physical or electronic receipt observation.

### POS-ACT-016: Store Operations

**Kind:** OrganizationRole  
**Role:** Own store-level operating policy, continuity, and escalation.

## Authority matrix

| Intent | Clerk | Manager | Customer | Support | System/Provider |
|---|---:|---:|---:|---:|---:|
| Start transaction | conditional | conditional | no | no | coordinate |
| Add ordinary item | yes | yes | present only | no | decide/coordinate |
| Override restricted sale | request | conditional approve | no | no | enforce |
| Apply coupon | submit | conditional override | present | no | validate |
| Initiate card payment | yes | yes | approve at device | no | authorize |
| Cancel pending card payment | conditional | yes | conditional at device | no | cancel |
| Manually set price | policy-dependent | policy-dependent | no | no | audit |
| View transaction audit | limited | limited | own receipt | diagnostic only | record |
| Change corporate price | no | no | no | no | Price Administrator through governed system |
| Restore device | basic retry | basic retry | no | yes | diagnostics |

Every "conditional" cell requires an explicit rule or remains Unknown.

## Personas versus actors

A persona can enrich the Clerk actor:

- new clerk with limited training,
- experienced clerk using keyboard-first flow,
- clerk with low vision,
- temporary clerk during seasonal peak.

Personas influence interface design and training. They do not redefine business authority unless a policy explicitly does.

## Initial constraints

| Alias | Constraint | Status | Owner/source needed |
|---|---|---|---|
| POS-CST-001 | transaction money calculations use one explicit currency and rounding policy | Assumed | Finance |
| POS-CST-002 | store can continue selected item entry during corporate connectivity loss | Assumed | Store Operations |
| POS-CST-003 | electronic payment capture must be idempotent | Decision candidate | Payments |
| POS-CST-004 | sensitive payment data must not enter general application logs | Decision candidate | Security/Payments |
| POS-CST-005 | essential clerk operation has keyboard alternative | Decision | Product/Accessibility |
| POS-CST-006 | every override records actor, reason, policy, and affected transaction | Assumed | Loss Prevention/Audit |
| POS-CST-007 | device signals may be duplicated, delayed, or malformed | Known engineering reality | Device contract |
| POS-CST-008 | project example uses synthetic identifiers and values | Decision | Project Builder team |

## Capability map

### POS-CAP-001: manage checkout session

- establish store, register, clerk, permissions, and operating context.

### POS-CAP-010: enter merchandise

- scan,
- manual search/entry,
- quantity,
- remove/void,
- restricted merchandise,
- unknown product.

### POS-CAP-020: apply price and discount policy

- store price,
- promotions,
- manufacturer coupon,
- corporate coupon,
- manual adjustments.

### POS-CAP-030: accept tender

- cash,
- card,
- QR,
- mixed tender,
- cancellation,
- reversal.

### POS-CAP-040: complete and evidence transaction

- receipt,
- audit,
- downstream publication,
- reconciliation.

### POS-CAP-050: operate through degradation

- local data,
- retry,
- offline policy,
- support,
- recovery and reconciliation.

## Stakeholder tensions

Project Builder should preserve these as competing outcomes rather than flattening them:

- checkout speed versus control,
- customer convenience versus fraud prevention,
- local continuity versus central consistency,
- concise prompts versus detailed explanation,
- privacy versus diagnostic depth,
- strict policy versus authorized exception,
- immediate receipt versus downstream settlement finality.

Architectural and interface decisions must state which tension they address.
