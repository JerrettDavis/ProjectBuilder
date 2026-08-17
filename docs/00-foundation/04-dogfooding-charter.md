# Dogfooding Charter

## Objective

Project Builder will model its own development. Dogfooding is not a marketing exercise or a late-stage sample project. It is a continuing test of whether the product can represent real, evolving software work without forcing the team back into disconnected prose.

## Initial dogfood scope

The first internal model must cover:

1. Create a workspace.
2. Create a project.
3. Define the project intent and outcome.
4. Add an actor.
5. Add an episode.
6. Add a scenario.
7. Add scenes and interactions.
8. Define state, rules, and failure paths.
9. Review model gaps.
10. save, revise, export, and import the project.

The model should include the people involved in development, the browser, the application, the database, CI, source control, and generated documentation as participants or systems where relevant.

## Release gate

A feature is not complete until:

- its shipped behavior exists in the dogfood model,
- the model identifies its happy and material failure paths,
- its relevant domain and presentation state are separated,
- implementation references are linked,
- automated evidence is linked,
- unresolved modeling limitations are recorded.

A temporary exception requires an owner, rationale, consequence, and expiration condition.

## Bootstrap paradox

The first model will be authored in a checked-in canonical JSON file before the application can edit it. This is acceptable because it gives the model format and validators a concrete target.

The migration path is:

1. Hand-author the initial model.
2. Load and validate it in automated tests.
3. Render it read-only in the product.
4. Edit it through structured forms.
5. Edit it through lenses and canvas.
6. manage revisions and evidence in the product.
7. Stop hand-editing the canonical file except for migration tests and fixtures.

## Dogfood model ownership

- Product owns project intent, outcomes, personas, and scope.
- Domain experts own terms, rules, and scenario truth.
- Design owns interface state and interaction representation.
- Engineering owns implementation projections and technical constraints.
- Validation owns evidence classification and sufficiency.
- The team jointly reviews cross-boundary scenarios.

No role can unilaterally mark another role's unresolved claim as verified.

## Review rhythm

At least once per release slice, the team holds a model review:

1. Open the shipped scenario in Story lens.
2. Step through it in Scenario Flow.
3. Inspect the state transition and invariants.
4. Open the selected interface state.
5. Trace the implementation slice.
6. Inspect evidence.
7. compare the model to actual behavior.
8. Record divergence as a gap or change set.

## Dogfood telemetry

Track:

- percentage of shipped interactions represented,
- claims with linked evidence,
- model findings discovered before implementation,
- implementation defects caused by model omissions,
- model changes triggered by production learning,
- time from model change to impacted evidence identification,
- cases the product cannot represent cleanly.

These are learning metrics, not performance targets for individual contributors.

## Failure signal

The dogfood effort is failing when the team:

- maintains a parallel private model elsewhere,
- cannot connect a PR to model elements,
- marks broad sections "not applicable" to avoid modeling,
- edits generated artifacts instead of source definitions,
- relies on agents to fill model gaps without human review,
- postpones modeling until after implementation,
- stops recording divergence because the model is expensive to update.

Each signal should trigger a product or workflow correction.
