# Training Curriculum

## Purpose

The curriculum teaches Project Builder as a way of thinking, not merely as a sequence of buttons. Learners progress from concrete actor stories to explicit behavior, state, interfaces, boundaries, implementation, and evidence. Every module can be completed by a human without an agent.

The reference exercise is a point-of-sale item scan, but instructors should add a second domain so learners understand the method rather than memorizing one model.

## Audience tracks

### Domain participant

Needs Modules 1 through 4 and the workshop portions of Module 8.

### Product owner or analyst

Needs Modules 1 through 6, 8, and 10.

### Designer

Needs Modules 1 through 7, 9, and 10.

### Architect or engineer

Needs all modules, with deeper exercises in 6 through 12.

### Validator, security, or operations

Needs Modules 1 through 6 and 9 through 12.

### Administrator

Needs Modules 1, 3, 8, 10, 12, plus the Administrator Guide.

## Delivery formats

- self-guided lessons,
- instructor-led workshop,
- embedded contextual lessons in Guide Rail,
- role-specific learning paths,
- organization-certified modeling practice,
- reference-project labs.

The same lesson content should be addressable from the Studio without forcing a separate learning portal.

## Module 0: orientation

### Learning outcomes

- explain Project Builder's purpose,
- distinguish canonical model from lens and layout,
- navigate Studio regions,
- use knowledge states,
- commit a revision.

### Exercise

Create a project called "Neighborhood Tool Library" with one outcome and one actor. Move between Guide Rail and Studio. Mark one answer Unknown and commit.

### Evidence

Learner can explain why moving a node does not change domain truth.

## Module 1: outcomes, actors, and authority

### Concepts

- outcome versus output,
- actor as contextual role,
- beneficiary,
- responsibility,
- authority,
- system and device participants,
- current versus desired behavior.

### Exercise

For a POS, identify Clerk, Customer, Manager, Scanner, Price Book, Payment Provider, Support Technician, and Auditor. Remove any actor that is merely a technology name without a role.

### Challenge

Model a self-service kiosk. Compare authority and interaction differences with a clerk-operated register.

### Evidence

Learner identifies who may initiate, approve, override, observe, and support.

## Module 2: episodes, scenarios, scenes, and interactions

### Concepts

- narrative hierarchy,
- concrete scenario,
- scene boundary,
- intent and observation,
- containment versus reuse,
- path classification.

### Exercise

Model "Add a known product to an active transaction."

### Challenge

Decompose the scanner interaction without introducing database tables.

### Evidence

Scenario has starting facts, trigger, expected outcome, interactions, and end facts.

## Module 3: state, rules, and invariants

### Concepts

- domain, presentation, workflow, infrastructure, and externally observed state,
- facts,
- command, event, effect,
- validation, eligibility, decision, derivation, calculation, policy,
- invariant,
- transition.

### Exercise

Separate:

- selected transaction tab,
- active transaction,
- pending product lookup,
- price-book connectivity,
- provider-reported product status.

Define one transaction invariant and one interface-only state.

### Challenge

Write a falsifiable invariant for total calculation.

### Evidence

Learner can explain why the UI does not own transaction truth.

## Module 4: paths, failures, and recovery

### Concepts

- happy, alternate, exceptional, degraded, recovery, cancellation, compensation,
- semantic result,
- duplicate,
- timeout,
- conflict,
- partial effect,
- retry safety.

### Exercise

Add unknown product, price-book unavailable, prohibited product, and duplicate scan paths.

### Challenge

Model a provider timeout followed by a safe retry and contrast it with an unsafe retry.

### Evidence

Every result has a defined state effect and observable response.

## Module 5: guided completeness and knowledge

### Concepts

- purpose profiles,
- Unknown, Assumed, Deferred, Disputed, Not Applicable,
- blocking versus advisory finding,
- waiver and accepted risk,
- sources and authority,
- completeness without false certainty.

### Exercise

Run Discovery and Implementation Ready profiles over the same incomplete model.

### Challenge

Resolve one finding with evidence, defer one, and record one dispute.

### Evidence

Learner explains why 100 percent is not a universal correctness claim.

## Module 6: lenses and visual reasoning

### Concepts

- Story Map,
- Scenario Flow,
- State and Rule,
- System Context,
- Traceability,
- drilldown,
- filters and layout,
- semantic outline,
- impact analysis.

### Exercise

Find the same invariant through three lenses. Move nodes, then verify the semantic revision did not change.

### Challenge

Edit a relation in Scenario Flow and inspect its effect in Traceability.

### Evidence

Learner can choose a lens based on a question rather than visual preference.

