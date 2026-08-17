# Session `/goal` Template

Use this template to dispatch one bounded implementation session. Replace bracketed content and delete irrelevant sections.

```text
/goal [Imperative one-sentence outcome]

CONTEXT

Project Builder is a definition-first visual domain and system modeling studio. This task implements one bounded vertical slice under the repository's Definition-Validated Delivery workflow.

SOURCE DEFINITION

- Project/model: [id or fixture]
- Revision/baseline: [revision]
- Capability: [id/name]
- Episode/scenario/interaction: [ids/names]
- Issue/epic: [id]
- Relevant decisions: [ADR ids]
- Blocking findings: [ids or none]

ACTOR OUTCOME

[Who] can [observable behavior] so that [beneficiary result].

STARTING FACTS

- [...]
- [...]

TRIGGER

[...]

SEMANTIC RESULTS

- Success: [...]
- Alternate: [...]
- Invalid/Denied: [...]
- Unavailable/Timeout: [...]
- Conflict/Duplicate: [...]
- Cancellation/Recovery: [...]

STATE TRANSITION

- Source:
- Trigger:
- Preconditions:
- Target:
- Events/facts:
- External effects:

INVARIANTS

1. [...]
2. [...]

INTERFACE BEHAVIOR

- Interface kind:
- Input/intent:
- Visible or observable state:
- Loading/pending:
- Success:
- Failure/degraded:
- Accessibility/focus:
- Authorization:

BOUNDARIES AND CONTRACTS

- [...]
- [...]

SCOPE

Included:
- [...]
- [...]

Excluded:
- [...]
- [...]

REQUIRED READS

1. AGENTS.md
2. [primary route document]
3. [feature definition]
4. [relevant example/ADR]

Do not preload:
- [unrelated documents]

EXPECTED CODE AREAS

- [paths/modules]

Inspect broadly enough to find existing patterns, but edit outside these areas only when required for a coherent build. Report every scope expansion.

IMPLEMENTATION CONSTRAINTS

- .NET 10/C# 14.
- preserve module dependency direction.
- Domain contains no UI, EF, ASP.NET, provider, or environment dependency.
- use explicit semantic result types.
- prefer pure domain decisions and immutable values.
- no generic service/repository abstraction.
- no speculative extension points.
- no public contract change without compatibility review.
- generated output must be deterministic and readable.
- [feature-specific constraints]

EVIDENCE

Focused:
- [commands/tests]

Slice:
- [integration/contract/component/E2E]

Repository:
- [verification command required or not]

Manual:
- [human-observable scenario]

MODEL AND DOCUMENTATION

Update:
- [dogfood fixture/model]
- [docs]
- [schema/projection if applicable]

A behavior change is incomplete when the canonical definition remains stale.

STOP CONDITIONS

Create a finding and continue only through a safe seam when:
- [business ambiguity]
- [contract ambiguity]
- [security/safety issue]
- [migration incompatibility]
- [architecture conflict]

Do not invent business rules or mark an agent assertion as evidence.

MACHINE SAFETY

- no global input injection,
- no mouse/focus/window stealing,
- no changes outside repository,
- no unapproved external side effects or production access,
- use isolated/headless UI tests where possible.

DEFINITION OF DONE

- [behavior acceptance]
- [state/invariant acceptance]
- [interface acceptance]
- [evidence acceptance]
- [documentation/model acceptance]
- no hidden failures or skipped required checks.

HANDOFF

## Source definition
## Behavior delivered
## Model changes
## Code and migration changes
## Evidence and commands
## Decisions
## Assumptions/findings
## Risks
## Diff scope
## Exact next entry point
```

## Sizing guidance

A session is too large when it includes several independent outcomes, introduces an unproven framework, or cannot be validated without later work. Split by observable behavior, not by technical layer.

A session is too small when it produces only interfaces, entities, repositories, or generic infrastructure that no actor or subsequent bounded session can exercise.

## Review prompt

After implementation, a separate reviewer can use:

```text
Review this change against its source Project Builder definition. Inspect the model diff, code diff, contracts, migrations, generated artifacts, and evidence. Identify semantic drift, missing result handling, invariant or transaction-boundary risk, architecture leakage, accessibility/security gaps, test weaknesses, and unnecessary complexity. Do not rewrite the feature. Produce prioritized findings with exact file references and a clear release-blocking classification.
```
