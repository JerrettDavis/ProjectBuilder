# Project Builder

Project Builder is a definition-first studio for turning a domain into an inspectable, testable, and eventually executable system model. Its persistent, theme-aware shell organizes delivered behavior around Purpose → Context → Behavior → State → Recovery → Evidence rather than generic CRUD screens. A project opens into an outcome cockpit, a Semantic Explorer, typed Actor/Outcome editors, a Scenario Flow composer, a State Lens, a visual Recovery lens, a purpose-relative gap map, a Problems/Evidence workbench, an operation-oriented revision timeline, and a contextual Guide Rail. The Guide Rail deterministically explains the next question, supports non-linear stage navigation, and preserves versioned browser-local prompt choices across close, reopen, and reload without treating them as canonical truth. Its guided framing studio lets a novice describe participant identity, intent, authority, constraints, beneficiary value, and observable success in plain language. Its guided Scenario flow turns existing participants and value into starting facts, trigger, outcome boundary, Scene, directed Interaction, Intent, Step, Observation, and explicit semantic results while a live seven-node flowboard and equivalent structured outline remain synchronized. Both guided flows preview and commit the same typed expected-revision operations used by the full Studio editors. Recovery authoring connects condition, ordered branch behavior, participant observation, terminal result, effect, owned recovery, and operational closure as a typed topology with an equivalent structured outline. Every delivered typed editor preserves browser-local drafts across refresh, supports bounded undo/redo and scoped keyboard commit, and blocks stale-base commits without treating presentation state as semantic truth. Contributors can inspect reason, author, revision transition, and deterministic operations after commit. Every accepted transition is one atomic change set. Supported Actor/Outcome models import and export as canonical JSON. Accessible Blazor interfaces and Minimal API contracts use the same application transitions and PostgreSQL persistence.

The Decision Lens ranks one next action and inspectable alternatives from the canonical revision, purpose profile, versioned findings, dependency gates, and bounded recent-work continuity. It explains every signal, never presents a completeness percentage, and shares its server-owned `builtin/1` projection with the project overview.

Workshop Studio composes a deterministic 65-minute facilitation runway from current purpose, participants, findings, and recommendations. Facilitator progress, parking-lot threads, and Decision/Assumption/Question notes recover locally against the matching `workshop/1` brief and revision; they remain explicitly provisional until an owned semantic command exists. Participant view removes facilitator controls and working notes, while the export preserves both source brief and provisional session attribution.

The Story Map follows explicit value into behavior through Outcome, Capability, Episode, Scenario, Scene, and participating Actor bands. Its embedded capability deck commits an outcome-linked semantic ability through the same expected-revision pipeline as other editors. Priority and knowledge overlays, selection, keyboard focus, and layout are projection state; missing capabilities remain visible diagnostics rather than inferred workflow truth. Persisted relations, canonical containment, and traces derived from explicit references declare distinct connector provenance in `story-map/1`.

Scenario Flow drills from a Story Map scenario into participant lanes, ordered interaction cards, exceptional and recovery routes, explicit external-interaction boundary effects, terminal results, and read-only playback. The `scenario-flow/1` response distinguishes semantic elements from fragments derived from explicit canonical fields. Its live explanation overlay synchronizes path-level before/after state, changed facts, participant observation, and explicitly linked invariants; playback stops for human review without pretending to execute or evaluate the invariant. Playback, selected path, current step, boundary visibility, and flow layout remain view state.

State and Rule drills from an explicit state definition into a causal before/transition/after graph, transition matrix, rule decision table, invariant proof panel, synchronized inspector, and deterministic structured equivalent. The `state-rule/1` projection preserves stable fact, rule, invariant, result, transition, and explicitly linked effect references while labeling predicate fragments as derived from authored fields. Representation and selection remain view state; missing Event definitions and effects remain diagnostics rather than invented behavior.

System Context composes an owned system, external system, intent-bearing interface, ownership/trust boundary, and governing contract as one expected-revision change set. Its `system-context/1` lens exposes ownership and trust overlays, contract-declared request/response movement, optional explicit crossing effects, synchronized inspection, and a keyboard-operable deterministic outline. Overlay, selection, and topology placement remain view state; the projection never infers data movement from prose.

The Traceability Atlas follows promised Outcomes through durable Claims to attributable Evidence. Its `traceability/1` views distinguish supported paths, missing-link debt, and evidence requiring review after a linked semantic change. The guided evidence deck commits Claim and Evidence together at one expected revision; producer names and summaries never become inferred test counts or proof status.

Lens Lab now exercises the shared accessible SVG canvas kernel over immutable `lens/1` data. Semantic frames, typed connectors, synchronized selection, a mini-map, bounded pan/zoom, Fit Scope, Fit Selection, and Across/Down alignment remain explicit presentation state. Pointer selection, pan, and wheel zoom have keyboard and visible-command equivalents, while the semantic outline remains a complete non-canvas path. A theme-aware View Memory dock persists separately versioned personal or team workspace layouts in PostgreSQL, restores them on reload, identifies stale semantic baselines, and resets to deterministic auto-layout without advancing semantic history. Enter or a visible inspector command opens a semantic-ID deep link with pinned parent context, explicit outside-scope stubs, and browser back/forward restoration.

## Prerequisites

