# Narrative Hierarchy

## Why a narrative hierarchy exists

People commonly understand systems as stories before they understand them as state machines or component graphs. Project Builder uses a narrative spine to preserve purpose and causality as the model becomes more formal.

The hierarchy is a guide, not a mandatory waterfall:

`Project → Context → Capability → Episode → Scenario → Scene → Interaction → Step`

## Project

A Project establishes the universe of discourse and modeling purpose.

Example:

> Model a point-of-sale system for convenience-store transactions, beginning with staffed checkout and preserving future self-checkout compatibility.

A Project should answer:

- Why does this model exist?
- What outcome is sought?
- Which reality is in scope?
- Which reality is explicitly out of scope?
- Who owns the model?
- What kind of decision will the model support?

## Context

A Context is a scope in which terms and rules have a stable meaning.

Point-of-sale examples:

- Store Sales.
- Corporate Pricing.
- Payment Acceptance.
- Coupon Adjudication.
- Inventory.
- Support and Maintenance.

"Item" can mean a sellable catalog entry in Store Sales and a counted physical unit in Inventory. The contexts should not be forced to share a vague object merely because the word is the same.

## Capability

A Capability describes an ability without prescribing the workflow or system.

Examples:

- Identify a sellable item.
- Determine the current price.
- Add merchandise to a transaction.
- Accept tender.
- Validate a coupon.
- Suspend and resume a transaction.
- Reconcile cash.

Capabilities help prevent the model from turning every observed workflow into permanent structure.

## Episode

An Episode is an end-to-end span of activity that produces a meaningful outcome.

Example:

> Complete a staffed retail sale.

Episode fields:

- beneficiary,
- desired outcome,
- initiating situation,
- primary actors,
- contexts involved,
- completion criteria,
- major risks,
- source authority,
- related capabilities.

Episodes can cross systems, interfaces, teams, and time. They are usually too broad to implement as one slice.

## Scenario

A Scenario is a concrete path through an episode under stated conditions.

Example:

> A clerk sells one recognized, unrestricted item for cash when the price book is available and the customer provides exact tender.

Scenario fields:

- path classification,
- trigger,
- starting facts,
- assumptions,
- participants,
- ordered scenes,
- expected result,
- final observable state,
- unresolved questions,
- evidence expectations.

Scenarios should be specific enough to reason about, but not encode incidental data in the title. Concrete examples belong in example sets.

### Scenario outline versus example

Scenario outline:

> Sell a recognized item for cash.

Example:

- Store 1042.
- UPC `012345678905`.
- Price `$2.49`.
- Cash received `$3.00`.
- Change `$0.51`.

The outline defines behavior. Examples validate and teach it.

## Scene

A Scene segments a scenario when a meaningful dimension changes:

- primary responsibility,
- interface,
- physical or logical setting,
- system boundary,
- transaction boundary,
- time horizon,
- dominant participant.

Example scenes:

1. Start transaction.
2. Capture item identifier.
3. Classify scanned value.
4. Resolve item and store price.
5. Add line to transaction.
6. Present updated total.
7. Accept cash.
8. Complete and print receipt.

A scene should not be created merely because the author wants another box. It should help explain responsibility, state, or boundary.

## Interaction

An Interaction describes an observable exchange.

Example:

> Clerk scans an item barcode through the scanner; the POS acknowledges capture and begins classification.

Required questions:

- Who initiates?
- What intent is expressed?
- Through which interface?
- Who or what receives it?
- What information is supplied?
- What authority is needed?
- What immediate observation is expected?
- What domain behavior can follow?
- What can fail at the interaction boundary?

An interaction does not imply synchronous software. A customer mailing a form, a scheduler publishing an event, and a service requesting an API are all interactions.

## Step

A Step is the smallest ordered unit needed to make the interaction understandable.

Example steps for barcode capture:

1. Scanner detects a complete symbol.
2. Scanner decodes the symbol into characters and symbology metadata.
3. Device interface submits the captured value.
4. POS validates format and records scan receipt.
5. POS presents acknowledgment or error.

These steps may later project into several software components, but the narrative does not begin with methods.

## Reuse and reference

A scenario can reuse a scene or interaction definition through a typed reference. Reuse should preserve meaning:

- A referenced interaction keeps its identity.
- Scenario-specific conditions live on the relationship or a scenario binding.
- The shared definition cannot be changed silently for one scenario.
- Forking creates a new element with provenance.

## Drilldown

Every level can be opened.

At a high level:

> Resolve item and store price.

At the next level:

1. Classify scanned value.
2. Parse UPC.
3. request store-specific price.
4. handle item not found.
5. handle price book unavailable.
6. return priced item.

At an implementation level:

- Presentation adapter receives scan.
- Application handler establishes correlation and idempotency.
- Domain classifier identifies identifier kind.
- Price Book port resolves sellable item.
- Transaction aggregate evaluates add-line rules.
- UI projection returns updated view state.
- Evidence verifies expected and failure behavior.

Drilldown creates contained or related model elements. It does not replace the parent description. The parent remains a valid abstraction and states the outcome expected from its children.

## Scenario grammar

A readable scenario can be rendered as:

```text
Given <starting facts and conditions>
And <participant authority and environment>
When <actor expresses intent through interface>
Then <observable outcome>
And <domain postconditions>
But <invariants remain true>
Otherwise <named alternate or exceptional path>
Evidence <required proof>
```

The product can generate this grammar from structured data. The author should not have to write syntax correctly to model behavior.

## Narrative validation

Findings include:

- episode has no beneficiary or completion signal,
- scenario has no trigger,
- scenario outcome does not contribute to episode outcome,
- scene changes no meaningful dimension,
- interaction has no initiator or receiver,
- intent is phrased as implementation rather than desired effect,
- observation is not visible to any participant,
- scenario references an actor outside its context without a relationship,
- path ends without semantic result,
- child behavior does not satisfy parent abstraction,
- shared interaction is overridden inconsistently.

## Naming guidance

Prefer active, domain-recognizable names:

- "Add priced item to transaction."
- "Reject expired manufacturer coupon."
- "Request card authorization."
- "Record cash tender."

Avoid vague names:

- "Process data."
- "Handle item."
- "Call API."
- "Update database."
- "Do validation."

The product can flag implementation-centric or vague verbs as a suggestion, not an automatic rewrite.
