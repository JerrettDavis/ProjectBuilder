# User Quickstart

## What Project Builder is

Project Builder is a guided studio for defining a domain and the system that will serve it. You begin with people, systems, goals, and concrete behavior. You then expose state, rules, alternate paths, interfaces, boundaries, architecture, and evidence as the model needs them.

You do not need to know UML, BPMN, domain-driven design, or software architecture to begin. Project Builder will use plain questions and show the formal model behind your answers. You can always move between the guided path and the full Studio.

## What you will produce

A useful Project Builder project can contain:

- who participates and who benefits,
- what outcomes matter,
- episodes and scenarios that show real behavior,
- happy, alternate, failure, and recovery paths,
- state, facts, rules, invariants, commands, events, and effects,
- graphical, command-line, API, event, MCP, device, document, or human interfaces,
- organizational and system boundaries,
- contracts and architectural decisions,
- implementation-ready vertical slices,
- specifications, test plans, evidence, and release baselines.

Every view is a lens over the same underlying model. Moving a box does not change business truth. Editing a business rule updates every view that depends on it.

## Before you begin

Have one concrete outcome in mind. Good starting statements include:

- "A clerk can add a product to an active sale."
- "A customer can schedule a service appointment."
- "A dispatcher can assign a driver to a delivery."
- "A device can report an unsafe temperature."
- "A support technician can diagnose a failed integration."

Avoid beginning with broad solutions such as "build a microservice platform" or "make an AI dashboard." Project Builder will help you uncover what those solutions must enable.

## Create a project

1. From the workspace home, choose **New Project**.
2. Enter a project name.
3. State the purpose in one or two sentences.
4. State the first observable outcome.
5. Select a modeling purpose:
   - **Discovery** when you are still learning the domain.
   - **Interface Design** when you need to map interactions to a surface.
   - **Architecture** when you need to expose systems and boundaries.
   - **Implementation Ready** when a team will build a vertical slice.
   - **Release Ready** when behavior and evidence must support a release.
6. Create the project.

Project creation records revision 1. The Guide Rail offers the next useful question, but you can open any Studio area immediately.

## Orient yourself in the Studio

```text
Header      project, lens, search, command palette, review, revision
Explorer    model hierarchy and semantic categories
Center      active structured editor, canvas, table, player, or document
Inspector   selected element, relations, sources, findings
Guide Rail  contextual questions and next actions
Bottom      Problems, Evidence, History, Comments, Simulation, Output
```

Useful commands:

| Action | Default command |
|---|---|
| Open command palette | `Ctrl+K` or platform equivalent |
| Search project | `Ctrl+Shift+F` |
| Save draft locally | automatic |
| Review and commit changes | `Ctrl+Enter` |
| Undo draft operation | `Ctrl+Z` |
| Redo draft operation | `Ctrl+Shift+Z` |
| Open selected item | `Enter` |
| Return to parent scope | `Alt+Up` |
| Toggle Guide Rail | command palette or assigned shortcut |
| Toggle Explorer | command palette or assigned shortcut |
| Open Problems | command palette or bottom panel shortcut |

Actual bindings are displayed in the command palette and can be configured according to organization policy.

## Step 1: identify actors

An actor is a role in a context. "Clerk" is usually an actor. "Maya Smith" is usually a person assigned to that role, not the role itself.

Capture:

- name and kind,
- goal,
- responsibility,
- authority,
- constraints,
- interfaces or devices used,
- source or confidence when the actor is inferred.

Actor kinds include human role, organization role, system role, device role, automated role, and external-provider role.

Do not force certainty. Use:

- **Known** when supported,
- **Assumed** when plausible but unverified,
- **Unknown** when the answer matters but is not available,
- **Deferred** when intentionally postponed,
- **Disputed** when credible participants disagree,
- **Not Applicable** when a rule truly does not apply.

## Step 2: define an outcome

An outcome is an observable result for a beneficiary.

