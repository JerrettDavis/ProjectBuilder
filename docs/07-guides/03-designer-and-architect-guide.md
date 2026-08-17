# Designer and Architect Guide

## Purpose

Project Builder joins interaction design and system architecture through shared behavior. Designers can shape what actors perceive and do. Architects can shape how responsibilities, boundaries, contracts, and operational properties support that behavior. Neither discipline owns a separate truth.

The recommended order is:

1. actor outcome,
2. scenario and state,
3. interface behavior,
4. internal decomposition,
5. boundaries and contracts,
6. architectural decisions,
7. evidence.

## Working from behavior, not screens or boxes

A screen-first approach often hides:

- what triggered the view,
- whose authority is represented,
- which state is business truth,
- which outcomes must be communicated,
- what happens when dependencies are slow or unavailable,
- whether a device or API is part of the interaction.

A system-box-first approach often hides:

- what actor outcome the box supports,
- why messages exist,
- which consistency or latency is actually required,
- what users observe during failure,
- where policy belongs.

Start with a scenario. Use interface and architecture lenses to reveal different responsibilities in the same sequence.

## Interface modeling workflow

### 1. Select the actor and scenario

Choose the scenario path and the interface through which the actor participates.

For the POS scan:

- actor: Clerk,
- device participant: Scanner,
- graphical interface: Register transaction view,
- intended outcome: item is correctly represented in the active transaction.

### 2. Identify visible state

Create a visible-state inventory:

| Visible concept | Source | Freshness | Empty state | Error state |
|---|---|---|---|---|
| Transaction lines | transaction read model | immediate after accepted change | no items | stale/refresh |
| Running total | derived transaction value | same revision as lines | zero | unavailable |
| Scan status | application workflow | transient | ready | invalid/offline |
| Required action | semantic result | until acknowledged | none | escalation |
| Connectivity | infrastructure observation | sampled | unknown | disconnected |

Visible state is not automatically domain state. Link it to domain facts, workflow state, or infrastructure observations explicitly.

### 3. Define intents before controls

Intents are stable meanings such as:

- AttemptAddScannedToken.
- RemoveTransactionLine.
- RequestManagerOverride.
- RetryPriceLookup.
- CancelPendingScan.

Controls and device signals are interface-specific affordances for those intents.

A button labeled "Add" and a scanner signal may invoke the same application intent through different adapters. Do not bind controls directly to database updates or domain object mutation.

### 4. Define semantic results

The interface must represent the results the application can produce:

- ItemAdded.
- TokenRequiresAlternateHandling.
- UnknownItem.
- SaleProhibited.
- OverrideRequired.
- DependencyUnavailable.
- Conflict.
- DuplicateIgnored.
- InvalidInput.
- Cancelled.

Map each result to:

- visible message,
- state update,
- focus behavior,
- next allowed intents,
- persistence of the message,
- accessibility announcement,
- telemetry and audit where appropriate.

### 5. Design all important interface states

At minimum consider:

- initial,
- loading or pending,
- empty,
- valid input,
- invalid input,
- denied,
- success,
- partial,
- failed,
- degraded,
- stale,
- offline,
- conflict,
- cancellation,
- recovery.

Do not use one generic red toast for every semantic outcome.

### 6. Bind a scenario to the interface

Use the scenario player to step through:

1. initial facts and visible state,
2. input or actor action,
3. immediate local feedback,
4. application intent,
5. validation and authorization,
6. domain decision,
7. external effects,
8. result,
9. read-model update,
10. visible and announced response.

This exposes gaps such as a semantic result with no representation or a control that has no authorized intent.

## Designing non-graphical interfaces

### CLI

Model:

- command grammar,
- options and arguments,
- stdin,
- stdout and stderr,
- exit codes,
- interactive prompts,
- idempotency and scripting behavior,
- human and automation actors.

### HTTP or RPC

Model:

- operation intent,
- resource or method,
- request and response schema,
- status and problem types,
- authentication and authorization,
- idempotency,
- concurrency,
- pagination and consistency,
- versioning,
- latency and limits.

### Event or message

Model:

- event meaning,
- producer authority,
- schema,
- partition or ordering needs,
- delivery semantics,
- deduplication,
- consumer expectations,
- retention,
- evolution,
- dead-letter and replay behavior.

### MCP

Model:

- server and client actors,
- tools, resources, and prompts,
- input and output schemas,
- capability discovery,
- authorization and consent,
- side-effect classification,
- error and cancellation behavior,
- provenance and audit,
- human approval points.

### Device

Model:

- physical signal,
- protocol,
- sampling or debounce,
- calibration,
- device identity,
- loss, duplication, and ordering,
- operator feedback,
- safety behavior,
- replacement and maintenance.

### Document or human procedure

Model:

- fields or steps,
- responsible role,
- handoff,
- validation,
- signatures or approval,
- time expectations,
- exception and escalation,
- retention and evidence.

## Architecture decomposition workflow

### 1. Mark the outer contract

The outer scenario should remain understandable:

