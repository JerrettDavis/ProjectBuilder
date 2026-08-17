# Validation Rule: [Stable Code and Name]

## Identity

| Field | Value |
|---|---|
| Code | PB-[AREA]-NNN |
| Version | |
| Status | Draft / Active / Deprecated |
| Owner | |
| Purpose profiles | |
| Default severity | Blocking / Error / Warning / Advisory |

## Intent

Explain what model defect or delivery risk the rule detects.

## Applicability

The rule applies when:

- element/relation kinds:
- project purpose/profile:
- model state:
- extension:
- exceptions:

## Predicate

State the rule deterministically.

```text
For every ...
there must be ...
unless ...
```

## Rationale

Explain why the rule matters in actor, correctness, safety, architecture, or evidence terms.

## Finding payload

- primary element,
- related elements,
- message,
- explanation,
- repair actions,
- source/rationale link,
- severity,
- waiver policy.

## Repair actions

1. ...
2. mark Unknown/Deferred if allowed.
3. link existing element.
4. create required element.
5. request authorized waiver.

Repairs must use ordinary commands and change sets.

## Examples

### Valid

...

### Invalid

...

### Not applicable

...

## Edge cases

- ...

## Test plan

- example tests,
- property tests,
- registry validation,
- performance,
- extension interaction,
- migration/version behavior.

## Compatibility

Describe whether changing this rule can change historical completeness, baseline status, or release gates. Rule versions must be preserved in baselines.
