# Interface Designer

## Purpose

The Interface Designer connects actor intent, observable state, domain behavior, and error handling. It generalizes beyond graphical screens because interfaces include CLIs, APIs, messages, MCP surfaces, devices, documents, and human procedures.

The designer begins after at least one coherent interaction exists. It does not require the entire system to be modeled.

## Common interface model

Every interface defines:

- interface kind,
- participants,
- boundary crossed,
- accepted intents,
- input shape,
- exposed or visible state,
- observations and semantic results,
- authorization,
- timing,
- accessibility or operability constraints,
- contract version,
- failure and recovery presentation,
- evidence.

## State mapping

The designer explicitly maps state categories:

```text
Domain state
  Transaction.Status = Open
  Transaction.BalanceDue = 2.49

Application state
  PriceLookup = Completed
  OperationId = ...

Presentation state
  SelectedTab = Sale
  ScanIndicator = Success
  FocusTarget = ScanInput

Rendered observation
  Line item visible
  Total "$2.49"
  Status "Item added"
```

A mapping can transform domain state into a read model. The interface should not bind directly to mutable aggregates.

## Intent binding

Controls and interface operations bind to intents:

```text
Button "Pay Cash"
  → Intent: Begin cash tender
  → Availability: Transaction open AND balance due > 0
  → Authority: Clerk assigned to register
  → Pending observation: Cash entry panel opens
```

The binding does not specify a handler class or database operation until implementation projection.

## Graphical interface editor

### Elements

- frame,
- region,
- stack, grid, and free layout,
- text and heading,
- data display,
- list and table,
- input,
- button and action,
- navigation,
- tabs,
- status and alert,
- dialog and drawer,
- menu,
- image and icon,
- reusable component,
- annotation.

### Frame states

A frame can have named variants:

- empty,
- loading,
- ordinary,
- validation error,
- denied,
- dependency unavailable,
- degraded,
- success,
- completed,
- offline,
- conflict.

Variants share component identity and override only changed presentation state.

### Bindings

A property can bind to:

- presentation state,
- read model value,
- derived display value,
- availability rule,
- authorization result,
- validation finding,
- scenario example data.

Bindings are inspectable and typed.

### Interaction overlay

A selected scenario overlays numbered interactions:

1. scanner submits captured value,
2. pending indicator appears,
3. product line appears,
4. total updates,
5. focus returns to scan target,
6. status is announced.

The user can step through happy and failure paths.

### Responsive and environmental variants

Frames can represent:

- viewport sizes,
- kiosk or register form factors,
- touch and keyboard modes,
- high contrast,
- reduced motion,
- disconnected or degraded device state,
- localization expansion.

The MVP need not become a full responsive CSS designer. It must capture constraints and representative states.

## CLI designer

Defines:

- executable or command namespace,
- command hierarchy,
- arguments and options,
- input streams,
- output streams,
- exit codes,
- prompts,
- interactive and non-interactive behavior,
- machine-readable output,
- help,
- cancellation and timeout,
- idempotency and side effects.

Example:

```text
project-builder model validate <file>
  --profile implementation-ready
  --format text|json
Exit:
  0 valid
  2 findings
  3 invalid file
  4 internal failure
```

## API designer

Defines:

- resource or operation,
- method or RPC,
- route,
- request schema,
- response and semantic errors,
- authentication and authorization,
- idempotency,
- concurrency,
- pagination,
- caching,
- rate expectations,
- versioning,
- correlation,
- examples,
- contract evidence.

OpenAPI is a projection of this model where applicable.

## Event and message designer

Defines:

- event meaning,
- producer authority,
- consumers,
- schema,
- partition or ordering key,
- delivery semantics,
- duplication behavior,
- replay,
- retention,
- compatibility,
- sensitive data,
- dead-letter and recovery,
- correlation and causation.

The interface model avoids claiming "exactly once" without defining the observable semantics and mechanisms.

## MCP designer

Defines:

### Tool
- name and description,
- input schema,
- output schema,
- side-effect classification,
- authority,
- confirmation requirements,
- idempotency,
- error semantics,
- model scope exposed,
- audit.

### Resource
- URI pattern,
- representation,
- sensitivity,
- freshness,
- pagination,
- authorization.

### Prompt
- intended user,
- arguments,
- context boundaries,
- output expectation,
- safety and provenance.

MCP is an interface type. Agent use remains optional.

## Device interface designer

Defines:

- physical device,
- signals and protocol,
- input and output,
- timing,
- connection states,
- calibration,
- environmental constraints,
- operator feedback,
- fault modes,
- simulation adapter,
- safety considerations.

POS barcode scanner example:

- decoded text,
- symbology,
- scan timestamp,
- device identifier,
- duplicate physical scans,
- disconnection,
- malformed or partial data,
- audible or visual acknowledgment.

## Document and form designer

Defines:

- document purpose,
- author and recipient,
- fields,
- authority,
- version,
- signing or approval,
- validation,
- retention,
- accessibility,
- delivery,
- correction and supersession.

## Human procedure designer

Defines:

- roles,
- ordered or conditional steps,
- tools and forms,
- handoffs,
- acknowledgment,
- time expectations,
- escalation,
- safety,
- evidence.

This allows Project Builder to model organizational work that cannot or should not be automated.

## Design system

A workspace can define:

- tokens,
- component definitions,
- variants,
- accessibility constraints,
- supported interface types,
- naming and content rules.

A reusable component has:

- interface contract,
- exposed properties,
- slots,
- intents emitted,
- observations rendered,
- state requirements,
- evidence.

A component instance cannot add an unmodeled domain side effect.

## Generated handoff

An interface handoff includes:

- frame or contract projection,
- state map,
- intent bindings,
- result and error matrix,
- scenario overlays,
- accessibility requirements,
- responsive or environmental variants,
- linked domain and application behavior,
- implementation constraints,
- evidence plan.

## Acceptance criteria

- An interface can be designed from modeled state without defining database tables.
- Controls bind to intents, not direct state mutation.
- Every material semantic result can be represented.
- Domain and presentation state remain visibly distinct.
- Non-graphical interfaces receive first-class editors.
- Scenario playback can move across interface states.
- Accessibility findings appear during design rather than only after implementation.
