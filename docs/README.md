# Project Builder Documentation

> A definition-first studio for turning a domain into an inspectable, testable, and eventually executable system model.

Project Builder helps people describe what they are trying to accomplish before they select implementation details. It begins with actors, outcomes, episodes, scenarios, scenes, and interactions. It then exposes state, rules, paths, interfaces, boundaries, systems, contracts, evidence, and implementation slices. Every editor is a lens over one canonical model.

The long-term ambition is a modern visual programming environment. Unlike a low-code tool that hides decisions behind generated plumbing, Project Builder makes decisions visible and requires the author to resolve material ambiguity. The system can assist, project, validate, scaffold, and eventually execute portions of the model, but no essential workflow depends on an agent.

## Start here

| Need | Read |
|---|---|
| Understand the product | [Vision and charter](00-foundation/01-vision-and-charter.md) |
| Understand the governing development paradigm | [Definition-Validated Development](00-foundation/03-definition-validated-development.md) |
| Understand the model | [Canonical meta-model](02-model/01-canonical-meta-model.md) |
| Understand the user experience | [Studio shell](03-experience/02-studio-shell.md) and [guided modeling wizard](03-experience/03-guided-modeling-wizard.md) |
| Understand the .NET architecture | [System context and containers](04-architecture/01-system-context-and-containers.md) |
| Begin implementation | [Implementation plan](IMPLEMENTATION-PLAN.md) and [repository bootstrap checklist](10-reference/repo-bootstrap-checklist.md) |
| Dispatch bounded agent sessions | [Bootstrap goal prompt](09-agent/03-bootstrap-goal-prompt.md) |
| See the model worked end to end | [Point-of-sale walkthrough](07-guides/07-point-of-sale-walkthrough.md) and [machine-readable fixture](schemas/pos-example.project-builder.json) |
| Review initial architecture decisions | [ADR index](11-decisions/README.md) |
| Inspect portable contracts | [Schemas and fixtures](schemas/README.md) |
| Browse standalone diagrams | [Diagram index](diagrams/README.md) |
| Verify package contents | [Generated manifest](MANIFEST.md) |

## Document map

### Foundation
- [Vision and charter](00-foundation/01-vision-and-charter.md)
- [Principles and non-negotiables](00-foundation/02-principles-and-non-negotiables.md)
- [Definition-Validated Development](00-foundation/03-definition-validated-development.md)
- [Dogfooding charter](00-foundation/04-dogfooding-charter.md)
- [Ubiquitous language](00-foundation/05-ubiquitous-language.md)
- [Assumptions, decisions, and open questions](00-foundation/06-assumptions-decisions-open-questions.md)

### Product
- [Product requirements](01-product/01-product-requirements.md)
- [Personas, jobs, and permissions](01-product/02-personas-jobs-and-permissions.md)
- [Capability map](01-product/03-capability-map.md)
- [Lifecycle and completeness](01-product/04-lifecycle-and-completeness.md)
- [Success metrics and analytics](01-product/05-success-metrics-and-analytics.md)
- [Scope, releases, and non-goals](01-product/06-scope-releases-and-non-goals.md)

### Canonical model
- [Canonical meta-model](02-model/01-canonical-meta-model.md)
- [Narrative hierarchy](02-model/02-narrative-hierarchy.md)
- [State, events, rules, and invariants](02-model/03-state-events-rules-and-invariants.md)
- [Paths, conditions, failures, and recovery](02-model/04-paths-conditions-failures-and-recovery.md)
- [Boundaries, layers, and vertical slices](02-model/05-boundaries-layers-and-vertical-slices.md)
- [Traceability, evidence, and gaps](02-model/06-traceability-evidence-and-gaps.md)
- [Project file format and versioning](02-model/07-project-file-format-and-versioning.md)
- [Validation rule catalog](02-model/08-validation-rule-catalog.md)

### Product experience
- [Information architecture](03-experience/01-information-architecture.md)
- [Studio shell](03-experience/02-studio-shell.md)
- [Guided modeling wizard](03-experience/03-guided-modeling-wizard.md)
- [Canvas, lenses, and drilldown](03-experience/04-canvas-lenses-and-drilldown.md)
- [Interface designer](03-experience/05-interface-designer.md)
- [Collaboration, history, and review](03-experience/06-collaboration-history-and-review.md)
- [Accessibility, keyboard, and focus](03-experience/07-accessibility-keyboard-and-focus.md)
- [Screen catalog and wireframes](03-experience/08-screen-catalog-and-wireframes.md)

### Architecture
- [System context and containers](04-architecture/01-system-context-and-containers.md)
- [.NET solution and repository structure](04-architecture/02-dotnet-solution-and-repository-structure.md)
- [Module and dependency architecture](04-architecture/03-module-and-dependency-architecture.md)
- [Command, query, and event model](04-architecture/04-application-command-query-and-event-model.md)
- [Persistence, revisions, and concurrency](04-architecture/05-persistence-revisions-and-concurrency.md)
- [API, realtime, and integration contracts](04-architecture/06-api-realtime-and-integration-contracts.md)
- [Projections, generators, and analyzers](04-architecture/07-projections-generators-and-analyzers.md)
- [Security, privacy, and threat model](04-architecture/08-security-privacy-and-threat-model.md)
- [Observability, performance, and operations](04-architecture/09-observability-performance-and-operations.md)
- [Deployment and environments](04-architecture/10-deployment-and-environments.md)