Weak: "Create a scan service."  
Stronger: "The clerk sees the correct purchasable item and price on the active transaction within the allowed response time."

Record:

- beneficiary,
- success signal,
- value or risk addressed,
- constraints,
- how someone will know it happened.

A screen, API, file, or event is usually an output. It becomes useful only in relation to an outcome.

## Step 3: create an episode

An episode is an end-to-end span of activity that produces an outcome. For a point of sale:

> Add merchandise to an active transaction.

An episode can have many scenarios:

- known item is scanned,
- barcode is unreadable,
- code is a coupon,
- product is prohibited,
- corporate price book is unavailable,
- clerk cancels the attempt.

Keep the episode at the actor and outcome level. Do not introduce database tables or service names unless they are already meaningful participants at that level.

## Step 4: describe one scenario

Choose a concrete path. State:

1. **Starting facts:** What must already be true?
2. **Trigger:** What starts this path?
3. **Actors:** Who participates?
4. **Expected outcome:** What should be observed?
5. **Scenes:** Where does responsibility, interface, or context change?
6. **Interactions:** What is exchanged?
7. **End facts:** What is true afterward?
8. **Path classification:** Happy, alternate, exceptional, degraded, recovery, cancellation, or compensation.

Example:

```text
Given an active transaction at Store 104
And a clerk is signed into register 7
And the corporate price book contains UPC 012345678905
When the scanner captures 012345678905
Then one unit of the corresponding item is added
And the transaction total reflects the store price
And the clerk sees the item description, quantity, and price
```

## Step 5: model interactions

For each interaction, capture:

- initiator,
- receiver,
- interface,
- intent,
- input or observed signal,
- authority,
- validation,
- semantic result,
- response or observation,
- boundary crossing,
- timing or reliability expectation when material.

A scanner emitting digits is not automatically a request to add an item. It may first be classified as a product, payment token, coupon, special command, or unknown input. Model the distinction when it affects behavior.

## Step 6: expose state, rules, and invariants

### State

State describes a condition relevant to behavior.

- **Domain state:** active transaction lines and totals.
- **Presentation state:** selected tab, open dialog, current focus.
- **Application-workflow state:** a pending lookup or approval.
- **Infrastructure state:** a retry counter or connection status.
- **Externally observed state:** provider availability reported to the system.

Do not make a selected UI tab into domain state unless the business actually recognizes it as a fact.

### Rule

A rule validates, decides, derives, calculates, or applies policy.

Example:

> A scanned token matching the product-code format is classified as a product code unless a higher-priority reserved pattern applies.

### Invariant

An invariant must remain true in every valid state within its scope.

Example:

> A transaction total equals the sum of its priced line extensions, taxes, discounts, and adjustments according to the active calculation policy.

### Transition

A transition connects:

- source state,
- trigger,
- preconditions and guard,
- semantic result,
- target state,
- facts and events,
- external effects.

Use the State and Rule lens to review combinations and impossible paths.

## Step 7: add the paths people forget

Open the Guide Rail or Problems panel and ask:

- What can be invalid?
- What can be unavailable?
- What can be slow?
- What can be duplicated?
- What can change concurrently?
- Who can be unauthorized?
- What can be cancelled?
- What happens after a partial external effect?
- Can recovery retry safely?
- What does the actor see in every case?
- Which assumptions are unsupported?

For item scan, common paths include:

- unreadable token,
- unrecognized token,
- unknown product,
- inactive product,
- age-restricted product,
- missing store price,
- price-book timeout,
- duplicate device signal,
- transaction changed concurrently,
- manager override required,
- user cancels,
- item added but UI response interrupted.

Do not label every non-happy path an exception. Alternate, degraded, denied, conflict, timeout, cancellation, and recovery outcomes often require different behavior.

## Step 8: choose and design the interface

Create an interface only after you can explain the interaction.

For a graphical interface:

