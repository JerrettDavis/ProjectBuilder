# Product Requirements

## Product objective

Project Builder must help a person transform an incompletely understood domain into a progressively formal, reviewable, and implementable model. The product should feel like a guided studio rather than a survey. A user can follow a recommended path, move freely among lenses, defer unknowns, and return to gaps without losing the story of the system.

## Functional requirement groups

### PR-01: Project framing

The product shall allow a user to:

- create a project inside a workspace,
- state the project intent in ordinary language,
- identify the desired outcome and beneficiaries,
- declare included and excluded scope,
- record constraints, assumptions, authorities, and source material,
- select a starting template or begin empty,
- choose a modeling purpose, such as discovery, interface design, architecture, implementation planning, or validation,
- establish a baseline and review status.

Acceptance signals:

- A project cannot be mistaken for a generic folder. It has purpose, ownership, and scope.
- Unknown scope is visible rather than represented by empty text.
- Templates populate suggestions and examples, not unreviewed facts.

### PR-02: Actor and participant discovery

The product shall support human, organization, system, service, device, scheduled, and external-provider participants.

For each actor, the author can record:

- role and responsibility,
- goals and incentives,
- authority,
- knowledge,
- channels and interfaces,
- accessibility or environmental constraints,
- relationships to other actors,
- contexts in which the role is valid,
- evidence or authority for the definition.

The product shall distinguish a role from a named person and a persona from an actor.

### PR-03: Narrative decomposition

The product shall support:

- capability,
- episode,
- scenario,
- scene,
- interaction,
- step,
- intent,
- observation,
- outcome,
- path classification.

The author can begin with any known level, but the system prompts for missing parents or context as the model matures.

Every scenario must be able to express:

- trigger,
- participants,
- preconditions,
- starting facts,
- ordered scenes,
- expected outcome,
- alternate and exceptional paths,
- unresolved branches,
- evidence expectations.

### PR-04: State and rule modeling

The product shall allow authors to define:

- domain facts and state,
- application workflow state,
- presentation state,
- infrastructure observations,
- commands and events,
- transitions,
- preconditions and postconditions,
- rules, calculations, decisions, and policies,
- invariants and broader properties,
- temporal conditions and deadlines,
- ownership and authority.

The system shall prevent model elements from silently crossing state categories.

### PR-05: Path and failure modeling

At every behavior level, the product shall prompt for:

- invalid input,
- denied authority,
- missing data,
- duplicate request,
- unavailable dependency,
- timeout,
- cancellation,
- partial completion,
- stale data,
- conflict,
- retry,
- compensation,
- recovery,
- operator intervention.

The author can mark a path not applicable with rationale or deferred with consequence and owner.

### PR-06: Boundary and system modeling

The product shall represent boundaries for:

- ownership,
- trust,
- transaction,
- process,
- deployment,
- protocol,
- data residency,
- vendor control,
- operational responsibility.

A boundary crossing can reference:

- interface and contract,
- data,
- authorization,
- latency and availability expectations,
- idempotency and retry behavior,
- privacy classification,
- audit requirement,
- failure presentation,
- evidence.

### PR-07: Lens and canvas system

The product shall provide coordinated lenses over the same model:

1. Story Map.
2. Scenario Flow.
3. State and Rule.
4. Interface.
5. System Context.
6. Data and Contract.
7. Decision and Risk.
8. Traceability and Evidence.
9. Implementation Slice.

A user can switch lenses without manually recreating elements. Lens-specific layout is stored separately from semantics.

### PR-08: Guided modeling

The guidance system shall:

- recommend a next question based on selected model context,
- explain why it matters,
- show which rule produced the prompt,
- allow Answer, Link Existing, Unknown, Assumed, Not Applicable, and Defer,
- avoid blocking exploration for non-critical gaps,
- distinguish structural errors from completeness suggestions,
- show progress by coverage category and purpose,
- preserve history of resolved and reopened gaps.

### PR-09: Interface design

The interface designer shall support:

- graphical UI,
- CLI,
- HTTP or RPC API,
- event or message interface,
- MCP tools, resources, and prompts,
- device interface,
- document or form,
- human procedure.

Common concepts include:

- visible or exposed state,
- accepted intents,
- observations and errors,
- authorization,
- validation,
- sequencing,
- quality expectations,
- evidence.

Graphical interfaces additionally support frames, layout, controls, bindings, responsive states, focus order, keyboard interactions, and scenario overlays.

### PR-10: Validation and evidence

The product shall connect model claims to evidence types:

- examples,
- scenario tests,
- property tests,
- contract tests,
- integration tests,
- end-to-end tests,
- accessibility reviews,
- threat reviews,
- performance experiments,
- operational rehearsals,
- human approval,
- external standards or documents.

Evidence has status, source, revision, freshness, and scope. A stale passing test cannot silently prove a changed claim.

### PR-11: Projection and generation

The product shall generate deterministic, attributable artifacts including:

- narrative specifications,
- TinyBDD-style or Gherkin-like scenarios,
- state transition tables,
- interface contracts,
- OpenAPI fragments,
- event schemas,
- traceability matrices,
- test plans,
- vertical-slice implementation plans,
- C# scaffolding,
- decision records,
- review packets.

Generated artifacts identify source project revision and model element identifiers.

### PR-12: History and collaboration

The product shall support:

- atomic change sets,
- revision history,
- diff and impact analysis,
- comments and discussions,
- review requests,
- approvals and waivers,
- named baselines,
- presence and selection awareness,
- conflict detection,
- export and import,
- audit trail.

No collaboration feature may sacrifice deterministic model state or authorship.

### PR-13: Search and navigation

Users shall be able to:

- search by text, type, tag, status, actor, boundary, and relation,
- navigate inbound and outbound references,
- open a model element in any relevant lens,
- locate unresolved gaps,
- follow traceability from outcome to evidence,
- save queries and filtered views,
- copy stable deep links.

### PR-14: Administration and governance

Workspace administrators can manage:

- members and roles,
- authentication and SSO configuration,
- project templates,
- validation profiles,
- evidence policies,
- retention and export,
- integration credentials,
- extensions,
- audit access.

Governance policy can strengthen requirements but cannot silently weaken model semantics.

## Non-functional requirements

### Usability
- A first-time user can complete the guided POS item-scan tutorial in under 45 minutes.
- Common edits provide immediate local feedback.
- The product never requires knowledge of architecture vocabulary to begin.
- Advanced concepts remain discoverable and explainable.

### Accessibility
- Target WCAG 2.2 AA.
- All core workflows are keyboard-operable.
- Drag operations have non-drag alternatives.
- Structured representations expose canvas content to assistive technology.
- Focus is visible, stable, and never moved merely because background data refreshed.

### Performance
Initial target budgets:

- Application shell interactive in under 3 seconds on a typical business laptop after warm cache.
- Project dashboard queries p95 under 500 ms for ordinary projects.
- Model command acknowledgment p95 under 300 ms excluding external imports.
- Canvas input-to-feedback under 50 ms for ordinary graphs.
- Search p95 under 750 ms for a 50,000-element workspace.
- Import of a 10,000-element project under 30 seconds with progress and cancellation.

Budgets are hypotheses until measured against representative hardware and data.

### Reliability
- Committed change sets are atomic.
- No acknowledged semantic edit is lost.
- Import failure cannot leave partial project state.
- Background projections are retryable and idempotent.
- Backups and restore are rehearsed.
- Every release includes migration and rollback procedures.

### Security and privacy
- Browser authentication uses secure same-origin cookies by default.
- Authorization is enforced server-side for every command and query.
- Sensitive content is classified and protected in logs, exports, and integrations.
- Audit records are tamper-evident within the application's threat model.
- Workspace data is isolated.
- Agent integrations are opt-in and least-privileged.

### Interoperability
- The canonical project format is versioned and documented.
- Exports are deterministic.
- Unknown extension data can be preserved when safe.
- Open standards are preferred for contracts and telemetry.
- Public APIs are described with OpenAPI.

### Maintainability
- Domain code has no EF Core, ASP.NET Core, browser, or provider dependencies.
- Build and test commands are deterministic.
- Package versions are centralized.
- Architecture boundaries are test-enforced.
- Generated code is inspectable and reproducible.