## Module 7: interface modeling

### Concepts

- interface kinds,
- visible state,
- intents before controls,
- semantic result representation,
- graphical and non-graphical interfaces,
- accessibility and operability,
- scenario playback.

### Exercise

Design the POS transaction view for ready, pending scan, item added, unknown item, offline, and override-required states.

### Challenge

Represent the same use case as an HTTP API and as a device interaction. Compare contracts and observations.

### Evidence

Every modeled result has a truthful interface state.

## Module 8: facilitation and collaborative modeling

### Concepts

- concrete-instance interview,
- counterexample loop,
- parking lot,
- preserving disagreement,
- abstraction control,
- workshop change set,
- review and ownership.

### Exercise

Run a thirty-minute discovery session for a return transaction.

### Challenge

Handle a disagreement over whether a manager override is always required.

### Evidence

Session ends with confirmed model, decisions, assumptions, disputes, and owners.

## Module 9: systems, boundaries, and architecture

### Concepts

- child-context decomposition,
- Domain, Application, Infrastructure, Presentation responsibilities,
- ownership/trust/transaction/process/deployment/vendor/residency/failure boundaries,
- contracts and properties,
- architectural decision.

### Exercise

Decompose product lookup through POS, store context, corporate price book, and transaction.

### Challenge

Compare synchronous lookup, cached data, and event-distributed price book without selecting based on fashion.

### Evidence

Selected architecture traces to required properties and accepted tradeoffs.

## Module 10: vertical slices and delivery

### Concepts

- outcome-bearing slice,
- model-to-issue projection,
- Definition of Ready,
- change set,
- stacked work,
- model and code review together,
- rollout and compatibility.

### Exercise

Project item scan into Presentation, Application, Domain, Infrastructure, Contracts, and Evidence.

### Challenge

Split it into session-sized work without creating horizontal layers.

### Evidence

Each planned session ends with a human-observable behavior and proof.

## Module 11: specification and validation

### Concepts

- examples,
- properties,
- transition and decision tables,
- contract tests,
- integration tests,
- accessibility, security, and operational evidence,
- evidence staleness,
- baseline.

### Exercise

Create an evidence plan for "a duplicate scan signal cannot add two items."

### Challenge

Distinguish the evidence needed for business correctness, database concurrency, device delivery, and interface feedback.

### Evidence

Claims map to suitable proof types, not only unit tests.

## Module 12: governance, dogfooding, and extension

### Concepts

- model version,
- baseline,
- decision and assumption,
- extension registry,
- generated projection,
- human/agent parity,
- Project Builder modeling itself.

### Exercise

Model a small Project Builder feature, generate its implementation outline, then record a discovered model defect.

### Challenge

Design an agent suggestion workflow that cannot silently commit truth or manufacture evidence.

### Evidence

Learner explains what remains canonical and how changes are governed.

## Capstone

### Brief

Model "accept a manufacturer coupon during an active POS transaction" from actor outcome through interface, domain rules, external validation boundary, failures, implementation slice, and evidence.

### Required artifacts

- actors and authority,
- outcome,
- episode and at least five scenarios,
- state and rule catalog,
- at least two invariants,
- path matrix,
- graphical interface states,
- one non-graphical contract,
- system context,
- boundary properties,
- implementation projection,
- evidence plan,
- model review,
- baseline.

### Assessment rubric

| Dimension | Emerging | Competent | Advanced |
|---|---|---|---|
| Outcome clarity | feature or screen | observable actor result | competing outcomes and measures |
| Behavioral depth | happy path only | material paths | recovery, concurrency, temporal behavior |
| State fidelity | mixed UI/domain | categories separated | ownership and transitions explicit |
| Rule quality | prose assertions | falsifiable rules | source, context, property evidence |
| Interface | static mockup | intent/result mapping | accessibility and degraded behavior |
| Architecture | technology boxes | traced boundaries | properties, alternatives, decisions |
| Evidence | test list | claim-linked plan | layered, stale-aware, operational |
| Governance | undocumented choices | decisions and assumptions | baseline, risk, extension impact |
| Facilitation | single viewpoint | confirmed group model | disagreement and authority handled |

## Instructor notes

- Never reward unnecessary ontology density.
- Ask learners to explain the model in ordinary language.
- Use real counterexamples.
- Introduce formal terms after the learner has encountered the problem they solve.
- Alternate POS with healthcare scheduling, logistics, manufacturing, civic process, or home automation examples.
- Include accessibility and support perspectives early.
- Mark assumptions rather than supplying convenient answers.
- Demonstrate that an empty layer can be correct.
- Review model diffs, not only final diagrams.
