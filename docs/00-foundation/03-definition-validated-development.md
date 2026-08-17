# Definition-Validated Development

## Governing paradigm

Project Builder follows Definition-Validated Development, abbreviated DVD:

`Reality → Model → Definition → Delegated Implementation → Validation → Learning → Refined Model`

The developer's job expands from producing code to preserving the relationship between reality, definition, implementation, and evidence. Code is one important artifact in that relationship. It is not the sole source of truth.

## Define

Definition begins with a universe of discourse. What part of reality is the project trying to model, and for what purpose? The author then records participants, outcomes, facts, interactions, constraints, and boundaries.

A strong definition does not merely describe the happy example. It distinguishes:

- what may vary,
- what must remain invariant,
- what initiates behavior,
- what authority is required,
- what state can change,
- what state cannot change,
- what can fail,
- what must be observed,
- what compensates or recovers,
- what evidence would be persuasive.

Project Builder structures this work without requiring the author to know formal modeling vocabulary at the beginning.

## Validate the definition

A definition can be internally inconsistent before implementation begins. Project Builder therefore validates the model itself:

- references resolve,
- required roles are assigned,
- transitions have defined source and target state,
- conditions are not contradictory,
- outcomes are observable,
- boundaries have contracts,
- failure paths terminate or explicitly remain unresolved,
- invariants are attached to owners,
- claims identify evidence,
- assumptions are visible.

Model validation does not prove business truth. It proves structural and semantic coherence relative to the authored definitions.

## Delegate implementation

Once behavior is sufficiently defined, implementation can be delegated to a person, team, generator, or agent. Delegation is constrained by the model:

- the behavior to implement,
- the boundaries to respect,
- the contracts to satisfy,
- the qualities to preserve,
- the evidence to return,
- the decisions that are fixed,
- the decisions still open.

The product should generate a work package that is smaller than the full project but contains enough context to avoid local optimization.

## Validate the implementation

Implementation validation gathers evidence at several levels:

| Claim | Typical evidence |
|---|---|
| A pure rule produces the expected result | Example and property tests |
| An aggregate preserves an invariant | State transition tests and property tests |
| An adapter honors a provider contract | Contract tests |
| A use case orchestrates effects in order | Integration tests |
| A user can complete a scenario | End-to-end behavioral test |
| A quality attribute holds under load | Performance experiment |
| A recovery path works in production conditions | Rehearsal, fault injection, or operational evidence |
| A generated projection matches the model | Deterministic snapshot plus semantic tests |

Evidence is attached to model claims, not merely stored as an undifferentiated build result.

## Learn

When implementation or testing reveals divergence, the team decides which artifact was wrong:

- the observation of reality,
- the model,
- the definition,
- the implementation,
- the test,
- the environment,
- the assumption.

The correction is recorded. The system should never treat a passing test against a faulty definition as proof of correctness.

## Refine

Refinement updates the authoritative model, supersedes stale decisions, regenerates projections, and identifies impacted evidence. Change impact is a first-class capability because a model that cannot safely evolve becomes documentation debt.

## Convergence

Project Builder supports simultaneous directions of reasoning.

Top-down reasoning begins with outcomes and decomposes into behavior, boundaries, interfaces, and implementation slices.

Bottom-up reasoning begins with discovered facts, existing systems, contracts, constraints, and code. It composes them into a more complete model.

The two directions converge in an executable domain language: named concepts, rules, transitions, commands, outcomes, and effects that stakeholders can recognize and engineers can implement.

## Practical workflow

1. **Observe**: capture a real situation, participant, pain, regulation, or opportunity.
2. **Frame**: declare scope, purpose, and excluded concerns.
3. **Narrate**: describe episodes and scenarios in the language of participants.
4. **Formalize**: identify state, rules, invariants, paths, and authority.
5. **Project**: design interfaces, boundaries, and vertical slices.
6. **Specify**: derive behavioral claims, properties, contracts, and quality experiments.
7. **Implement**: write or generate the smallest coherent slice.
8. **Prove**: attach evidence at the appropriate levels.
9. **Compare**: evaluate actual outcomes against the model.
10. **Refine**: update definitions and propagate impact.

## Anti-patterns

### Tool-first modeling
Selecting microservices, event sourcing, a graph database, or a UI framework before the behavior and boundaries are understood. Project Builder may record the proposal, but it should ask what problem the choice solves.

### Ticket transcription
Copying backlog items into a model without reconciling duplicated terms, conflicting behavior, or missing state.

### Test count as evidence
Counting tests without identifying which claims they prove.

### Generated certainty
Allowing an agent to produce polished prose that conceals unknowns or lacks authority.

### Layer-only delivery
Building repositories, controllers, database tables, and screens as independent workstreams without a complete behavioral slice.

### Diagram drift
Maintaining images that cannot identify their source model revision or impacted claims.

## DVD inside Project Builder

Project Builder should eventually store each product feature as:

- observed need,
- model elements,
- definition baseline,
- implementation work package,
- code and configuration references,
- evidence,
- discovered divergence,
- refined baseline.

That record is the product's own development history and the strongest possible dogfooding test.
