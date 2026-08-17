# MVP and Release Slices

## MVP thesis

The first meaningful product is not a generic diagram editor and not a code generator. It is a guided, revisioned definition studio that can carry one real interaction from actor intent to interface behavior, domain transition, external boundary, failure paths, and evidence.

The point-of-sale item-scan slice is the reference journey because it contains:

- a human actor,
- a device actor,
- a graphical interface,
- an application intent,
- classification and decision logic,
- an external corporate price book,
- store-specific context,
- domain state,
- failure and degraded paths,
- latency and availability concerns,
- security and audit concerns,
- enough behavior to demonstrate vertical slicing.

## Release 0.0: repository bootstrap

### Audience

Contributors.

### Included

- solution scaffolding,
- build and package governance,
- local orchestration,
- persistence resource,
- health and telemetry,
- architecture checks,
- test harness,
- initial schema,
- dogfood fixture,
- documentation and agent instructions.

### Excluded

- production authentication,
- modeling UI,
- collaboration,
- code generation.

### Release proof

Clean clone, build, test, run, schema validation, architectural dependency proof.

## Release 0.1: modeling kernel

### Audience

Developers and internal modelers.

### Included

- project creation,
- actors and outcomes,
- narrative hierarchy,
- state, facts, rules, invariants, transitions,
- paths and semantic results,
- typed relations,
- change sets and revisions,
- deterministic JSON import/export,
- command-line or test-harness access.

### Excluded

- polished studio,
- canvas,
- interface design,
- multi-user collaboration.

### Release proof

The POS item-scan model can be built through application commands and exported. Invalid models produce stable validation findings.

## Release 0.2: structured studio

### Audience

Internal product team and design partners.

### Included

- project dashboard,
- explorer,
- typed structured editors,
- Inspector,
- Problems panel,
- purpose profiles,
- search,
- revision history,
- commit preview,
- autosaved draft,
- full keyboard route.

### Excluded

- freeform canvas,
- interface mockups,
- realtime co-editing,
- source generation.

### Release proof

A domain expert completes the POS capture without writing JSON or code.

## Release 0.3: guided modeling

### Audience

Facilitators, product owners, analysts, domain experts.

### Included

- Guide Rail,
- prompt registry,
- Answer, Unknown, Assumed, Deferred, Disputed, Not Applicable paths,
- suggested next action,
- gap map,
- workshop mode,
- guided review,
- contextual learning links.

### Excluded

- agent-generated answers,
- generic project templates marketplace.

### Release proof

A first-time user can create a coherent discovery model with no hidden mandatory fields and can explain every unresolved gap.

## Release 0.4: visual lenses

### Audience

All design-partner roles.

### Included

- Story Map,
- Scenario Flow,
- State and Rule,
- System Context,
- Traceability lenses,
- accessible canvas,
- drilldown,
- saved layouts,
- semantic outline,
- overlays and scenario stepping,
- lens-to-lens synchronization.

### Excluded

- graphical UI designer,
- WebGL optimization unless evidence demands it,
- simultaneous text editing.

### Release proof

The same POS claim can be edited in one lens and correctly inspected in all others. View movement does not produce semantic change.

## Release 0.5: interface designer

### Audience

Product designers, architects, engineers.

### Included

- interface classification,
- graphical frame/control designer,
- CLI/API/event/MCP/device/document/human-procedure editors,
- visible state,
- intent bindings,
- error and edge states,
- scenario-on-interface playback,
- accessibility constraints.

### Excluded

- pixel-perfect design-system replacement,
- production HTML or mobile code generation,
- arbitrary Figma file parity.

### Release proof

The complete item-scan interaction can be played over scanner and POS interface states, including unknown item, offline price book, duplicate scan, and prohibited item.

## Release 0.6: architecture and boundaries

### Audience

Architects, senior engineers, security, operations.

### Included

- systems and components,
- boundary classifications,
- contracts,
- ownership and trust,
- availability, latency, consistency, retry, idempotency, recovery,
- vertical-slice projection,
- threat and operations overlays,
- architecture prompts.

### Excluded

- automatic cloud provisioning,
- independent microservices,
- vendor marketplace.

### Release proof

The POS scan traces into a complete implementation slice and exposes every external or operational assumption.

## Release 0.7: specification and evidence

### Audience

Engineers, testers, reviewers, auditors.

