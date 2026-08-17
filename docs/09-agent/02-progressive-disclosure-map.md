# Progressive Disclosure Map

## Purpose

This map tells a contributor or agent which documents to read for a task. It prevents the entire documentation suite from flooding working context while preserving access to deeper rationale when needed.

Always begin with the root repository instructions and the active task. Then follow one route below.

## Universal minimum

Read:

1. `/AGENTS.md`.
2. root `/README.md`.
3. active issue or `/goal` prompt.
4. `docs/00-foundation/02-principles-and-non-negotiables.md`.
5. the one relevant route below.

Do not read every linked document preemptively.

## Route: repository bootstrap

Read in order:

1. `docs/IMPLEMENTATION-PLAN.md`, Phase 0.
2. `docs/04-architecture/02-dotnet-solution-and-repository-structure.md`.
3. `docs/04-architecture/03-module-and-dependency-architecture.md`.
4. `docs/05-engineering/01-engineering-standards.md`.
5. `docs/05-engineering/03-testing-and-evidence-strategy.md`.
6. selected session in `docs/06-delivery/04-session-sized-implementation-plan.md`.

Open only when needed:

- CI/CD for workflow changes,
- observability for service defaults,
- security for identity or secrets,
- source-generation policy for analyzers/generators.

## Route: canonical model

Read:

1. `docs/02-model/01-canonical-meta-model.md`.
2. one subject document:
   - narrative hierarchy,
   - state/rules/invariants,
   - paths/failures/recovery,
   - boundaries/layers,
   - traceability/evidence,
   - format/versioning.
3. `docs/02-model/08-validation-rule-catalog.md`.
4. relevant POS example.
5. feature tests and current domain types.

Open architecture command/change-set document only when implementing write behavior.

## Route: application command or query

Read:

1. relevant model subject.
2. `docs/04-architecture/04-application-command-query-and-event-model.md`.
3. `docs/04-architecture/03-module-and-dependency-architecture.md`.
4. source feature folder and tests.
5. selected acceptance story.

Open persistence/realtime/API documents only if the use case crosses those boundaries.

## Route: persistence or migration

Read:

1. `docs/04-architecture/05-persistence-revisions-and-concurrency.md`.
2. `docs/02-model/07-project-file-format-and-versioning.md`.
3. `docs/05-engineering/03-testing-and-evidence-strategy.md`, persistence section.
4. relevant domain/application contract.
5. existing mappings and migrations.

Open deployment guide for production migration behavior.

## Route: Studio UI

Read:

1. `docs/03-experience/01-information-architecture.md`.
2. `docs/03-experience/02-studio-shell.md`.
3. selected screen/editor document.
4. `docs/03-experience/07-accessibility-keyboard-and-focus.md`.
5. current component and UI tests.

For a guided flow, add Guided Modeling Wizard. For canvas, use the canvas route.

## Route: canvas and lenses

Read:

1. `docs/03-experience/04-canvas-lenses-and-drilldown.md`.
2. `docs/04-architecture/07-projections-generators-and-analyzers.md`, projection section.
3. `docs/03-experience/07-accessibility-keyboard-and-focus.md`.
4. relevant model documents.
5. current lens graph/canvas contracts and performance tests.

Do not load interface designer docs unless the lens includes target-system UI frames.

## Route: interface designer

Read:

1. `docs/03-experience/05-interface-designer.md`.
2. `docs/07-guides/03-designer-and-architect-guide.md`.
3. `docs/02-model/03-state-events-rules-and-invariants.md`.
4. relevant scenario example.
5. accessibility document.

Use `08-example-pos/05-interface-and-state-model.md` for the reference behavior.

## Route: boundaries and architecture

Read:

1. `docs/02-model/05-boundaries-layers-and-vertical-slices.md`.
2. `docs/07-guides/03-designer-and-architect-guide.md`.
3. `docs/04-architecture/01-system-context-and-containers.md`.
4. relevant ADRs and scenario.
5. security/operations docs only for affected properties.

## Route: import, export, schema

Read:

1. `docs/02-model/07-project-file-format-and-versioning.md`.
2. schemas under `docs/schemas/`.
3. `docs/04-architecture/05-persistence-revisions-and-concurrency.md`.
4. security import controls.
5. contract and migration tests.

## Route: validation and completeness

Read:

1. `docs/01-product/04-lifecycle-and-completeness.md`.
2. `docs/02-model/06-traceability-evidence-and-gaps.md`.
3. `docs/02-model/08-validation-rule-catalog.md`.
4. selected purpose profile.
5. tests for the relevant rule registry.

## Route: specifications, generators, analyzers

Read:

1. `docs/04-architecture/07-projections-generators-and-analyzers.md`.
2. `docs/05-engineering/06-source-generation-and-extension-policy.md`.
3. `docs/02-model/07-project-file-format-and-versioning.md`.
4. target projection contract.
5. current generator tests and package wiring.

Open PatternKit or other external repository conventions only when the task explicitly integrates them.

## Route: security and identity

Read:

1. `docs/04-architecture/08-security-privacy-and-threat-model.md`.
2. relevant application/API/persistence document.
3. `docs/07-guides/05-administrator-guide.md`.
4. current authorization policies and threat model.
5. selected ASVS mapping or security tests.

## Route: collaboration

Read:

1. `docs/03-experience/06-collaboration-history-and-review.md`.
2. `docs/04-architecture/04-application-command-query-and-event-model.md`.
3. `docs/04-architecture/05-persistence-revisions-and-concurrency.md`.
4. `docs/04-architecture/06-api-realtime-and-integration-contracts.md`.
5. current conflict and SignalR tests.

## Route: CI, release, deployment, operations

Read:

1. `docs/05-engineering/04-ci-cd-and-release-engineering.md`.
2. `docs/04-architecture/09-observability-performance-and-operations.md`.
3. `docs/04-architecture/10-deployment-and-environments.md`.
4. `docs/06-delivery/06-definition-of-ready-and-done.md`.
5. administrator guide.
6. active release profile.

## Route: training or documentation

Read:

1. target audience guide.
2. `docs/00-foundation/05-ubiquitous-language.md`.
3. relevant model/experience document.
4. POS walkthrough or another approved example.
5. documentation Definition of Done.

## Route: dogfooding

Read:

1. `docs/00-foundation/04-dogfooding-charter.md`.
2. the product feature definition.
3. corresponding dogfood model slice.
4. `docs/05-engineering/02-definition-validated-delivery-workflow.md`.
5. evidence strategy.

## Route: agent feature

Read:

1. `docs/09-agent/01-agent-operating-model.md`.
2. security/privacy model.
3. canonical change-set/API contracts.
4. target human workflow.
5. evaluation plan and project policy.

Do not implement an agent shortcut before the equivalent human path is complete.

## Escalation triggers

Load broader context or create a finding when:

- a proposed type changes the canonical element envelope,
- a new relation kind has unclear cardinality or deletion behavior,
- a UI action mutates semantic state outside commands,
- a provider type crosses into Domain/Application,
- a change alters project format,
- a change creates a new independently deployed process,
- an invariant cannot be protected by the proposed transaction boundary,
- generated output would overwrite user-owned code,
- an agent path lacks human parity,
- accessibility requires a different interaction contract,
- current docs disagree.

## Session context header

Every session prompt should include:

```text
Required reads:
- AGENTS.md
- <one route primary document>
- <feature definition>
- <relevant ADR or example>

Do not preload:
- <unrelated docs>

Escalate when:
- <specific triggers>
```