### Engineering and delivery
- [Engineering standards](05-engineering/01-engineering-standards.md)
- [Definition-validated delivery workflow](05-engineering/02-definition-validated-delivery-workflow.md)
- [Testing and evidence strategy](05-engineering/03-testing-and-evidence-strategy.md)
- [CI/CD and release engineering](05-engineering/04-ci-cd-and-release-engineering.md)
- [Branch, PR, and review process](05-engineering/05-branch-pr-and-review-process.md)
- [Source generation and extension policy](05-engineering/06-source-generation-and-extension-policy.md)
- [ADRs and governance](05-engineering/07-adrs-and-governance.md)
- [Roadmap](06-delivery/01-roadmap.md)
- [MVP and release slices](06-delivery/02-mvp-and-release-slices.md)
- [Epics, stories, and acceptance](06-delivery/03-epics-stories-and-acceptance.md)
- [Session-sized implementation plan](06-delivery/04-session-sized-implementation-plan.md)
- [Risk register](06-delivery/05-risk-register.md)
- [Definition of Ready and Done](06-delivery/06-definition-of-ready-and-done.md)

### Guides, example, and agent operation
- [User quickstart](07-guides/01-user-quickstart.md)
- [Facilitator and domain expert guide](07-guides/02-facilitator-and-domain-expert-guide.md)
- [Designer and architect guide](07-guides/03-designer-and-architect-guide.md)
- [Engineer and validator guide](07-guides/04-engineer-and-validator-guide.md)
- [Administrator guide](07-guides/05-administrator-guide.md)
- [Training curriculum](07-guides/06-training-curriculum.md)
- [Point-of-sale walkthrough](07-guides/07-point-of-sale-walkthrough.md)
- [Worked POS model](08-example-pos/README.md)
- [Agent operating model](09-agent/01-agent-operating-model.md)
- [Progressive disclosure map](09-agent/02-progressive-disclosure-map.md)
- [Bootstrap goal prompt](09-agent/03-bootstrap-goal-prompt.md)
- [Session goal template](09-agent/04-session-goal-template.md)
- [Root AGENTS template](09-agent/AGENTS.md.template)

### Reference templates
- [ADR template](10-reference/ADR-TEMPLATE.md)
- [Feature definition template](10-reference/FEATURE-SPEC-TEMPLATE.md)
- [Scenario template](10-reference/SCENARIO-TEMPLATE.md)
- [Model review checklist](10-reference/MODEL-REVIEW-CHECKLIST.md)
- [Traceability matrix template](10-reference/TRACEABILITY-MATRIX-TEMPLATE.md)
- [Validation rule template](10-reference/VALIDATION-RULE-TEMPLATE.md)
- [Workshop template](10-reference/WORKSHOP-TEMPLATE.md)
- [Release baseline template](10-reference/RELEASE-BASELINE-TEMPLATE.md)
- [Model extension template](10-reference/MODEL-EXTENSION-TEMPLATE.md)
- [Repository bootstrap checklist](10-reference/repo-bootstrap-checklist.md)
- [External sources and standards](10-reference/sources.md)

### Portable contracts and examples
- [Schema and fixture guide](schemas/README.md)
- [Canonical project schema](schemas/project-builder-model.schema.json)
- [Change-set schema](schemas/project-builder-changeset.schema.json)
- [Projection schema](schemas/project-builder-projection.schema.json)
- [POS project fixture](schemas/pos-example.project-builder.json)
- [Example change set](schemas/example-change-set.json)
- [Example generated projection](schemas/example-projection.json)

### Diagrams and decisions
- [Standalone Mermaid diagram sources](diagrams/README.md)
- [Initial architecture decision records](11-decisions/README.md)

## Recommended repository adoption sequence

1. Copy this `docs` folder into the fresh repository unchanged.
2. Read the Vision, principles, Definition-Validated Development, canonical model, and Implementation Plan.
3. Copy `09-agent/AGENTS.md.template` to the repository root as `AGENTS.md`, then shorten or specialize only where the real repository requires it.
4. Dispatch the [bootstrap `/goal`](09-agent/03-bootstrap-goal-prompt.md).
5. Create `dogfood/project-builder-foundation.project-builder.json` from the schema-valid example and model the repository bootstrap behavior.
6. Execute Sessions A01 through A05, then proceed through the canonical model sessions in order.
7. Treat every implementation discovery as a model finding, decision, assumption, or source update rather than an undocumented code interpretation.
8. Establish the first internal baseline only after the repository, model fixture, and evidence command agree.

## Reading contract

These documents distinguish four kinds of statements:

- **Decision**: the team has selected a direction. Reversal requires an ADR or an explicit product decision.
- **Invariant**: a property that must remain true.
- **Assumption**: a current belief that requires validation.
- **Option**: a deliberately unresolved choice.

The application should eventually represent this distinction directly. The documentation uses it now so that prose does not quietly harden guesses into architecture.
