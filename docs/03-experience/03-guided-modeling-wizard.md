# Guided Modeling Wizard

## Concept

The wizard is a context-sensitive **Guidance Rail**. It can be opened from project overview, an element, a finding, or a model stage. It provides a recommended route while preserving studio freedom.

The wizard does not ask the user to complete an enormous upfront questionnaire. It helps establish one coherent interaction, then expands outward and downward.

## First-project flow

### Stage 1: Frame the intent

Prompts:

1. What are you trying to make possible?
2. Who receives value when it works?
3. What situation exists today?
4. What part of the world or organization is in scope?
5. What is explicitly outside this project?
6. Who can authoritatively answer domain questions?

Outputs:

- project purpose,
- outcome,
- initial context,
- source and authority records,
- scope constraints,
- initial gaps.

### Stage 2: Identify participants

Prompts:

1. Who initiates the outcome?
2. Who performs work?
3. Who approves or authorizes it?
4. Who is affected without directly operating the system?
5. Which systems, devices, vendors, or documents participate?
6. Who supports or recovers the process?

Outputs:

- actors,
- systems,
- devices,
- responsibilities,
- authority relations,
- persona candidates.

### Stage 3: Name the episode

Prompt:

> Describe one end-to-end situation in which an actor obtains the desired outcome.

The guide asks for:

- trigger,
- completion signal,
- primary actor,
- major phases,
- known constraints.

Output:

- episode and major scenes.

### Stage 4: Choose one scenario

The system suggests starting with a narrow ordinary example.

For POS:

> A clerk sells one recognized unrestricted item for cash while required services are available.

The guide captures:

- starting facts,
- example data,
- expected outcome,
- assumptions,
- path classification.

### Stage 5: Decompose scenes and interactions

For each scene:

- What changes about setting, responsibility, interface, or boundary?
- Who acts?
- What intent do they express?
- Through what interface?
- What do they observe?
- What meaningful state can change?

The user can enter a simple sentence and refine its structured fields.

### Stage 6: Identify state and rules

The guide derives candidate questions from the interaction:

- What must already be true?
- Which facts are read?
- Which facts can change?
- What must remain true?
- What result categories exist?
- Who owns each rule?
- Is any fact obtained from another authority?

The guide can show a plain-language state table.

### Stage 7: Expand paths

The guide asks only context-relevant failure prompts.

For an external price lookup:

- What if the item is unknown?
- What if the store has no effective price?
- What if the provider is unavailable?
- Can cached data be used?
- Can the clerk retry or enter a price?
- What authority is required for override?
- What state remains after failure?

### Stage 8: Select an interface

The guide asks:

- Who or what will express the intent?
- Is the interaction graphical, command based, API based, event based, device based, document based, or procedural?
- Which state must be visible?
- Which intents are available?
- Which results need distinct feedback?
- What accessibility or environmental constraints apply?

Output:

- interface,
- initial view or contract,
- control-to-intent bindings,
- state exposure map.

### Stage 9: Identify boundaries

The guide evaluates the modeled path:

- Does responsibility leave the current actor, team, or system?
- Does trust change?
- Does a transaction end?
- Does data cross a provider or residency boundary?
- Does work become asynchronous?
- What contract governs the crossing?
- What happens if it fails?

Output:

- boundaries,
- contracts,
- risks,
- decisions,
- ports.

### Stage 10: Plan proof

Prompts:

- Which statements are material claims?
- Who can validate domain truth?
- Which examples should always pass?
- Which properties should hold over many cases?
- Which boundaries need contract tests?
- Which path needs an end-to-end demonstration?
- Which qualities need experiments or rehearsals?

Output:

- evidence requirements,
- acceptance specification,
- readiness findings.

### Stage 11: Prepare implementation

The guide assembles a vertical-slice packet and asks:

- Which architecture decisions are now required?
- Which remain safely open?
- What is the smallest demonstrable behavior?
- What dependencies or test environments are needed?
- What evidence must be returned?

## Prompt anatomy

Each prompt contains:

```text
Question
Why this matters
Current context
Known related facts
Examples
Answer control
Alternative dispositions
Resulting model changes
Validation implications
```

Example:

```text
Question:
What should the clerk see when the scanned product is not found?

Why:
The scenario currently ends at an external lookup failure, but the initiating actor has no observation or recovery action.

Related:
Interaction: Add scanned product
Path: Product not found
Interface: Staffed POS screen

Answer:
[Describe observation]

Actions:
[Retry] [Manual entry] [Request manager] [Remove scan]

Other:
[Unknown] [Not applicable] [Defer]
```

## Adaptive behavior

Prompts are produced by deterministic rules based on:

- selected element kind,
- missing fields,
- relationships,
- purpose profile,
- findings,
- prior answers,
- path and boundary kinds,
- role and permission.

The guide must not infer that a missing answer is "No."

## Wizard map

The user can open a full map:

```text
Frame
  ✓ Purpose
  ✓ Outcome
  ! Scope boundary
Participants
  ✓ Clerk
  ✓ Customer
  ? Support technician
Behavior
  ✓ Episode
  ✓ Happy scenario
  ! Price unavailable path
State and Rules
  ✓ Transaction state
  ! Coupon invariant
Interface
  ✓ POS frame
  ! Keyboard fallback
Systems
  ✓ Price Book boundary
  ? Offline policy
Evidence
  ! Contract test
  ! Recovery rehearsal
```

Statuses link directly to the relevant element or prompt.

## Gap queue

A user can defer a prompt into a Gap. The queue supports:

- owner,
- severity,
- due milestone,
- consequence,
- dependency,
- source,
- next investigation action.

Deferred questions remain part of readiness evaluation.

## Teaching levels

### Guided
Plain language, examples, one question at a time.

### Standard
Grouped questions, model terminology visible, direct editing available.

### Expert
Compact findings and bulk operations, no repetitive explanation, rule codes visible.

The underlying model is identical.

## Agent assistance

Optional assistance can:

- suggest candidate actors or paths,
- transform prose into an uncommitted structured draft,
- explain a finding,
- compare scenario variants,
- draft test examples.

The guide clearly labels generated suggestions, sources them when possible, and requires review before creating a committed change set.

## Wizard acceptance criteria

- A user can complete the full POS item-scan example without opening an advanced inspector.
- A user can jump to studio editing and return at the same prompt.
- Every prompt explains its trigger.
- Every material answer produces inspectable model operations.
- Unknown, Not Applicable, Assumed, and Deferred are available without entering fake content.
- The guide does not trap the user in a linear sequence.
- Closing the rail never loses committed or recovered draft work.
