# Project Builder Schemas and Fixtures

## Files

| File | Purpose |
|---|---|
| `project-builder-model.schema.json` | canonical portable project model |
| `project-builder-changeset.schema.json` | semantic draft/commit operations |
| `project-builder-projection.schema.json` | generated artifact package and coverage |
| `pos-example.project-builder.json` | schema-valid POS reference model |
| `example-change-set.json` | compact semantic change-set example |
| `example-projection.json` | compact behavior-projection example |

## Contract rules

- JSON Schema draft 2020-12.
- UTF-8 and deterministic serialization for canonical exports.
- GUID identity.
- semantic model and view layout remain distinct.
- project format version is separate from generator/projection version.
- schema validation precedes semantic validation.
- an accepted document can still contain model findings.
- import never executes extension content.
- unknown safe extensions follow explicit preserve/reject policy.
- change sets are atomic and revision checked.
- projection artifacts identify source project revision and content digest.

## Validation layers

1. Parse limits and safe input handling.
2. Envelope and JSON Schema.
3. identifier uniqueness and reference resolution.
4. containment and relation rules.
5. domain/model invariants.
6. purpose-profile completeness.
7. authorization and import policy.
8. transactional persistence.

The schemas intentionally cannot prove every semantic rule. Those rules belong in the canonical model validation engine and are described in `../02-model/08-validation-rule-catalog.md`.

## Evolution

Before changing a schema:

1. state the scenario requiring the change,
2. classify compatibility,
3. update the format decision,
4. add old/current/future fixture tests,
5. implement deterministic migration,
6. verify unknown extension preservation,
7. update projections and documentation,
8. publish the compatibility impact.

Do not bind portable format shape directly to EF Core table layout.