### Included

- behavioral specification output,
- example and decision tables,
- transition tables,
- contract projections,
- property-test candidates,
- acceptance and traceability matrices,
- evidence records and verification results,
- baselines and review packets.

### Excluded

- broad production code generation,
- agent-only test authoring.

### Release proof

A team implements one Project Builder slice from the generated definition packet and attaches automated evidence to each required claim.

## Release 0.8: collaboration and administration

### Audience

Teams and organizations.

### Included

- workspaces,
- roles,
- review and approval,
- comments,
- presence,
- optimistic conflict handling,
- audit,
- retention,
- backup/restore,
- project templates,
- organization policies.

### Excluded

- general CRDT editor,
- anonymous public editing.

### Release proof

Two editors can work safely, stale changes never overwrite committed truth, and an administrator can restore a project to a verified point.

## Release 0.9: dogfood beta

### Audience

Design partners and internal teams.

### Included

- complete self-model for the beta scope,
- model-to-repository traceability,
- generated registries or tests for selected internal capabilities,
- onboarding curriculum,
- import/export compatibility policy,
- security and accessibility hardening,
- scale and resilience testing.

### Release proof

Project Builder's own beta release packet is generated and reviewed from Project Builder.

## Version 1.0

### Product promise

A team can collaboratively define a software or socio-technical system from actor outcomes through interface behavior, domain rules, system boundaries, implementation slices, and evidence. The model is revisioned, reviewable, portable, accessible, and useful without an agent.

### Required feature set

- Releases 0.1 through 0.9 at supported quality.
- Stable v1 project format and migration policy.
- Published deployment and backup procedures.
- Published performance envelope.
- Security review and ASVS coverage.
- WCAG 2.2 AA target evidence for primary flows.
- Tenant isolation and audit.
- Deterministic projections.
- Dogfood baseline and release evidence.
- Support and incident procedures.

## Explicitly later than 1.0

- Full low-code runtime.
- Round-trip production code editing.
- General plugin marketplace.
- Native mobile design and generation.
- Automated infrastructure deployment.
- Broad AI assistant.
- CRDT-based unrestricted simultaneous diagram editing.
- Domain-specific template marketplace.
- Full Figma import/export compatibility.

## Feature slicing rule

No release slice may add a concept only to the data model. Each accepted slice includes:

1. Domain semantics and invalid states.
2. Application command/query behavior.
3. Persistence and migration.
4. API contract.
5. Structured or visual user interaction.
6. Authorization.
7. Validation and problems.
8. Import/export impact.
9. Telemetry.
10. Automated evidence.
11. Documentation and training impact.
12. Dogfood-model update.

## Representative slice card

### Slice: classify a scanned token

**Outcome:** A clerk sees the correct next action after a scanner emits a token.

**Model definition:**

- actors: Clerk, Scanner, Classification Policy,
- trigger: ScannerTokenCaptured,
- state: ActiveTransaction,
- rule: TokenClassification,
- results: ProductCode, PaymentToken, CouponCode, SpecialCode, Unrecognized,
- invariant: classification never changes transaction state,
- path: classification timeout,
- evidence: example table and property test.

**Implementation:**

- device adapter receives token,
- Presentation translates signal to intent,
- Application invokes classification use case,
- Domain classifies normalized token,
- Application routes semantic result,
- Infrastructure is not involved unless the configured policy needs an external lookup,
- UI renders the next observable state.

**Release proof:**

- example tests for all supported classes,
- property that malformed tokens never mutate transaction,
- browser or device simulator run,
- trace from scenario step to test result.

## Release readiness matrix

| Concern | Preview | 1.0 |
|---|---:|---:|
| Model migrations | best effort with documented reset options | supported versioned migrations |
| Import/export | deterministic current format | stable v1 compatibility contract |
| Authentication | local/dev and selected provider | documented production providers |
| Tenant isolation | tested for design partners | formally reviewed and continuously tested |
| Accessibility | primary keyboard paths | WCAG target evidence for primary flows |
| Backup/restore | scripted | rehearsed and documented |
| Performance | measured reference projects | published envelope and regression gates |
| Collaboration | comments/presence/conflict warning | safe edit, review, audit, recovery |
| Agent assistance | absent or lab-only | optional, policy controlled, not required |
| Support | project team | defined support and incident process |