1. Choose the state that must be visible.
2. Add regions and controls.
3. Bind display fields to read-model observations.
4. Bind controls or device inputs to intents.
5. model loading, empty, invalid, denied, failed, degraded, and success states.
6. define keyboard and accessibility behavior.
7. map the scenario steps over the interface.
8. play happy and failure paths.

For an API, CLI, event, MCP, device, document, or human procedure, use the specialized structured editor. The same model concepts still apply: accepted intent, observed result, state, contract, errors, authority, and constraints.

## Step 9: drill into systems and boundaries

When an interaction depends on internal or external behavior, open it as a child context.

At the outer level:

> POS accepts a scanned token and shows the resulting transaction state.

At the next level:

- scanner adapter captures token,
- classifier determines token kind,
- product code is looked up for store,
- policy determines sale eligibility,
- application attempts to add the line,
- transaction applies rules and emits result,
- interface receives an updated read model.

Mark boundaries:

- organizational ownership,
- trust,
- external vendor,
- process,
- transaction,
- deployment,
- protocol,
- data residency,
- failure domain.

Attach contracts and properties only where they matter.

## Step 10: project an implementation slice

When the model is ready, open **Implementation Slice**.

A typical projection identifies:

- **Presentation:** translates interface input and renders observations.
- **Application:** authorizes and coordinates the use case.
- **Domain:** owns business facts, rules, transitions, and semantic outcomes.
- **Infrastructure:** implements external mechanisms and provider interactions.
- **Contracts:** define versioned boundary agreements.
- **Evidence:** proves claims at the appropriate layers.

The four labels are responsibility lenses, not mandatory folders for every behavior. A pure domain rule may need no infrastructure. An external observation may require an infrastructure adapter without becoming domain truth itself.

## Step 11: review completeness

Select a purpose profile and inspect:

- blocking findings,
- unknown and assumed claims,
- unhandled semantic results,
- missing interface states,
- unsupported boundary crossings,
- invariants without proof,
- stale evidence,
- decisions without authority,
- unresolved review comments.

Completeness means fit for a named purpose. It does not mean the model is universally complete or correct.

## Step 12: commit a revision

Project Builder keeps a private draft until you commit.

Before commit:

1. Review operations.
2. Add a reason.
3. inspect new or changed findings.
4. confirm the base revision.
5. commit.

A stale revision never overwrites another user's work. You will receive a conflict report and can rebase or selectively reapply your operations.

## Step 13: create a baseline

Create a baseline when a model revision is approved for a meaningful purpose such as implementation or release.

A baseline pins:

- project revision,
- purpose profile and rule versions,
- projection versions,
- evidence,
- approvals,
- accepted risks.

A baseline is immutable. A later correction supersedes it.

## Recommended first-hour exercise

Model one concrete scenario only:

1. Create a project.
2. Add one primary actor and one beneficiary.
3. Define one outcome.
4. Create one episode and one happy scenario.
5. Add two interactions.
6. Add one domain state and one invariant.
7. Add one failure path.
8. design one interface state.
9. mark one external boundary.
10. review Discovery completeness and commit.

Depth before breadth teaches the product faster than listing an entire system.

## Common mistakes

### Beginning with screens

A screen without behavior often hides unresolved domain decisions. Start with actor outcome and scenario, then design the interface.

### Treating systems as actors without context

A system can participate, but record the role it plays in the scenario.

### Writing only the happy path

A system is often defined by how it rejects, degrades, recovers, and communicates uncertainty.

### Duplicating the same fact across diagrams

Create one model element and link it. Every lens should project the shared identity.

### Converting every unknown into a guessed value

Unknown and Assumed are valid, visible states. Guesses disguised as facts are not.

### Treating tests as coverage percentages

Evidence supports claims. Different claims require examples, properties, contracts, integration behavior, accessibility checks, security review, or operational rehearsal.

### Over-architecting early

Model the behavior and boundary first. Select process, persistence, messaging, and deployment patterns when the properties require them.