- .NET SDK 10.0.303 (the pinned SDK permits later 10.0.3xx patches).
- [Aspire CLI](https://aspire.dev/get-started/install-cli/) 13.4.x for the one-command local environment.
- A running Docker-compatible container engine for the PostgreSQL development resource.

No global workload installation, production credential, or external database is required.

## Run the healthy shell

From the repository root, use one command:

```powershell
./eng/run.ps1
```

On POSIX shells:

```bash
bash ./eng/run.sh
```

Aspire prints the developer dashboard URL. Open it, select the `web` resource endpoint, and verify that `web` and `projectbuilder` are healthy. The shell links to `/health` (readiness, including PostgreSQL when configured) and `/alive` (process liveness).

Use `/projects/import` for the accessible canonical JSON workflow. Portability is currently fail-closed to Project, Actor, Outcome, and `benefitsFrom`; schema-valid content outside that live profile receives explicit compatibility findings before persistence. A native model in that same profile becomes exportable when a stored Outcome exactly represents its intended outcome.

## Build and evidence commands

Windows PowerShell:

```powershell
./eng/restore.ps1
./eng/build.ps1
./eng/test.ps1
./eng/e2e.ps1
./eng/verify.ps1
./eng/smoke-health.ps1 -BaseUrl http://localhost:5242
```

POSIX shell:

```bash
bash ./eng/restore.sh
bash ./eng/build.sh
bash ./eng/test.sh
bash ./eng/e2e.sh
bash ./eng/verify.sh
bash ./eng/smoke-health.sh http://localhost:5242
```

`verify` is the CI entry point. It pins CI-mode warnings, checks formatting, restores, builds, installs the pinned headless Chromium runtime under ignored repository artifacts, executes every NUnit test through Microsoft.Testing.Platform, emits TRX, browser screenshots, and Cobertura evidence under `artifacts`, audits vulnerable dependencies, and scans for high-confidence secret patterns. Browser scenarios use a real Kestrel host and real PostgreSQL container; they never inject global input or open a headed window. C06 Scenario authoring evidence is captured as states 59–63, C07 State authoring evidence as states 64–68, C08 Problems/Evidence evidence as states 69–75, C09 draft/history evidence as states 76–83, the Recovery lens as states 84–87, cross-editor keyboard/refresh recovery as states 88–93, C10 purpose-profile/gap-map comparison as states 94–97, governed gap disposition as states 98–101, the D01 deterministic Guide Rail registry as states 102–105, the D02 recoverable contextual rail as states 106–110, the D03 guided Actor/Outcome framing journey as states 111–116, the D04 guided POS item-scan Scenario journey as states 117–122, the D05 explainable recommendation journey as states 123–128, the D06 internal discovery workshop as states 129–135, the E01 immutable Lens Lab as states 136–141, the E02 outcome-centered Story Map as states 142–148, the E03 playable Scenario Flow as states 149–155, the E04 State and Rule lens as states 156–162, the E05 System Context journey as states 163–168, the E06 Traceability journey as states 169–176, the E07 canvas interaction kernel as states 177–181, E08 layout persistence as states 182–187, E09 drilldown/navigation as states 188–194, and E10 scenario overlay as states 195–200.

The executable BDD evidence for the responsive light/dark studio shell, outcome cockpit transitions, Semantic Explorer, typed editors, keyboard navigation, route recovery, semantic conflict recovery, API idempotency, the complete actor/outcome/narrative journey, typed state and recovery, change-set history, and canonical portability is written to `artifacts/e2e/foundation-journeys`. C02 captures cockpit states 31–36, C03 Explorer states 37–42, C04 actor-editor states 43–47, and C05 duplicate/outcome/update/conflict states 48–58. Screenshots are verification artifacts, not canonical model content.

Direct .NET equivalents:

```shell
dotnet restore ProjectBuilder.slnx
dotnet build ProjectBuilder.slnx --configuration Release --no-restore
dotnet test ProjectBuilder.slnx --configuration Release --no-build --no-restore
```

## Foundation boundaries

- `ProjectBuilder.Domain` is pure and has no framework, provider, filesystem, network, clock, or environment dependency.
- `Application` references owned `Domain` and `Contracts`; `Infrastructure` provides the EF Core/PostgreSQL mechanism; `Web` is the composition root.
- `Projections` consumes client-safe contracts and owns deterministic `lens/1`, `story-map/1`, `scenario-flow/1`, `state-rule/1`, `system-context/1`, and `traceability/1` canonicalizers plus fail-closed topology validation; it contains no layout, target execution, or semantic writes.
- `AppHost` is development orchestration only.
- `ProjectBuilder.Web.Client` is the scoped Studio interactivity boundary and references only client-safe `Contracts`. It hosts the Semantic Explorer and typed Actor/Outcome editors; semantic writes remain server-owned expected-revision change sets.
- Browser E2E now covers the C01 persistent shell through E09 canvas navigation and foundation behavior through B10. Lens Lab, Story Map, Scenario Flow, State and Rule, System Context, and Traceability render typed server-owned projections with diagnostics, provenance, inspectors, overlays/matrices/playback/impact, and structured keyboard paths. Lens Lab additionally proves shared SVG interaction, personal/team layout persistence, semantic-ID drilldown, pinned context, cross-scope stubs, deep links, and browser location history. View layout, viewport, selection, search, filters, overlays, chosen path, representation, and current playback step remain distinct from semantic truth. Cross-lens kernel adoption, multiple state facts/rules/transitions, Event definitions, capability update/removal, multi-scene/join/interaction commands, system-context update/removal and multi-interface commands, evidence addition/supersession/revocation, portable import/export of newer runtime definitions, canonical Decision/Assumption/Question commands, realtime workshop collaboration, arbitrary historical snapshot materialization, and semantic deletion remain deferred pending their owned transitions.

Start documentation at [docs/README.md](docs/README.md). The schema-valid dogfood baseline is [dogfood/project-builder-foundation.project-builder.json](dogfood/project-builder-foundation.project-builder.json), and current foundation findings are recorded in [docs/00-foundation/07-foundation-findings.md](docs/00-foundation/07-foundation-findings.md).
