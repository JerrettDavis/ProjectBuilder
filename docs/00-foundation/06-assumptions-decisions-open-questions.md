# Assumptions, Decisions, and Open Questions

## Recorded decisions

| ID | Decision | Rationale |
|---|---|---|
| D-001 | Use one canonical typed model with many projections. | Prevents semantic drift across stories, diagrams, interfaces, architecture, and tests. |
| D-002 | Begin as a modular monolith. | Preserves transactional consistency and reduces premature distributed complexity. |
| D-003 | Use .NET 10, C# 14, and `.slnx`. | Matches the required platform and current LTS toolchain. |
| D-004 | Use a Blazor Web App with an Interactive WebAssembly studio. | Keeps most product code in C# while supporting client-side interaction and future offline work. |
| D-005 | Use PostgreSQL and EF Core 10. | Provides mature relational constraints plus JSONB for versioned type-specific model payloads. |
| D-006 | Store semantic model state separately from canvas layout and personal view state. | Visual rearrangement must not modify business meaning. |
| D-007 | Use append-only change sets plus current-state tables, not full event sourcing at launch. | Provides history and deterministic revisions without imposing replay semantics on every read and migration. |
| D-008 | Use deterministic guidance rules before agentic assistance. | Human workflows must remain complete, explainable, and testable. |
| D-009 | Build structured editors before the general canvas. | A reliable semantic kernel is more important than early visual spectacle. |
| D-010 | Treat code generation as a projection, not the source of truth. | Maintains inspectability and model authority. |
| D-011 | Aspire is local orchestration, not the production runtime architecture. | Prevents development tooling from dictating deployment topology. |
| D-012 | Accessibility target is WCAG 2.2 AA, including non-drag alternatives. | The product is an authoring tool and must be operable without precise pointer control. |

## Working assumptions

| ID | Assumption | Validation approach |
|---|---|---|
| A-001 | Users can understand the narrative hierarchy after a guided example. | Moderated onboarding test with founder, analyst, designer, and engineer profiles. |
| A-002 | A common kernel can represent business, human, software, and device workflows without becoming vague. | Model POS, approval workflow, data pipeline, CLI tool, and device interaction. |
| A-003 | SVG is sufficient for early canvas scale and accessibility. | Performance spike at 1,000 and 5,000 visible elements, with interaction latency measurements. |
| A-004 | Command-level optimistic concurrency is adequate before CRDT adoption. | Multi-user editing study and conflict telemetry. |
| A-005 | PostgreSQL adjacency tables plus indexed JSONB are sufficient for model queries. | Query benchmark against representative project sizes. |
| A-006 | Generated specifications and scaffolds provide value before executable visual programming. | Measure whether generated work packages reduce clarification and rework. |
| A-007 | Users will accept explicit unknowns and evidence statuses instead of a simple completeness score. | Usability tests focused on model review and release decisions. |
| A-008 | A thin JavaScript interop layer is enough for browser APIs and high-frequency pointer handling. | Prototype pointer capture, clipboard, text measurement, resize observation, and file access. |

## Open product questions

1. What is the smallest vocabulary a non-technical user can adopt without losing the distinctions needed by engineers?
2. Should "Episode" appear in the default interface, or should the novice label be "Journey" while the stored type remains Episode?
3. Which modeling purposes require formal approval versus lightweight review?
4. How should the product represent conflicting stakeholder definitions without forcing early resolution?
5. How much of the meta-model can administrators extend without undermining interoperability?
6. What does "ready for implementation" mean for different project types?
7. Which evidence can be ingested automatically from source control and CI without creating noisy or misleading links?
8. How should a model connect to existing code when the code predates Project Builder?
9. When does a project become large enough to require multiple bounded contexts and model packages?
10. Which generated artifacts should be round-trippable, and which must remain one-way projections?

## Open technical questions

1. Custom SVG canvas versus a third-party diagramming foundation.
2. Interactive WebAssembly only for the studio versus mixed Interactive Auto rendering.
3. EF Core table-per-type, table-per-hierarchy, or explicit element table with typed JSON payloads.
4. Node-level versus scene-level aggregate boundaries for transactional edits.
5. Server-generated versus client-generated time-ordered identifiers while offline.
6. Whether free-text collaborative editing needs a specialized CRDT before graph operations do.
7. PostgreSQL full-text search versus a separate search service at scale.
8. How to package executable projection plugins without allowing arbitrary unsafe code in the server process.
9. Whether generated C# should use partial types, standalone projects, or both.
10. How to represent formal logic gradually without making ordinary scenarios hostile to non-specialists.

## Decision discipline

An open question is not permission to let implementation choose accidentally. Before a slice depends on one of these choices, the team must:

1. state the scenario that creates the need,
2. identify measurable decision criteria,
3. run the smallest useful experiment,
4. record the evidence,
5. write or update an ADR,
6. update the dogfood model.
