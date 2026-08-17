# Risk Register

## Operating model

Risks are reviewed at least once per release slice and whenever a relevant model claim changes. Probability and impact use Low, Medium, High, and Critical. Each risk has an owner, leading indicators, prevention, contingency, and decision trigger. Accepted risk requires an explicit review record.

## Product and semantic risks

### R-001: the ontology becomes academic rather than useful

**Probability:** High  
**Impact:** Critical

**Description:** Users may be forced to learn an elaborate vocabulary before receiving value.

**Leading indicators:**

- workshops spend more time debating labels than behavior,
- users create generic placeholders to bypass fields,
- high abandonment in actor/scenario capture,
- facilitators maintain separate notes outside the product.

**Prevention:**

- plain-language prompts and domain aliases,
- progressive disclosure,
- examples before definitions,
- structured quick capture followed by refinement,
- usability testing with non-architect participants,
- allow Unknown and Deferred without fabricating completion.

**Contingency:** simplify the visible language while preserving typed internal distinctions; introduce profiles and role-specific lenses.

**Trigger:** fewer than 70 percent of observed participants can explain their first scenario without facilitator translation.

### R-002: the model becomes a universal graph with weak semantics

**Probability:** Medium  
**Impact:** Critical

**Description:** Generic nodes and edges can make rendering easy but prevent useful validation, projection, and execution.

**Prevention:**

- typed elements and relations,
- registry validation,
- explicit extension points,
- concrete vertical-slice tests,
- forbid arbitrary payloads as the primary domain model.

**Contingency:** freeze generic extension growth and migrate common patterns into first-class types.

### R-003: the model becomes too rigid for real domains

**Probability:** Medium  
**Impact:** High

**Description:** A fixed hierarchy may force unlike domains into one narrative form.

**Prevention:**

- containment for orientation, graph relations for semantics,
- aliases and purpose profiles,
- typed extensions,
- support system-only, human, device, and organizational behavior,
- validate against varied reference projects.

**Contingency:** add a versioned meta-model extension only after several concrete examples expose the same missing concept.

### R-004: completeness creates false confidence

**Probability:** High  
**Impact:** Critical

**Description:** A percentage can imply correctness despite unresolved assumptions or missing evidence.

**Prevention:**

- purpose-specific rule sets,
- blocking findings never hidden by scores,
- explicit knowledge states,
- visible rule version,
- evidence staleness,
- narrative explanation beside metrics.

**Contingency:** remove aggregate score from default view and retain a rule-status matrix.

### R-005: users model documentation after the fact

**Probability:** High  
**Impact:** High

**Description:** The model may become a compliance artifact disconnected from delivery.

**Prevention:**

- make model outputs directly useful for stories, tests, contracts, and reviews,
- integrate with development workflow,
- require dogfood updates in feature completion,
- surface implementation drift,
- minimize duplicate authoring.

**Contingency:** focus product scope on the strongest live-delivery use cases and stop low-value document projections.

### R-006: Project Builder cannot dogfood itself

**Probability:** Medium  
**Impact:** Critical

**Description:** Meta-model gaps or awkward workflows may make the product unsuitable for its own development.

**Prevention:** begin dogfood fixture in Phase 0; model each feature before or during implementation; track every exception.

**Contingency:** treat exceptions as first-class findings, not private spreadsheets. Do not declare the kernel stable while critical exceptions remain unexplained.

## Experience risks

### R-010: the product feels like a long wizard

**Probability:** High  
**Impact:** High

**Prevention:** Guide Rail is optional and contextual; Studio is always available; prompts preserve place; users can branch, defer, search, and edit directly.

**Trigger:** users repeatedly close the guide and cannot locate the corresponding structured editor.

### R-011: the product feels like an unstructured diagram tool

**Probability:** Medium  
**Impact:** High

**Prevention:** typed insertion, semantic connectors, Inspector validation, model-backed layouts, structured editors, purpose profiles.

### R-012: canvas accessibility is inadequate

**Probability:** High  
**Impact:** Critical

**Prevention:**

- semantic outline,
- keyboard command parity,
- no drag-only operation,
- focus and announcement design,
- zoom-independent labels,
- reduced motion,
- early assistive-technology testing.

**Contingency:** make structured and table views complete alternatives, not secondary fallbacks.

### R-013: visual scale becomes unmanageable

**Probability:** High  
**Impact:** High

**Prevention:** scopes, drilldown, filters, lenses, clustering, saved views, virtualized outlines, reference-size performance tests.

**Trigger:** median task requires navigating more than 150 simultaneously rendered semantic nodes.

### R-014: guidance becomes repetitive or patronizing

**Probability:** Medium  
**Impact:** Medium

**Prevention:** role and proficiency settings, concise rationale, skip/defer, remember demonstrated knowledge, no celebratory friction.

## Architecture and data risks

### R-020: premature microservices increase delivery cost

**Probability:** Medium  
**Impact:** High

**Prevention:** modular monolith, measured extraction criteria, ports at real external boundaries, deployment-independent module design.

### R-021: event sourcing complexity overwhelms the product

**Probability:** Medium  
**Impact:** High

**Description:** Full event sourcing may impose replay, upcasting, temporal-query, and debugging obligations before product value is proven.