> When the scanner emits a token during an active transaction, the POS attempts the correct handling and shows the result.

### 2. Open a child context

Decompose only what needs more detail:

- Capture Scanner Signal.
- Normalize Token.
- Classify Token.
- Resolve Product and Store Price.
- Evaluate Sale Eligibility.
- Add Transaction Line.
- Publish Updated Transaction Observation.

Each can contain actors, state, interactions, paths, and evidence.

### 3. Assign responsibility

Use the four responsibility lenses carefully.

#### Domain

Owns truths and decisions intrinsic to the modeled business reality:

- transaction line and totals,
- product identity and sellability concepts,
- price and discount policy,
- sale eligibility,
- invariants.

Domain is not "anything important." An external provider response is an observation until interpreted through domain concepts.

#### Application

Owns the customized coordination of one use case:

- authorize intent,
- load needed state,
- invoke domain behavior,
- coordinate ports,
- manage transaction intent,
- map semantic results,
- publish or schedule effects.

Application is not a generic service layer.

#### Infrastructure

Owns mechanisms external to the logic being built:

- PostgreSQL,
- corporate price-book client,
- scanner driver,
- message broker,
- filesystem,
- identity provider,
- clock, network, object storage.

Infrastructure implements ports and translates provider details.

#### Presentation

Owns interaction translation:

- UI,
- HTTP,
- CLI,
- event consumer,
- MCP endpoint,
- device adapter,
- human procedure adapter.

Presentation turns external input into an application intent and application observations into interface behavior.

### 4. Mark boundaries

Boundary types can overlap.

| Boundary | Design question |
|---|---|
| Ownership | Who can change and support this? |
| Trust | What input or identity cannot be trusted? |
| Transaction | What must be atomic? |
| Process | What can fail independently? |
| Deployment | What can be released separately? |
| Protocol | What translation and versioning exist? |
| Vendor | What cannot be controlled and how can it be replaced? |
| Data residency | Where may data exist? |
| Failure domain | What becomes unavailable together? |
| Human handoff | What context and authority must transfer? |

### 5. Attach properties to crossings

Only add properties that affect design or validation:

- expected latency and timeout,
- availability,
- consistency,
- ordering,
- throughput,
- data classification,
- authentication,
- authorization,
- idempotency,
- retry,
- recovery,
- audit,
- retention,
- version compatibility,
- cost.

Avoid inventing numerical targets without source or status. Mark estimates as Assumed.

### 6. Select architecture through decisions

An architectural decision includes:

- context and forcing functions,
- considered options,
- selected option,
- rationale,
- consequences,
- risks,
- validation plan,
- review trigger,
- affected model elements.

Example:

> Use a modular monolith for the initial product because behavior and module boundaries are still evolving, strong in-process consistency is useful, and independent deployment has no measured requirement.

The decision is separate from the invariant and from the deployment diagram.

## Vertical slicing

A slice follows one outcome through all needed responsibilities. For item scan:

```text
Scanner signal
  -> Presentation device adapter
  -> Application AttemptScannedToken use case
  -> Domain token classification
  -> Infrastructure price-book port when ProductCode
  -> Domain eligibility and AddItem behavior
  -> Application semantic result
  -> Presentation transaction view update
  -> Evidence across rule, adapter, persistence, and browser
```

Do not split delivery into "all entities," "all repositories," "all services," and "all screens." That sequence delays the first coherent proof and encourages speculative abstractions.

## Architecture review questions

### Behavior

- Which actor outcome does each component support?
- Can every message or API operation be traced to an intent?
- Are semantic results visible to the initiating actor?

### State and consistency

- Who owns each fact?
- What is the consistency requirement and source?
- Which invariant defines the transaction boundary?
- What happens on stale data or concurrent change?

### Boundaries

- Where is trust established?
- Which provider types leak across the boundary?
- How are version and compatibility managed?
- What is the degraded behavior?

### Operations

- What does healthy mean?
- Which metric predicts user harm?
- Can a failure be diagnosed through traces and safe logs?
- How is backup restoration proven?
- What is the rollout and rollback or forward-fix path?

### Evolution

- What is generated versus user-owned?
- What can change without migrating existing models?
- Which extension points are real?
- What evidence would justify extracting a service?

## Designer review questions

- Can the actor understand current state and available actions?
- Are domain and workflow state represented honestly?
- Are denied, failed, degraded, and recovery states distinguishable?
- Is every action reachable without pointer-only interaction?
- Does focus move predictably?
- Are device and system interactions visible enough for the actor?
- Does the interface expose policy without burdening the user with implementation?
- Can a reviewer trace a control to an intent and result?
- Does the interface avoid implying success before the domain commits it?

## Handoff contract between design and engineering

Do not hand off only static screens. Hand off:

- scenario and path identities,
- starting facts,
- visible-state model,
- intent bindings,
- semantic result matrix,
- state transitions,
- accessibility behavior,
- boundary assumptions,
- unresolved findings,
- evidence examples.

Do not hand off only backend contracts. Include what the actor observes and can do in each result state.
