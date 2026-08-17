# Principles and Non-Negotiables

## 1. One model, many lenses

Story maps, process diagrams, UI frames, system maps, state diagrams, test plans, and generated code are projections of one model. The product must not make users reconcile duplicate copies of the same fact.

A lens may hide detail, change layout, or add derived annotations. It may not create an independent semantic truth without making the divergence explicit.

## 2. Reality before machinery

The product begins with the reality being modeled: people, goals, facts, rules, consequences, and constraints. Frameworks, services, queues, databases, and deployment units enter only when the model reaches a boundary that requires an implementation decision.

An external system can supply a domain fact. The fact remains part of the domain model; the means by which it is obtained belongs to infrastructure.

## 3. Definitions are claims

A requirement, invariant, path, contract, or architectural decision is a claim about the system. Claims should state their scope, authority, status, and expected evidence. "Complete" is not a feeling. It is the absence of material unresolved claims for a declared purpose and review baseline.

## 4. Unknown is a valid value

The interface must allow:

- Unknown.
- Not yet investigated.
- Not applicable, with rationale.
- Assumed, with owner and review date.
- Contested.
- Deferred, with consequence.
- Superseded.

Blank fields cannot safely carry these meanings.

## 5. Guidance is inspectable and deterministic

The wizard is backed by versioned rules and prompts. A user can see why a prompt appeared, which model fact triggered it, and what accepting or deferring it will do.

Agentic suggestions may supplement the guidance engine. They do not replace it.

## 6. Human completion is always possible

Every essential operation has a human-authored path. An agent can draft, compare, summarize, or propose. The user can perform the same operation manually, inspect the proposed change set, and reject it without losing progress.

## 7. Hierarchy for orientation, graphs for truth

Containment provides a navigable narrative:

`Project → Context → Capability → Episode → Scenario → Scene → Interaction → Step`

Real systems are not trees. Actors participate in many scenarios, rules constrain many transitions, interfaces expose many interactions, and evidence can validate many claims. These relationships are represented as typed graph edges.

## 8. State is explicit

The product distinguishes:

- domain state,
- application workflow state,
- presentation state,
- canvas and editor state,
- infrastructure state,
- observed external facts.

No UI tab selection becomes a domain fact merely because it is visible. No database column becomes a domain concept merely because it persists data.

## 9. Behavior precedes structure

Architecture must be earned by behavior, boundaries, quality needs, and operational constraints. A team may record a provisional architecture, but the product marks it as an assumption until the model supplies a reason.

## 10. Vertical slices preserve causality

When the model reaches implementation, it projects a complete path from an initiating interface through application orchestration, domain behavior, infrastructure effects, observations, and evidence. Layers remain distinct, but work is delivered by behavior rather than horizontal technical batches.

## 11. Validation is broader than examples

Examples make behavior understandable. Properties search a wider input space. Contracts validate boundaries. Integration tests validate assembly. End-to-end tests validate observable effects. Operational evidence validates real behavior. The product should preserve the role and limitation of each proof.

## 12. Generated artifacts are readable projections

Generated C#, schemas, specifications, diagrams, and tests must be deterministic, inspectable, and attributable to model elements. Generation must not create an opaque second application that only the generator understands.

## 13. Collaboration never obscures authorship

Every committed change set records who or what proposed it, who accepted it, its base revision, its reason, and its effects. Concurrent editing may be convenient, but auditability is not optional.

## 14. Accessibility is architectural

The canvas cannot be the only path to understanding or editing the model. Every visual operation requires a structured representation, keyboard operation, and meaningful accessible name. Dragging is an enhancement, not a prerequisite.

## 15. The modular monolith is the default

Project Builder is a deeply connected model system. Early distribution would turn internal consistency into network coordination before the domain is stable. Start with explicit modules and dependency boundaries inside one deployable application. Extract services only after measured scaling, isolation, or ownership needs justify the cost.

## 16. No silent repair

Import, migration, validation, or agent assistance must not silently change semantic content. Safe normalization, such as deterministic ordering, can occur automatically. Semantic repair is proposed as a reviewable change set.

## 17. No false completeness score

A single percentage can make an incomplete model look authoritative. Project Builder reports coverage by declared purpose and category, shows material gaps, and distinguishes verified, defined, assumed, unknown, and inapplicable items.

## 18. Dogfooding is a release gate

The application must model its own shipped behavior. A feature that cannot be represented exposes either a product gap or a poorly understood implementation. Both are useful findings and both must be resolved or explicitly accepted.