**Prevention:** append-only semantic change sets plus current-state tables and snapshots; keep operations explicit and replayable where useful.

**Contingency:** introduce event-sourced aggregates only for measured temporal use cases.

### R-022: semantic and visual state become entangled

**Probability:** Medium  
**Impact:** Critical

**Prevention:** separate storage, APIs, hashes, revisions, authorization, and events for model versus view state; property test that layout operations preserve semantic hash.

### R-023: flexible JSON payloads become an unqueryable data swamp

**Probability:** Medium  
**Impact:** High

**Prevention:** normalize identity, kind, ownership, relations, revisions, and indexed fields; use typed domain payloads and versioned schemas; maintain query profiles and indexes.

### R-024: format migrations lose user work

**Probability:** Medium  
**Impact:** Critical

**Prevention:** versioned envelopes, fixtures for every supported version, transactional migrations, round-trip tests, backups, dry-run reports, unknown-extension policy.

### R-025: concurrent edits overwrite truth

**Probability:** High  
**Impact:** Critical

**Prevention:** expected revisions, idempotency, atomic commits, conflict report, private drafts, safe operation-level rebase, no last-write-wins semantic commits.

### R-026: source generators create hidden build fragility

**Probability:** Medium  
**Impact:** High

**Prevention:** self-contained analyzer packages, no hardcoded paths, deterministic incremental generators, readable output, compile tests, diagnostics for invalid declarations, ordinary runtime fallback where practical.

## Security and privacy risks

### R-030: cross-tenant data exposure

**Probability:** Low  
**Impact:** Critical

**Prevention:** tenant-scoped authorization at every application query and command, database constraints or policies where appropriate, opaque not-found behavior, cross-tenant adversarial tests, audit.

### R-031: malicious project import

**Probability:** High  
**Impact:** Critical

**Prevention:** strict size/depth/count limits, schema and semantic validation, no executable content, content sanitization, decompression limits, extension allowlist, transactional processing, fuzz tests.

### R-032: sensitive business architecture is exposed to providers

**Probability:** Medium  
**Impact:** Critical

**Prevention:** classification policy, encryption, provider-neutral agent gateway, explicit consent, redaction, local or private options, no agent dependency, detailed audit.

### R-033: attachment or projection content causes active-content attacks

**Probability:** Medium  
**Impact:** High

**Prevention:** content-type validation, malware scanning, isolated storage, safe download headers, HTML/SVG sanitization, no direct execution, Content Security Policy.

### R-034: authorization is enforced only in UI

**Probability:** Medium  
**Impact:** Critical

**Prevention:** application and API policy enforcement, denial tests, no client-trusted workspace IDs, policy review.

## Delivery and operations risks

### R-040: initial scope is too broad

**Probability:** High  
**Impact:** Critical

**Prevention:** POS item-scan reference slice, release gates, explicit non-goals, structured editors before broad canvas, no agent or code generation in MVP.

**Trigger:** more than two consecutive sessions produce infrastructure without a new user-observable behavior.

### R-041: excessive project and abstraction count slows contributors

**Probability:** Medium  
**Impact:** High

**Prevention:** assembly boundary justification rule, feature folders, no generic service layers, architecture review.

### R-042: browser rendering cannot support representative projects

**Probability:** Medium  
**Impact:** High

**Prevention:** published reference models, projection caching, viewport culling, batched renders, performance telemetry, replaceable renderer.

### R-043: collaboration infrastructure arrives before editing semantics

**Probability:** Medium  
**Impact:** Medium

**Prevention:** private drafts and optimistic concurrency first; comments and presence before general co-editing; CRDT only after measured need.

### R-044: backup exists but restore does not work

**Probability:** Medium  
**Impact:** Critical

**Prevention:** automated restore tests, scheduled rehearsals, recovery-point and recovery-time objectives, artifact integrity checks.

### R-045: third-party dependencies constrain commercial use or distribution

**Probability:** Medium  
**Impact:** High

**Prevention:** software bill of materials, license allowlist, dependency review, avoid restrictive runtime/editor dependencies, documented replacement seams.

### R-046: tests prove implementation details rather than product truth

**Probability:** High  
**Impact:** High

**Prevention:** claim-linked scenarios, semantic results, property and contract tests, minimal interaction mocks, model-based test binding.

## Adoption and business risks

### R-050: teams cannot justify the modeling cost

**Probability:** High  
**Impact:** Critical

**Prevention:** time-to-first-useful-artifact metric, direct issue/test/contract outputs, templates, incremental adoption, retrospective defect and rework measurement.

### R-051: product is mistaken for Figma, BPMN, UML, or a ticket system

**Probability:** High  
**Impact:** Medium

**Prevention:** clear positioning: definition and traceability studio with interface and architecture lenses; document integrations and boundaries; do not chase visual parity for its own sake.

### R-052: generated artifacts create ownership confusion

**Probability:** Medium  
**Impact:** High

**Prevention:** canonical source markers, generated/user-owned boundaries, regeneration preview, projection versioning, no silent overwrite.

## Risk review template

```markdown
### Review date
YYYY-MM-DD

### Changed model claims
...

### New or changed risks
...

### Leading indicators
...

### Mitigations completed
...

### Accepted risks
...

### Decisions triggered
...
```
