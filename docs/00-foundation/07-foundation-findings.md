# Foundation Findings

## Evidence placeholder timestamp

- **Finding:** The project schema requires `producedAt` for evidence whose status is `planned`.
- **Impact:** A planned placeholder must carry a timestamp that describes fixture authorship rather than actual evidence production, which can be misread.
- **Current handling:** Foundation placeholders use the deterministic export timestamp and state the limitation explicitly.
- **Alternatives:** Make `producedAt` conditional on produced/passed/failed states, or add a distinct `plannedAt` field in a future schema revision.
- **Owner:** Canonical model and project-format owner (not yet assigned).

## Repository licensing and security contact

- **Decision:** Jerrett Davis (`@JerrettDavis`) is the initial repository owner and default code owner.
- **Finding:** A private security reporting contact and open-source license have not been selected.
- **Impact:** External contribution and redistribution must remain closed until the owner makes these governance decisions.
- **Current handling:** `CODEOWNERS` assigns `@JerrettDavis`; `LICENSE` and `SECURITY.md` retain explicit unresolved placeholders without inventing permissions or contact details.
- **Owner:** Jerrett Davis (`@JerrettDavis`).

## Local container runtime

- **Finding:** Docker was installed but its daemon was unavailable during initial foundation work.
- **Resolution:** The daemon was started non-interactively with the installed Docker Desktop CLI. Aspire then reported `postgres`, `projectbuilder`, and `web` healthy, and `eng/smoke-health.ps1` passed against the live Web resource.
- **Current handling:** Docker remains a documented prerequisite. Run and smoke commands fail explicitly when it is unavailable; no external database or production credential is used.
- **Status:** Resolved for the foundation baseline on 2026-08-15.

## Element name length compatibility

- **Finding:** The engineering standards describe a 200-character `ElementName` limit, while the versioned project schema permits 500 characters.
- **Impact:** Enforcing 200 in the canonical primitive would reject project documents that are valid under the current machine-readable contract.
- **Current handling:** Session B01 uses the schema-compatible limit of 500 Unicode code points through one explicit `ElementName.MaxLength` boundary. No schema was expanded or weakened.
- **Alternatives:** Align the prose standard to 500, revise the project format with an explicit compatibility decision, or introduce justified element-kind-specific limits.
- **Owner:** Jerrett Davis (`@JerrettDavis`) with the canonical model and project-format owner when assigned.
- **Status:** Open; resolve before declaring a stable 1.0 persistence contract.

## Development-only workspace authorization

- **Finding:** Production authentication, workspace membership, and role policy are not yet owned or implemented.
- **Impact:** Treating the local modeler identity as a production identity would bypass the required server-enforced tenant and membership boundary.
- **Current handling:** Project creation is allowed only when ASP.NET Core runs in `Development`, only for the stable local workspace identifier, and only through the server-side development access policy. Other environments receive a semantic denial.
- **Alternatives:** Implement the security/identity route with cookie authentication and persisted workspace membership, or keep deployment explicitly development-only.
- **Owner:** Jerrett Davis (`@JerrettDavis`) with the security/identity owner when assigned.
- **Status:** Open; production deployment is not authorized.

## Playwright compatibility-spike cache residue

- **Finding:** The first direct Playwright compatibility command used Playwright's default browser cache under `%LOCALAPPDATA%\ms-playwright` before repository-local browser storage was configured.
- **Impact:** That one compatibility invocation wrote browser binaries outside the repository contrary to the session's machine-safety boundary.
- **Current handling:** `eng/e2e` and `eng/verify` now set `PLAYWRIGHT_BROWSERS_PATH` to ignored `artifacts/playwright` before installation or execution. The external cache was not deleted without explicit authority.
- **Alternatives:** The machine owner may remove the identified Playwright cache, or retain it for other Playwright projects.
- **Owner:** Jerrett Davis (`@JerrettDavis`) for the machine-level cleanup decision.
- **Status:** Repository commands corrected; external cache cleanup remains unowned.

## PostgreSQL timestamp precision

- **Finding:** PostgreSQL `timestamp with time zone` preserves microseconds while .NET timestamps can expose 100-nanosecond precision.
- **Impact:** A creation response could differ from its persisted query or exact idempotent retry by one sub-microsecond digit.
- **Resolution:** The infrastructure clock truncates to PostgreSQL microsecond precision before the domain transition. The real API/PostgreSQL scenario asserts create, query, and retry equality.
- **Status:** Resolved in the PB-010.1 slice on 2026-08-15.

## Actor contextual-role format gap

- **Finding:** The runtime Actor definition requires a contextual-role statement distinct from its name and typed actor kind, while project format 1.0 has no `contextualRole` payload property.
- **Impact:** The runtime persists this semantic fact, but a future runtime-to-portable export cannot preserve it in the typed payload without an owned compatibility decision. The dogfood fixture continues to use the common element `description` and does not expand the schema.
- **Current handling:** B04 stores contextual role in PostgreSQL and exposes it through the application, API, and UI. Import/export mapping remains deferred.
- **Alternatives:** Add a versioned optional `contextualRole` property, define the common description as its canonical portable representation, or revise the actor meta-model.
- **Owner:** Jerrett Davis (`@JerrettDavis`) with the canonical model and project-format owner when assigned.
- **Status:** Open before actor import/export is implemented.

## Beneficiary representation

- **Decision:** Runtime truth stores one typed `benefitsFrom` relation from Actor to Outcome; it does not duplicate beneficiary identifiers inside the outcome aggregate.
- **Compatibility note:** Project format 1.0 currently projects beneficiaries as `outcome.payload.beneficiaryIds`. A future exporter must deterministically derive that array from relations, without creating a second canonical source.
- **Status:** B04 runtime behavior is delivered; portable export remains deferred.

## Narrative leaf representation in project format 1.0

- **Finding:** Project format 1.0 requires inline `intent` and allows inline `observation` text in an Interaction payload, while Intent and Observation also exist as generic first-class element kinds without typed payloads.
- **Impact:** A portable document representing first-class narrative leaves must repeat their text in the Interaction payload or lose schema-required interaction meaning. That conflicts with the one-canonical-fact direction if treated as two authored values.
- **Current handling:** The B05 runtime stores Intent and Observation only as child elements and derives the interaction read model from them. The schema-valid dogfood fixture contains matching projection text and records this limitation; no schema was expanded.
- **Alternatives:** Version the Interaction payload to reference intent/observation element IDs, add typed leaf payloads and define inline fields as derived compatibility projections, or remove first-class leaf kinds through an owned meta-model decision.
- **Owner:** Jerrett Davis (`@JerrettDavis`) with the canonical model and project-format owner when assigned.
- **Status:** Remains open; B10 rejects narrative live import rather than choosing among these compatibility policies.

## State-logic detail in project format 1.0

- **Finding:** The runtime B06 definitions type fact value/authority/mutability, transition source/trigger/target/result references, and semantic-result kind/meaning, while project format 1.0 assigns generic empty payloads to `factDefinition`, `transitionDefinition`, and `resultDefinition`.
- **Impact:** The dogfood document can name these elements and preserve a readable description, but a future runtime-to-portable export cannot round-trip every typed field without an owned format decision.
- **Current handling:** State, rule, and invariant use their existing typed schema payloads. Fact, transition, and result details remain readable in common descriptions; the schema was not expanded to make the B06 fixture appear more complete.
- **Alternatives:** Version typed payloads for those three element kinds, define a deterministic projection into existing relations/common fields, or revise the runtime meta-model through the compatibility process.
- **Owner:** Jerrett Davis (`@JerrettDavis`) with the canonical model and project-format owner when assigned.
- **Status:** Remains open; B10 rejects state/logic live import rather than dropping typed detail.

## Multi-element commit atomicity evidence

- **Finding:** The initial B05 handoff had success-path PostgreSQL evidence but no explicit proof that a database failure midway through a multi-element narrative insert rolls back every element and the project revision.
- **Resolution:** A real PostgreSQL integration test now induces a constraint failure during the narrative transaction and proves that no model element remains and the revision is unchanged.
- **Status:** Resolved during B06 on 2026-08-16.

## API status response isolation

- **Finding:** The HTML status-page re-execution middleware also handled `/api` failures, allowing a JSON API denial or not-found response to be replaced by an unrelated component-rendering error.
- **Resolution:** HTML status-page rewriting is now scoped away from `/api`; an end-to-end contract assertion proves that an unauthorized workspace retains its `403 project.denied` response.
- **Status:** Resolved during B06 on 2026-08-16.

## Path detail in project format 1.0

- **Finding:** The runtime B07 path definition types source Scenario/Transition, ordered segments, terminal Result, terminal state, observation, owner, reciprocal recovery link, recovery strategy, retry/idempotency, exit, reconciliation, and typed Condition/Effect references. Project format 1.0 preserves only path classification, condition text, result text, state effect, and one recovery path identifier; Condition and Effect payloads are generic.
- **Impact:** The dogfood document remains schema-valid and readable, but a future runtime-to-portable export cannot round-trip the complete B07 packet without an owned compatibility decision.
- **Current handling:** The portable path payload is the canonical recovery link and common descriptions preserve otherwise unsupported detail. A duplicate `recoversFrom` relation was deliberately not added. The runtime retains the complete typed packet in PostgreSQL.
- **Alternatives:** Version the path, condition, and effect payloads; define a deterministic projection into governed typed relations after B08; or narrow the runtime model through an explicit meta-model decision.
- **Owner:** Jerrett Davis (`@JerrettDavis`) with the canonical model and project-format owner when assigned.
- **Status:** Remains open; B10 rejects path live import rather than dropping typed recovery detail.

## Executable relation registry scope

- **Decision:** B08 makes only `benefitsFrom` executable because it is the sole relation currently created by an owned runtime behavior. The canonical meta-model's other relation families remain definitions, not permissive generic graph commands.
- **Current handling:** The static Domain registry exhaustively covers its relation-kind enum and declares endpoint kinds, direction, cardinality, endpoint uniqueness, ownership, deletion behavior, and cycle policy. Invalid endpoint and cardinality combinations fail with `PB-REF-002` or `PB-REF-003` before a committable relation can be constructed. PostgreSQL independently enforces the one-beneficiary-per-outcome cardinality.
- **Extension rule:** A future relation kind must arrive with its concrete actor outcome, typed endpoints, descriptor, validation, persistence behavior, query projection, and evidence; adding only an enum value makes registry initialization fail.
- **Status:** B08 scope delivered on 2026-08-16.

## Relation-descriptor portability gap

- **Finding:** Project format 1.0 can carry relation instances but has no versioned representation for relation descriptors such as allowed endpoint pairs, cardinality, ownership, or deletion behavior.
- **Impact:** The revision-8 dogfood model can represent the `benefitsFrom` instance and describe the delivered registry capability, but a portable file cannot independently reproduce or compare the executable descriptor registry.
- **Current handling:** Runtime descriptors remain typed Domain definitions and are projected through the stable model API and accessible overview. The schema was not expanded during B08.
- **Alternatives:** Add a versioned descriptor section to a future format, define descriptors as product/version metadata rather than project content, or expose a separate versioned registry contract.
- **Owner:** Jerrett Davis (`@JerrettDavis`) with the canonical model and project-format owner when assigned.
- **Status:** Remains open; B10 does not claim that portable relation instances reproduce the runtime descriptor registry.

## Local Aspire diagnostic output

- **Finding:** Aspire's detached CLI mode writes its own operational log under the invoking user's `.aspire` directory, and the full JSON form of `aspire describe` includes generated local connection and telemetry credentials.
- **Impact:** Raw diagnostic output is unsuitable for committed or uploaded evidence, and detached operational rehearsal uses tool-owned state outside the repository even though application artifacts remain repository-local.
- **Current handling:** Repository run and verification scripts do not call `aspire describe` or retain its output. The B08 diagnostic values were ephemeral local-development credentials, the AppHost was stopped, and the repository secret scan passed. No external or production credential was accessed.
- **Alternatives:** Accept Aspire's documented per-user CLI state as a prerequisite exception, isolate CLI state when Aspire supports a repository-local option, or replace raw resource description with a redacted health-evidence command.
- **Owner:** Jerrett Davis (`@JerrettDavis`) for the local tooling policy.
- **Status:** Open; do not attach raw `aspire describe --format Json` output.

## EF migration tool patch alignment

- **Finding:** The installed `dotnet-ef` tool is 10.0.7 while the repository's EF Core runtime packages are 10.0.11.
- **Impact:** B08 migration generation emitted the supported older-tool warning; generated SQL shape, compilation, migration application, and PostgreSQL behavior all passed, but repeat generation is not fully tool-version aligned.
- **Current handling:** No global tool was installed or changed. The checked-in forward migration is deterministic and verified by the repository build and real PostgreSQL tests.
- **Alternatives:** Pin `dotnet-ef` 10.0.11 in a repository tool manifest and restore it through the documented command path, or update the machine-global tool under explicit machine-owner authority.
- **Owner:** Jerrett Davis (`@JerrettDavis`) with the build tooling owner when assigned.
- **Status:** Open before the next migration-producing session.

## Typed change-set audit scope

- **Decision:** B09 records a closed, ordered list of typed operations for each owned feature transition. Current project state remains canonical; the history is an explanatory audit projection and is not replayed as event-sourced truth.
- **Current handling:** Expected revision is checked before commit, one database transaction persists the state changes, revision, audit stamp, and operation rows, and existing operation identities retain their idempotent result without another revision. The stable model query and accessible overview expose reason, author, and ordered operations.
- **Extension rule:** A new semantic write must declare its typed operations in the feature transition and prove atomic rollback, conflict behavior, idempotency, and history projection. A permissive generic graph operation is not an acceptable substitute.
- **Status:** B09 scope delivered on 2026-08-16.

## Historical change-set backfill fidelity

- **Finding:** Change sets written before the B09 operation table contain a change kind and primary element identifier, but do not contain enough data to reconstruct every element or relation affected by a multi-element commit.
- **Impact:** The forward migration can preserve a truthful one-operation historical summary, but cannot manufacture exact typed operations for old rows.
- **Current handling:** Migration backfills one deterministic historical operation per existing change set and labels it with the stored change kind and element identifier. Every post-B09 commit records its complete typed operation list. A real PostgreSQL migration test covers the backfill.
- **Alternatives:** Retain the honest summary, derive richer historical detail from an explicitly versioned migration source when one exists, or start exact operation auditing at the B09 boundary.
- **Owner:** Jerrett Davis (`@JerrettDavis`) with the persistence owner when assigned.
- **Status:** Accepted limitation; do not infer missing historical operations.

## Change-set portability gap

- **Finding:** Project format 1.0 has no versioned representation for revision history, audit reasons, authors, or ordered change operations.
- **Impact:** The revision-9 dogfood file can model the delivered capability and evidence, but B10 import/export cannot round-trip runtime history without an owned compatibility decision.
- **Current handling:** History remains a PostgreSQL-backed application projection and is not inserted into `extensions` or a speculative schema section. Current project state is the portable canonical model.
- **Alternatives:** Define history as intentionally non-portable operational metadata, add a versioned optional audit section, or publish a separate versioned audit format.
- **Owner:** Jerrett Davis (`@JerrettDavis`) with the project-format owner when assigned.
- **Status:** Remains open; B10 preserves the boundary by rejecting unsupported live imports rather than inventing a portable history representation.

## B10 supported live-import profile

- **Decision:** The first live import profile accepts only Project, Actor, Outcome, and the registry-owned `benefitsFrom` relation. Views, claims, evidence, tags/sources, and other schema-valid element or relation kinds return `import.compatibility.unsupported` before persistence.
- **Rationale:** Those excluded structures do not yet have owned runtime persistence and semantic transitions. Storing them as generic rows would create a second untyped canonical model and conceal the format/runtime gaps already recorded here.
- **Current handling:** The complete format-1.0 envelope is bounded, schema-validated, deterministically read, and inspected. Only the owned profile proceeds to one PostgreSQL transaction and one typed import change set.
- **Status:** Safe B10 profile delivered; expand only with the concrete behavior and typed model for each kind.

## Canonical snapshot byte fidelity

- **Finding:** PostgreSQL `jsonb` preserves JSON meaning but normalizes property order and whitespace, so it cannot preserve canonical export bytes.
- **Resolution:** Revision-bound portable snapshots store validated canonical UTF-8 JSON as `text`, alongside its SHA-256. Typed payloads remain `jsonb`. Export refuses a snapshot whose model revision differs from current state.
- **Evidence:** Codec and real PostgreSQL tests prove export-import-export equality, identical hash, and stale-snapshot refusal.
- **Status:** Resolved during B10 on 2026-08-16.

## Native project export coverage

- **Finding:** Compatible native Project, Actor, Outcome, and `benefitsFrom` state now has a complete portable projection, while advanced narrative, state/logic, and path packets still contain format gaps that cannot be filled without invention.
- **Current handling:** Actor and Outcome commits attempt snapshot projection inside their PostgreSQL transaction. A snapshot is written only when one persisted Outcome exactly states the project's intended outcome and every current kind belongs to the supported profile. Imported and compatible native snapshots export byte-stably at their exact current revision; later unsupported edits make the prior snapshot stale and export fails closed.
- **Projection decision:** The native workflow currently represents only active, defined projects, so that supported subset projects `status: active` and `definitionStatus: defined`. Element and relation statuses come from persisted typed state. No alternate lifecycle state is inferred.
- **Impact:** The export endpoint never emits an incomplete advanced model as canonical truth.
- **Alternatives:** Build a complete typed snapshot projector as each format gap is resolved, create validated snapshots after every accepted change set, or introduce a versioned portable profile that explicitly excludes unsupported kinds.
- **Owner:** Jerrett Davis (`@JerrettDavis`) with the canonical model and project-format owner when assigned.
- **Status:** Partially resolved during B10 on 2026-08-16; advanced native projection remains open and universal export is not claimed.

## Theme-aware studio foundation

- **Decision:** The application now projects every delivered route into a persistent light/dark studio shell with global navigation, semantic explorer, route-owned work surface, contextual guide, operational workbench, keyboard skip navigation, and responsive collapse. The information architecture follows Purpose → Context → Behavior → State → Recovery → Evidence rather than a generic CRUD table.
- **Boundary:** React Flow and a WebAssembly Studio canvas remain deferred until a modeled lens, layout state, selection contract, and non-drag equivalent are delivered. Adding a graph library before those contracts would create speculative presentation state.
- **Evidence:** Headless Chromium asserts named landmarks, first-tab focus transfer, responsive collapse, recovery actions, and captures desktop light/dark, narrow, and recovery views.
- **Status:** C01 shell and routing delivered on 2026-08-16.

## Static SSR route-recovery status

- **Finding:** Setting HTTP 404 from inside the statically rendered recovery component suppresses its rendered body in the current ASP.NET Core component endpoint pipeline.
- **Impact:** A technically correct empty response prevents a human contributor from recovering through the product UI.
- **Current handling:** Unmatched GET/HEAD document requests that explicitly accept HTML redirect to `/not-found`, which states that model state was unchanged and offers stable recovery actions. Unmatched API, asset, non-HTML, and mutation requests remain plain 404 responses and are never redirected to HTML.
- **Alternatives:** Adopt framework-supported not-found response rendering when it can preserve both body and 404 status, introduce a dedicated server-rendered endpoint result, or retain the recovery redirect.
- **Owner:** Jerrett Davis (`@JerrettDavis`) with the web platform owner when assigned.
- **Status:** Open framework integration gap; usable human recovery is delivered.

## Plain JSON import boundary

- **Decision:** B10 accepts the authoritative plain JSON media type only, bounded to 1 MiB and nesting depth 64. It rejects unsupported versions, duplicate core identifiers, unsafe absolute URI schemes, schema failures, and incompatible live semantics.
- **Known gap:** Deterministic ZIP bundles, assets, SVG/HTML sanitization, decompression limits, malware scanning, and quarantine storage remain deferred because no bundle upload behavior is implemented.
- **Status:** Plain JSON boundary delivered; bundle import requires a separate security-owned slice.

## Outcome cockpit projection boundary

- **Decision:** C02 projects the project overview as an outcome-centered cockpit from the stable runtime query. Purpose and intended outcome are focal; actors, outcomes, narratives, state/logic, paths, recent changes, gaps, and the recommended next action remain derived presentation state rather than another canonical model.
- **Determinism:** The next action selects the first absent owned packet in Context → Outcome → Behavior → State → Recovery order. When all currently supported packets exist, it directs the modeler to review authored change history. No percentage, maturity grade, or inferred readiness is calculated.
- **Unknown handling:** The runtime query does not expose evidence records. The Evidence stage therefore displays `Unknown` with that limitation instead of treating absent query data as missing evidence or completed proof.
- **Accessibility and theme:** The graph-like topology is read-only and hidden from accessibility APIs because a visible structured list exposes the same nodes, statuses, and descriptions. Headless evidence covers light, dark, narrow, unavailable, empty, populated, and complete-profile states.
- **Boundary:** React Flow, editable canvas state, selection, layout persistence, and WebAssembly remain deferred until an owned modeling lens and keyboard/non-drag interaction contract require them.
- **Status:** C02 outcome cockpit delivered on 2026-08-16.

## C01 dogfood identifier correction

- **Finding:** The revision-12 C01 relations and claim referred to the pre-existing Bootstrap Repository episode identifier instead of the C01 outcome identifier, although the C01 outcome element itself was present and schema validation could not infer the semantic mismatch.
- **Resolution:** Revision 13 corrects the intended-outcome list, capability payload, relations, and claim to reference `Contributor Navigates a Coherent Studio Shell`. No runtime or schema behavior changed.
- **Prevention:** Contract validation continues to prove referential validity; semantic fixture reviews must additionally compare referenced element kinds and meanings for newly modeled outcomes.
- **Status:** Resolved during C02 on 2026-08-16.

## Semantic Explorer client boundary

- **Decision:** C03 introduces `ProjectBuilder.Web.Client` as the first Interactive WebAssembly Studio boundary because live filtering, virtualization, URL-stable selection, and keyboard view organization require client interactivity. The assembly references only `ProjectBuilder.Contracts`; the server host references it for composition and static asset delivery.
- **State separation:** The Explorer fetches the stable read-only project model endpoint. Selected identifiers live in the URL, expansion and ordering live in component view state, and no client-side object becomes an editable semantic model. Existing server handlers remain the only semantic write path. The current query does not expose element definition/knowledge status, so the inspector states `Status not exposed` rather than inferring `Defined`.
- **Interaction:** The virtualized tree uses an ARIA active descendant, arrow/Home/End navigation, expansion, Enter/Space selection, live search, reference badges, contextual open/add actions, and both buttons and Alt+Arrow keys for non-drag view reordering. Search and tree focus survive their respective updates.
- **Focus correction:** Activating the Blazor client caused the existing `FocusOnNavigate` component to focus the initial page heading and bypass the first-tab skip link. C03 removes that automatic focus movement and marks the same-document skip link as native navigation, preserving first-tab and explicit focus transfer without stealing focus during Explorer updates.
- **Theme and evidence:** The Explorer uses shared studio tokens and a split tree/inspector surface rather than a CRUD table. Headless Chromium covers populated, filtered, selected, dark, narrow, and complete-profile states against Kestrel and PostgreSQL.
- **Current limitation:** Reordered view position is session-local. Persisting it requires an owned personal/shared view-state contract, revision policy, and migration behavior; semantic ordering is deliberately untouched.
- **Status:** C03 Explorer delivered on 2026-08-16.

## Typed editor and actor knowledge-state boundary

- **Decision:** C04 composes reusable client-safe field descriptors and validation presentation into an actor-specific Studio editor. The draft, changed-field set, readiness projection, and operation identity remain client state until an explicit commit dispatches the existing expected-revision actor command.
- **Semantic handling:** Actor knowledge state is now accepted by the application command, persisted on the canonical element, returned by stable contracts, included in request fingerprints, and rehydrated from PostgreSQL. The UI exposes every canonical state and does not infer certainty.
- **Source gap:** Runtime actors do not own source references, a knowledge owner, review date, or structured rationale. The editor links to the runtime contract and audit history, explicitly labels source attachment as `Unknown · not exposed`, and directs the contributor to the owned change reason rather than inventing fields.
- **Portability finding:** Format 1.0 can represent non-Known knowledge states, but the current B10 live-import profile intentionally accepts only Known elements. Native snapshot projection therefore fails closed after a non-Known edit instead of producing a document the live importer cannot round-trip. The project-format owner must either expand typed import preservation or retain this explicit profile limitation.
- **Evidence:** Real PostgreSQL and headless Chromium prove staged validation, explicit Assumed state, one typed commit, query persistence, established actor-journey compatibility, and reviewed clean, staged, committed, light, dark, and narrow renderings.
- **Status:** C04 typed editor framework delivered on 2026-08-16; structured actor sources and complete knowledge provenance remain open.

## Participant and outcome revision semantics

- **Decision:** C05 adds typed Actor and Outcome update commands rather than exposing generic graph mutation. Updates preserve element identifiers, semantic order, and original creation attribution; each accepted edit advances one expected project revision and records `element.updated`. Changing an outcome beneficiary preserves the owned relation identifier and records `relation.updated` in the same PostgreSQL transaction.
- **Conflict recovery:** A stale editor receives structured expected/actual revision evidence. The contributor can explicitly refresh the revision and operation identity while preserving the local draft, then retry. No last-write-wins or silent draft replacement occurs.
- **Duplicate guidance:** Actor and Outcome editors project deterministic case-insensitive name containment against the current model. Suggestions link to existing meaning but neither block nor merge because lexical similarity is not proof of semantic identity.
- **Delete-policy finding:** Actor and Outcome removal/deprecation is not implemented. Actors can be referenced by outcomes, narratives, state, paths, evidence, and future baselines; outcomes can anchor narratives, claims, and intended project purpose. Hard delete, cascade, detach, deprecate, and replacement have materially different semantic consequences with no owned policy in the delivered runtime model.
- **Alternatives:** Add explicit deprecation with replacement links, implement restrict-only removal with a complete impact report, or define typed detach/cascade operations per relation registry rule. The model and product owner must choose the lifecycle policy before a destructive command is exposed.
- **Owner:** Jerrett Davis (`@JerrettDavis`) with the canonical-model owner when assigned.
- **Status:** C05 create/read/update, relation revision, duplicate guidance, and conflict recovery delivered on 2026-08-16. Destructive behavior remains an explicit finding rather than an invented cascade.

## Narrative flow composition boundary

- **Decision:** C06 replaces the flat server-rendered narrative form with an Interactive WebAssembly Scenario Flow composer. The ordered outline, participant lane, semantic chain, readiness inspector, and operation preview are projections over the existing typed narrative request; the server-owned command remains the only semantic write boundary.
- **Ordering and accessibility:** The delivered command owns one complete Episode → Scenario → Scene → Interaction packet with Intent, Step, and Observation children at deterministic orders 01–07. Every value is available through labeled controls and a structured outline, and no action requires drag. The UI does not pretend to reorder siblings that the current command cannot independently add or move.
- **Reference handling:** Desired outcome, initiator, and receiver are selected from the current project model. Identical initiator and receiver values are reported before commit. Containment, participation, direction, intent, observation, and semantic result operations commit atomically with expected revision and audit reason.
- **Visual evidence:** Shared theme tokens drive light and dark rendering. Headless Chromium and real PostgreSQL cover clean light, clean dark, narrow responsive, fully authored, and committed states while retaining the established end-to-end project journey.
- **Current limitation:** Multiple scenes/interactions/steps, sibling reorder commands, narrative revision, and richer path/reference editing are not yet owned by runtime commands. Adding client-only nodes would create a second model and is explicitly deferred.
- **Owner:** Jerrett Davis (`@JerrettDavis`) with the canonical-model owner when assigned.
- **Status:** C06 complete narrative packet composition delivered on 2026-08-16; multi-item narrative editing remains a future command slice.

## State Lens and result-path boundary

- **Decision:** C07 replaces the flat state-and-logic form with an Interactive WebAssembly State Lens. It projects the existing typed state, fact, rule, invariant, semantic-result, and transition packet as a catalog, connected logic deck, invariant proof contract, transition topology, result/path matrix, readiness inspector, and atomic command preview.
- **Truth separation:** State categories remain explicit and are never inferred from where a field is displayed or persisted. Fact and rule authority reference modeled actors. Transition bindings to the owned fact, rule, invariant, and semantic results are created only by the server-owned expected-revision command.
- **Invariant handling:** The editor requires both a falsifying example and proof expectations. These remain modeled claims about proof, not evidence that the invariant has already been verified.
- **Path handling:** The matrix presents each typed semantic result and labels its path as `Unmodeled · next slice`. The existing path command requires scenario and transition/result references, so C07 does not invent client-only path connections or treat absence as success.
- **Reference proof:** A dedicated BDD journey authors the POS `Transaction / Add product` state model with Added, NotFound, Unavailable, and Closed results entirely through the UI. Headless evidence covers light, dark, narrow, authored, and committed states against real PostgreSQL.
- **Current limitation:** The command owns one complete state-and-logic packet. Multiple catalog items, independent update/reorder operations, and direct path connection require dedicated semantic commands rather than generic graph mutation.
- **Owner:** Jerrett Davis (`@JerrettDavis`) with the canonical-model owner when assigned.
- **Status:** C07 State Lens and reference POS authoring delivered on 2026-08-16; independent revision and path-link commands remain future slices.

## Problems query and finding lifecycle boundary

- **Decision:** C08 introduces a deterministic application query that evaluates the current canonical model into revision-bound findings and evidence requirements. It does not store a parallel findings graph. Results are ordered by severity, code, and scope and expose rule, explanation, affected scope, modeled owner, safe repair route, and whether a repair command is currently available.
- **Status semantics:** Query-produced findings are labeled `Open · derived`. Evidence requirements are `Unknown` when no structured requirement exists and `Required` when a proof expectation exists but no artifact linkage is exposed. Neither state is presented as verified or resolved.
- **Navigation:** Existing typed commands are linked only when their prerequisites are met. Exact-scope navigation uses stable model identifiers. An unavailable repair opens context and explains the missing command rather than mutating model truth.
- **Evidence boundary:** The runtime query exposes outcomes and invariant proof expectations but no evidence artifacts, freshness, waivers, baselines, or operational observations. The matrix shows this limitation directly and never treats build/test screenshots as canonical project evidence.
- **Lifecycle finding:** Assignment, suppression, waiver, durable resolution, and evidence-artifact attachment require owned semantic types, authorization, expected-revision commands, audit behavior, and compatibility policy. C08 does not invent component-local lifecycle state.
- **Alternatives:** Add a canonical Finding/Gap lifecycle with explicit status transitions; keep findings entirely derived and add scoped waiver records; or introduce baseline-scoped review dispositions. The canonical-model and governance owners must select the durable policy before lifecycle actions are enabled.
- **Owner:** Jerrett Davis (`@JerrettDavis`) with the canonical-model and governance owners when assigned.
- **Status:** C08 deterministic Problems and Evidence workbench delivered on 2026-08-16; finding/evidence lifecycle mutation remains an explicit future slice.

## Draft recovery and revision projection boundary

- **Decision:** C09 stores the Actor editor draft in browser-local storage as a versioned envelope containing its project/editor key, base revision, idempotency operation identifier, update time, and typed value. Bounded undo/redo snapshots remain local presentation state. Successful commit or explicit discard clears the envelope.
- **Concurrency behavior:** A recovered envelope whose base revision differs from current canonical revision is visibly stale and cannot commit. The contributor must explicitly adopt the latest revision while preserving draft input; the server's expected-revision check remains authoritative.
- **History decision:** The revision workbench projects the existing canonical change-set contract as a timeline and deterministic typed-operation diff. It does not create a parallel history model or infer before/after field values that the contract does not provide.
- **Finding:** A complete materialized read-only model at an arbitrary historical revision requires an owned revision query and persistence policy. C09 reports this boundary in the workbench.
- **Evidence:** Headless Chromium states 76–93 cover staged drafts, undo/redo, refresh recovery, stale-base blocking, explicit reconciliation and commit, scoped keyboard commands, light/dark history, semantic operation selection, and narrow layouts across Actor, Outcome, Narrative, State, and Recovery.
- **Status:** C09 cross-editor draft and keyboard integration delivered on 2026-08-16; historical snapshot materialization remains an explicit gap.

## Recovery lens and keyboard lifecycle

- **Decision:** The previous server-rendered Path field stack is replaced by an Interactive WebAssembly Recovery lens. It projects existing typed scenario, transition, fact, rule, result, actor, branch, effect, and recovery contracts into a condition → branch → effect → recovery topology. A structured outline and ordinary labeled fields remain the complete non-canvas alternative.
- **Draft behavior:** The Recovery lens uses the same browser-local versioned envelope boundary as Actor, adds bounded undo/redo, clears state on discard or successful commit, and refuses a stale recovered draft until the contributor explicitly adopts the latest canonical revision.
- **Keyboard behavior:** `Ctrl+S`, `Ctrl+Z`, `Ctrl+Y`, and `Ctrl+Shift+Z` are implemented through an isolated browser listener scoped to the active editor. Registration includes a unique mount token so disposal after refresh or same-route navigation cannot unregister a newer component. The listener never uses global input injection and does not move focus.
- **Evidence finding:** The established seven-revision dogfood journey initially exposed an accessible-name collision between the Intended effect region and field and then exposed the stale-listener disposal race after refresh. Both were corrected before acceptance. Required markers are now included in accessible names.
- **Evidence:** Headless Chromium states 84–87 prove keyboard undo/redo, refresh recovery, keyboard commit, and reviewed light, dark, and narrow Recovery lens layouts against Kestrel and PostgreSQL. States 17–19 continue to prove complete path creation, commit, and canonical review.
- **Status:** Recovery lens delivered on 2026-08-16. Path editing/reordering and configurable bindings remain explicit follow-through; all delivered typed editors now share the draft/keyboard boundary.

## Purpose-profile and gap-governance boundary

- **Decision:** C10 evaluates Discovery and Implementation Ready as deterministic read-only overlays over one revision-bound canonical model query. Profile selection is URL-addressable view state; it does not change facts, definition status, or revision.
- **No false completeness:** Purpose, participants, behavior, state, paths, and evidence are projected as inspectable dimensions and explicit predicates. Missing prerequisites produce gaps rather than vacuous `Defined` results. No aggregate percentage is calculated.
- **Visual contract:** The theme-aware gap topology connects the six dimensions, while a structured predicate rail exposes the equivalent non-canvas explanation. Every dimension links to an existing authored definition or review route.
- **Fail-closed contract:** Unknown purpose profile identifiers return `purpose-profile.invalid`. The first registry intentionally supports only `discovery` and `implementation-ready`.
- **Governance decision:** The first mutation is a create-only governed record, not a second canonical Finding element and not component-local state. Assumed, Deferred, Accepted Risk, and Not Applicable require a stable profile/rule/scope key, rationale, material consequence, modeled authority actor, audit reason, and expected revision. Assumed, Deferred, and Accepted Risk require a future review/expiration date; Deferred also requires a target milestone.
- **No false repair:** A disposition changes the project revision and is projected beside the derived finding, but the rule continues to evaluate and the finding remains visible. Resolution, reopening, and supersession are separate future transitions because they require prior-disposition identity and, for resolution, a resolving change set.
- **Schema finding:** The portable project schema has no governed disposition collection. The runtime stores the record in PostgreSQL and the dogfood fixture records delivered behavior as claim/evidence truth; the interchange format was not expanded merely to make the fixture look complete.
- **Evidence:** Application tests prove the same revision produces different requirements and severities without changing facts. PostgreSQL/headless-Chromium states 94–97 prove profile projections; states 98–101 prove staged and committed Deferred governance in light, dark, and responsive layouts. Domain tests prove atomic revision advance and fail-closed required fields.
- **Owner:** Jerrett Davis (`@JerrettDavis`) with canonical-model and governance owners when assigned.
- **Status:** C10 profile evaluation, gap visualization, and first governed disposition transition delivered on 2026-08-16. Lifecycle continuation (Resolved, Reopened, Superseded) remains deferred; D01 is the next product slice after verification.

## Deterministic guidance-registry boundary

- **Decision:** D01 introduces a versioned typed prompt registry in Application and derives applicability only from explicit facts in the current canonical model. Missing knowledge does not silently become a negative fact. Registry construction fails closed for duplicate identifiers, malformed versions or ordering, unreachable/contradictory applicability, incomplete answer semantics, or invalid repair routes.
- **Prompt anatomy:** Every built-in prompt owns its question, rationale, learning content, deterministic trigger explanation, related fact kinds, examples explicitly labeled as examples, Author/Unknown/Assumed/Deferred/Not Applicable mappings, and an authored repair-route template. The stable API exposes this client-safe projection without exposing domain or infrastructure types.
- **Studio projection:** The Guide Rail presents a six-stage topology, applicable prompt queue, causal chain, learning context, answer-to-change map, and links to owned authoring/problem flows. Stage choice is keyboard-operable view state and never mutates semantic truth. Shared tokens provide reviewed light, dark, and responsive presentations; Evidence remains visible at desktop widths after visual review caught and corrected a connector-grid overflow.
- **No pretend interaction:** D01 deliberately does not persist an answer. The UI states that its mappings describe the reviewable change a future owned command must create. Open/close/reopen state, progress trail, back/next flow, answer-state orchestration, selection synchronization, and focus restoration belong to D02.
- **Evidence:** Application tests prove deterministic ordering, complete built-in validation, explicit contradictory-applicability detection, and fail-closed handling of missing prerequisite facts. Real Kestrel/PostgreSQL and headless Chromium prove the stable API and screenshots 102–105 cover outcome, participant, dark, and responsive states.
- **Owner:** Jerrett Davis (`@JerrettDavis`) with the experience and canonical-model owners when assigned.
- **Status:** D01 prompt registry and inspectable guidance map delivered on 2026-08-16. D02 Guide Rail shell is the exact next slice.

## Recoverable Guide Rail session boundary

- **Decision:** D02 treats guide location and uncommitted answer choices as versioned browser-local presentation state keyed by project. A session is restored only when both registry version and canonical model revision still match; otherwise guidance reevaluates safely instead of replaying stale answers onto changed truth.
- **Shell behavior:** The non-modal contextual rail opens beside the adaptive route, synchronizes stage and prompt selection, supports previous/next without trapping direct navigation, shows explicit local progress, and previews each answer's modeled consequence. Closing the rail leaves the route usable; reopening or reloading returns to the selected prompt and local answer state.
- **Focus and keyboard:** `Ctrl+Shift+G` toggles the rail, Escape closes it, and the scoped browser registration remembers and restores the invoking element without global input injection. Opening does not transfer focus automatically. A mount token prevents stale component disposal from unregistering a newer handler.
- **Truth boundary:** Local `Author`, `Unknown`, `Assumed`, `Deferred`, and `Not Applicable` selections are not canonical dispositions, do not satisfy readiness, do not advance revision, and create no audit history. D03 must translate concrete participant/outcome answers into reviewable operations through the existing expected-revision commands.
- **Visual evidence:** Headless Chromium screenshots 106–110 cover answered, closed, reopened, dark, and responsive states. Review exposed an overly compressed six-column topology beside the rail and a visible live-region leak; acceptance uses a deliberate 3×2 open-rail topology and screen-reader-only announcement while the closed rail retains a six-stage panorama.
- **Owner:** Jerrett Davis (`@JerrettDavis`) with the experience and accessibility owners when assigned.
- **Status:** D02 contextual Guide Rail orchestration delivered on 2026-08-16. D03 actor and outcome guided flow is the exact next slice.

## Guided participant and outcome framing boundary

- **Decision:** D03 composes the existing typed Actor and Outcome API commands inside the Guide Rail rather than introducing a wizard-specific server command. Guided values are recoverable browser drafts with the current base revision and idempotency identifier; finishing a step crosses the same expected-revision boundary as the full Studio editor.
- **Plain-language flow:** The Participant composer asks who is involved, what part they play, what they seek, what work they own, what they can decide, and what constrains them. The Outcome composer asks who receives value, what becomes possible, and what observable signals show success. Precise model terms remain visible in the operation preview but are not prerequisites for answering.
- **Relationship handling:** The Outcome step presents canonical Actors as beneficiary cards and commits one `outcome.added` plus one `relation.added · benefitsFrom` operation atomically. It does not accept a free-text beneficiary that would create duplicate or unresolved meaning. The full Actor editor retains lexical duplicate guidance; the deterministic participant prompt stops applying once any Actor exists, so D03 does not create an unreachable guided duplicate branch.
- **Revision behavior:** After Actor commit, the stale Participant prompt is discarded and guidance reevaluates to Frame at revision 2. After Outcome commit, Frame and Participants become Established and the rail advances to the Behavior prompt at revision 3. A recovered draft based on another revision cannot commit until the contributor explicitly adopts the latest revision while preserving input.
- **Knowledge boundary:** Known, Unknown, Assumed, Deferred, Disputed, and Not Applicable remain selectable through the existing element command contract. Actor/Outcome currently lack structured assumption authority and review-date fields; non-Known choices require an audit reason and expose this limitation rather than inventing provenance.
- **Evidence:** Real Kestrel/PostgreSQL and headless Chromium prove recovered participant input, `actor.added`, existing-beneficiary selection, atomic Outcome/relation commit, deterministic next-prompt evaluation, and revision-history inspection. Reviewed screenshots 111–116 cover recovered, transitioned, relationship, narrow, dark, and history states.
- **Owner:** Jerrett Davis (`@JerrettDavis`) with experience and canonical-model owners when assigned.
- **Status:** D03 guided Actor and Outcome framing delivered on 2026-08-16. D04 guided Scenario flow is the exact next slice.

## Guided Scenario flow boundary

- **Decision:** D04 embeds a recoverable plain-language behavior composer in the deterministic Behavior prompt and translates it through the existing `projectbuilder.narrative.define` command. It does not create a wizard-specific scenario contract or client-side graph model.
- **Semantic projection:** The flowboard projects Episode → Scenario → Scene → Interaction → Intent → Step → Observation readiness beside the authored fields. Its ordered list is the complete keyboard and screen-reader alternative; layout and readiness never become semantic truth.
- **Plain-language path:** The guide captures an outcome boundary, initiating situation, completion condition, ordinary path classification, starting facts, trigger, expected result, responsibility-bearing setting, directed participants, expressed intent, meaningful work, observation, and named semantic results. Existing Actor and Outcome identities are selected rather than copied as free text.
- **Revision behavior:** The browser-local draft retains base revision and idempotency identity, survives rail closure/reopen, and refuses stale commit until the contributor explicitly adopts the latest revision. `Ctrl+S` and the visible Finish action cross the same expected-revision boundary. Successful commit clears the draft and reevaluates guidance to State.
- **Atomicity:** One accepted command creates seven typed elements plus containment, participation, outcome, direction, intent, step, and observation relations in one transaction. History exposes `narrative.defined` and the deterministic seven-operation sequence.
- **Scope finding:** The narrative schema supports named semantic result categories but does not itself own state facts/rules, result transitions, alternate paths, recovery, or evidence artifacts. Those remain explicit subsequent Guide stages backed by their separate commands; D04 does not pretend result names prove those paths are modeled.
- **Evidence:** Real Kestrel/PostgreSQL and headless Chromium prove the ordinary recognized-item POS scan, close/reopen recovery, 0/7-to-7/7 topology, exact canonical query, deterministic advance to State, and revision history. Reviewed screenshots 117–122 cover empty, recovered, ready, dark, narrow, and history states.
- **Owner:** Jerrett Davis (`@JerrettDavis`) with experience and canonical-model owners when assigned.
- **Status:** D04 guided Scenario composition delivered on 2026-08-16. D05 deterministic completeness recommendations is the exact next slice; guided State/Recovery/Evidence authoring remains an explicit follow-through rather than hidden Scenario state.

## Deterministic recommendation boundary

- **Decision:** D05 introduces one versioned Application query (`builtin/1`) that ranks next actions from the canonical revision, selected purpose profile, deterministic findings, explicit dependency gates, and bounded recent-work continuity. The overview and the interactive Decision Lens project this same result; recommendation logic is not duplicated in client state.
- **Truth boundary:** Recommendations, ranks, profile pressure, and UI selection are query projections. They do not change model facts or revision, and the interface deliberately provides no completeness percentage. Purpose may change whether an incomplete dimension is Required or Advisory, but it cannot rewrite semantic truth.
- **Ordering:** Ready dependencies precede blocked work; purpose pressure precedes advisory work; finding severity, recent-work continuity, semantic stage order, and stable rule identifier break remaining ties. Recent work can never cross a dependency gate. An unknown profile fails closed.
- **Interface:** The theme-aware Decision Lens uses a decision graph plus an equivalent structured outline, ranked action lanes, explicit blocked prerequisites, a stable-input ledger, and ordinary links into owned editors. The overview consumes the same query for its compact orientation card.
- **Evidence:** Application tests prove byte-stable serialized results, purpose-relative pressure at one revision, and dependency-gate precedence. Real Kestrel/PostgreSQL and headless Chromium prove the stable API and action continuity; reviewed screenshots 123–128 cover Discovery, Implementation Ready, alternatives, dark, narrow, and selected-editor states.
- **Finding:** Runtime evidence readiness is still derived from the findings projection because the current runtime model query has no durable evidence-record collection. D05 exposes that boundary rather than treating test count or screenshots as canonical evidence.
- **Status:** D05 explainable deterministic recommendations delivered on 2026-08-16. D06 workshop mode is the exact next slice.

## Workshop facilitation and provisional-record boundary

- **Decision:** D06 introduces a deterministic `workshop/1` Application query that composes a six-movement, 65-minute discovery workshop from canonical purpose, actors, findings, and recommendations. The query is byte-stable for the same revision and profile and does not create workshop domain state.
- **Presentation state:** Running/paused status, current movement, discussed movements, parking-lot threads, and room notes are versioned browser-local state. Recovery requires both the same workshop brief version and canonical model revision, preventing stale facilitation context from being replayed onto changed truth.
- **Truth boundary:** Captured Decision, Assumption, and Question notes are labeled `Provisional`, retain an explicit owner including `Unknown`, and export with the source brief and revision. They are not added to semantic history because runtime Domain, Application, and persistence do not yet own commands for those schema-defined element kinds.
- **Participant projection:** Participant view intentionally removes pause, movement-completion, agenda-selection, parking-lot, and facilitator-note controls. It shows only the shared movement, intended result, project, profile, revision, and an explicit return action.
- **Export:** The browser generates a readable JSON workshop summary containing the deterministic server brief and provisional local session. Export is evidence/hand-off, not silent import or semantic commit.
- **Evidence:** Application tests prove brief determinism, stable order/duration, and purpose-relative evaluation without revision mutation. Real Kestrel/PostgreSQL and headless Chromium prove the complete internal discovery workshop, reload recovery, provisional capture, participant separation, and export content. Reviewed screenshots 129–135 cover ready, live, completed, captured, participant, dark, and narrow states.
- **Finding:** The portable schema includes `decision`, but executable runtime support does not. Adding canonical Decision/Assumption/Question capture requires owned domain definitions, authority/status lifecycle, expected-revision commands, persistence, import/export policy, and review semantics; D06 does not infer that policy.
- **Finding:** Workshop state is single-browser presentation state. Realtime presence, shared facilitation, comments, authorization-separated participant display, and durable collaborative history remain future collaboration behaviors rather than simulated local features.
- **Status:** D06 workshop mode delivered on 2026-08-16. E01 immutable lens projection contract is the exact next slice.

## Immutable lens projection boundary

- **Decision:** E01 introduces `lens/1`, a read-only Projections-owned contract over client-safe Project, Actor, Outcome, and existing `benefitsFrom` definitions. Nodes, ports, edges, filters, diagnostics, inspector fields, accessibility order, content hash, and projection identity are canonicalized deterministically for one revision.
- **Validation:** Projection output fails closed for duplicate identifiers, missing endpoints, ports owned by another node, wrong port direction, or an incomplete structured equivalent. An intentional corrupted-edge test proves the validator catches the violation.
- **Truth boundary:** Project context remains pinned while filters suppress unmatched definitions and any now-incomplete edges. Selection, focus, search, filter choice, and CSS lane placement are view state; there are no graph coordinates, semantic graph edits, or generic graph-engine abstractions.
- **Interface:** The Lens Lab provides theme-aware semantic lanes, typed port handles, relation cards, a sticky schema-driven inspector, diagnostics, deterministic hash/revision ledger, and a complete structured topology. Arrow keys traverse the deterministic node order after focus enters the visual canvas; ordinary buttons provide the non-drag alternative.
- **Evidence:** Projection tests prove byte stability, input-order independence, safe filtering, directional ports, and deliberate-corruption rejection. Real Kestrel/PostgreSQL and headless Chromium prove the stable endpoint and screenshots 136–141 cover light, selected inspector, filtered diagnostic, keyboard/outline, dark, and narrow states.
- **Finding:** The current project model query exposes Project, Actor, Outcome, Narrative, State, Path, and relations but not the complete portable meta-model. E01 stays at supported Project/Actor/Outcome depth; E02 must extend through an owned immutable Story Map contract rather than infer capabilities or episodes from unrelated runtime records.
- **Owner:** Jerrett Davis (`@JerrettDavis`) with projection, experience, and accessibility owners when assigned.
- **Status:** E01 immutable lens projection contract delivered on 2026-08-16. E02 Story Map lens is the exact next slice.

## Story Map and explicit capability boundary

- **Decision:** E02 adds an owned Capability definition and expected-revision `capability.added` transition, PostgreSQL payload persistence, client-safe contracts, and a deterministic `story-map/1` projection over explicit Outcome, Capability, Episode, Scenario, Scene, and Actor definitions.
- **Truth boundary:** The lens never derives a capability from workflow names. When none is modeled it reports `story-map.capability.missing`; priority and knowledge overlays change annotations only. Layout, focus, selection, and overlay state never advance the canonical revision.
- **Connector provenance:** Existing relation records are `semantic-relation`; Episode/Scenario and Scenario/Scene hierarchy is `semantic-containment`; capability, exercise, and participation traces are `derived-explicit-reference` from typed IDs. Derived does not mean guessed.
- **Interface:** A theme-aware value horizon, ability lanes, narrative bands, participant orbit, sticky inspector, trace ribbon, and deterministic structured equivalent share one immutable response. The capability deck shapes an ability around selected value rather than exposing a generic CRUD form. Arrow keys, Home, End, and ordinary buttons provide non-drag operation.
- **Evidence:** Domain and projection tests cover capability invariants, determinism, annotation-only overlays, filter-safe edges, and the explicit missing-capability diagnostic. Real Kestrel/PostgreSQL and headless Chromium prove the semantic commit and full value-to-scene trace; reviewed screenshots 142–148 cover the explicit gap, staged command, complete light map, scenario inspector, keyboard/overlay state, dark theme, and narrow layout.
- **Finding:** Capability is create-only and stored as validated JSON payload references; update, removal, ordering, import/export policy, and relational foreign keys are not owned. The narrative command currently creates one Episode-Scenario-Scene packet; multiple scenes, sibling ordering, and alternate paths require explicit semantic transitions rather than client graph edits.
- **Finding:** The repository-local EF migration was generated successfully, but the installed global `dotnet-ef` 10.0.7 tool is older than runtime 10.0.11. No compatibility defect appeared; a repository tool manifest remains a future reproducibility improvement.
- **Owner:** Jerrett Davis (`@JerrettDavis`) with projection, model, experience, and accessibility owners when assigned.
- **Status:** E02 Story Map delivered on 2026-08-16. E03 Scenario Flow lens is the exact next slice.

## Scenario Flow and explanatory playback boundary

- **Decision:** E03 introduces `scenario-flow/1`, a deterministic scenario-scoped projection over the existing Narrative and Path packets. The immutable model query now preserves stable Interaction, Intent, Step, Observation, Scenario, Transition, Condition, Effect, owner, and Result identifiers instead of forcing joins by display name.
- **Primary-route semantics:** Starting facts, trigger, and expected outcome are explicit scenario fields projected with `derived-explicit-field` provenance. Intent, Interaction, Step, and Observation are `semantic-element` nodes. The primary route ends once at its expected outcome; the interaction's declared result set remains inspectable metadata and is not rendered as sequential terminals.
- **Path semantics:** Exceptional and recovery routes project their canonical conditions, effects, result elements, and ordered payload segments. Segment nodes have deterministic explicit-field references. Playback order is derived from authored sequence and never becomes model truth.
- **Boundary finding:** An `externalInteraction` Effect produces an explicit `semantic-effect-classification` boundary edge and labeled boundary band. The runtime has no typed System, Interface, or Boundary element yet, so E03 cannot name either side as an architectural boundary; E05 owns that attachment. When no qualifying effect exists, `scenario-flow.boundary.unmodeled` is emitted instead of inventing one.
- **Interface:** The theme-aware workbench combines participant lanes, a responsive flowboard, path tabs, play/pause/previous/next controls, boundary visibility, synchronized inspector and transcript, and a deterministic all-node outline. Arrow keys, Home, End, Space, tabs, and ordinary buttons provide non-drag operation. Playback is read-only explanation and does not execute production code or evaluate runtime rules.
- **Evidence:** Projection tests prove byte stability, provenance, no-inference diagnostics, one primary terminal, ordered playback, classified boundary edges, and deliberate missing-endpoint rejection. Real Kestrel/PostgreSQL and headless Chromium build a seven-revision model and prove primary, exceptional, recovery, keyboard, timed playback, stable API, dark, and narrow behavior. Reviewed screenshots 149–155 cover those states.
- **Finding:** The current narrative command owns one Scene and one Interaction packet. Multiple scenes, joins, separately ordered interactions, and target-path links require owned semantic transitions rather than client-side graph edits. The existing Path command owns one branch plus one recovery packet.
- **Owner:** Jerrett Davis (`@JerrettDavis`) with projection, narrative, experience, and accessibility owners when assigned.
- **Status:** E03 Scenario Flow delivered on 2026-08-16. E04 State and Rule lens is the exact next slice.

## State and Rule equivalent-representation boundary

- **Decision:** E04 introduces `state-rule/1`, a deterministic state-scoped projection over the existing State, Fact, Rule, Invariant, Transition, Semantic Result, and explicitly linked Path Effect definitions. Stable IDs and allowed knowledge states now cross the existing client-safe model query; no migration or project-format change was required.
- **Provenance:** State, Fact, Rule, Invariant, Transition, Result, and Effect nodes are semantic elements. Before and after predicates are `derived-explicit-field` fragments of the authored Transition. Transition reference edges distinguish changed facts, evaluated rules, checked invariants, returned results, and path-owned effects.
- **Equivalent views:** The theme-aware workbench shares one immutable response across the causal graph, transition matrix, rule decision table, invariant proof panel, inspector, and deterministic outline. Arrow keys, Home, End, Tab, and ordinary buttons provide non-drag operation. Representation and selection are presentation state.
- **Unknown boundary:** Fact knowledge capability includes Known, Unknown, and Assumed where the canonical definition permits them. The lens reports `state-rule.events.unmodeled` because the runtime has no EventDefinition, and `state-rule.effects.unmodeled` when no Path explicitly links an Effect. Trigger prose never becomes an invented event.
- **Evidence:** Projection tests prove byte stability, provenance, semantic reference edges, no-inference diagnostics, and deliberate missing-endpoint rejection. Real Kestrel/PostgreSQL and headless Chromium prove the stable API and complete UI journey; reviewed screenshots 156–162 cover causal graph, keyboard inspector, transition matrix, decision table, invariant proof, dark theme, and narrow layout.
- **Finding:** The existing state command owns one State, Fact, Rule, Invariant, Transition, and typed result packet. Multiple facts, rules, transitions, event definitions, cross-state dependencies, and ordinary-path effect requests need owned semantic commands rather than client-side topology edits.
- **Owner:** Jerrett Davis (`@JerrettDavis`) with projection, model, experience, and accessibility owners when assigned.
- **Status:** E04 State and Rule delivered on 2026-08-16. E05 System Context lens is the exact next slice.

## System Context definition and projection boundary

- **Decision:** E05 introduces one atomic `system-context.defined` command that commits an owned System, external System, Interface, Boundary, and Contract at one expected revision. Known external authority must be distinct from owned authority, every actor reference is validated, and an optional crossing Effect must already exist in the project.
- **Projection:** `system-context/1` deterministically projects actors, systems, interface, contract, boundary, optional effect, and request/response data movement. Data flows use only explicit Contract fields and carry `contract-explicit-field` provenance; containment and reference connectors are labeled separately.
- **Interface:** The theme-aware composer shapes one accountable crossing with a live non-canonical topology preview. The review lens provides ownership/trust overlays, a boundary membrane, contract ledger, synchronized inspector, arrow-key traversal, and a deterministic structured equivalent. Overlay, focus, selection, and layout remain presentation state.
- **Evidence:** Domain tests prove the five-definition atomic transition and distinct-known-authority invariant. Projection tests prove byte stability and deliberate missing-endpoint rejection. Real PostgreSQL, Kestrel, WebAssembly, and headless Chromium prove the complete human workflow; reviewed screenshots 163–168 cover authoring, ownership, keyboard selection, trust, dark, and narrow states.
- **Finding:** The portable schema already describes System, Interface, Boundary, and Contract, but the runtime import/export compatibility profile still fails closed beyond Project, Actor, Outcome, and `benefitsFrom`. E05 does not silently widen that profile; native system-context persistence and query are delivered first.
- **Finding:** One command currently owns one context packet. Independent update/removal, multiple interfaces or contracts per system, nested boundary modeling, and relational foreign keys inside JSON payload references require later owned transitions and migration policy.
- **Owner:** Jerrett Davis (`@JerrettDavis`) with model, architecture, experience, and accessibility owners when assigned.
- **Status:** E05 System Context delivered on 2026-08-16. E06 Traceability lens is the exact next slice.

## Traceability definition and projection boundary

- **Decision:** E06 adds durable Claim and Evidence records as first-class canonical collections, separate from semantic elements, plus one atomic `evidence-packet.defined` command. The command validates explicit modeled scope, expected revision, author and reason, and prevents Passed or Failed evidence from using an Unknown summary.
- **Projection:** `traceability/1` deterministically projects Outcome → Claim → Evidence paths, missing-link debt with exact repair routes, and revision-relative impact. A linked definition changed after the evidence baseline becomes `review-required`; producer names and summary prose never become inferred test counts, coverage, or proof status.
- **Interface:** The guided evidence deck shapes scope, claim, owner, producer, environment, summary, and limitations beside a live attribution preview. The Traceability Atlas provides a value-to-proof river, debt workbench, impact radar, synchronized inspector, arrow-key traversal, and deterministic structured equivalent. Mode, selection, focus, and placement remain presentation state.
- **Evidence:** Domain tests prove atomicity and evidence-status validation. Projection tests prove byte stability, explicit provenance, later-change impact, no-inference diagnostics, and deliberate missing-endpoint rejection. Real PostgreSQL, Kestrel, WebAssembly, and headless Chromium prove missing, authoring, supported, keyboard, debt, impact, dark, and narrow states in reviewed screenshots 169–176.
- **Finding:** The initial command owns one Claim and one Evidence record, with one Evidence record per new Claim. Independent evidence addition, supersession, revocation, disputed review, and multi-evidence claim lifecycle need explicit transitions and authority policy.
- **Finding:** Claim and Evidence native persistence is delivered, but portable import/export still fails closed to the existing live profile. The schema already owns both collections; widening import/export requires compatibility and unknown-extension policy rather than silent serialization.
- **Owner:** Jerrett Davis (`@JerrettDavis`) with model, evidence, experience, and accessibility owners when assigned.
- **Status:** E06 Traceability delivered on 2026-08-16. E07 Canvas interaction kernel is the exact next slice now that multiple concrete lenses expose reusable selection, navigation, and viewport behavior.

## Accessible canvas interaction boundary

- **Decision:** E07 introduces one reusable Web.Client `ModelCanvas` over immutable client-safe node and edge views. It owns SVG geometry, semantic frames, typed connector paths, selection, alignment, mini-map, pan, zoom, and fit behavior entirely inside Presentation; no command, API, persistence record, or project revision is produced.
- **Interaction parity:** Pointer selection maps to arrow/Home/End navigation; pointer pan maps to Shift+Arrow; wheel zoom maps to `+`/`-` and visible controls; Fit Scope, Fit Selection, Across, and Down are ordinary buttons with keyboard activation. The existing deterministic semantic outline remains the complete non-canvas equivalent.
- **Interface:** Lens Lab now gives the canvas visual priority, with a wrapping shadcn-style command bar, dotted semantic frames, typed port handles, labeled connector provenance, status text and shape, synchronized inspector dock, scope mini-map, keyboard map, light/dark tokens, reduced-motion handling, and a narrow stacked layout.
- **Evidence:** The E01 journey now also carries E07 claim attribution and proves pointer/keyboard selection, pointer/keyboard pan, wheel/button/keyboard zoom, fit selection, fit scope, alignment, unchanged immutable projection, and structured equivalence. Reviewed screenshots 177–181 cover focused selection, panning, vertical alignment, dark theme, and narrow layout.
- **Finding:** E07 integrates the kernel with Lens Lab first. Specialized Story Map, Scenario Flow, State and Rule, System Context, and Traceability renderers should adopt shared viewport behavior only when their distinct grammar can be preserved; forcing them through generic node cards would erase domain meaning.
- **Finding:** Viewport and alignment are ephemeral. E08 owns personal/team layout definitions, reset, deterministic auto-layout input, concurrency, and the invariant that layout saves never advance the semantic revision.
- **Finding:** Pointer events are handled directly in Blazor without global input or JavaScript. Pointer capture beyond the SVG boundary and pinch gestures remain unclaimed until measured browser behavior requires the documented thin interop seam.
- **Owner:** Jerrett Davis (`@JerrettDavis`) with experience, accessibility, and projection owners when assigned.
- **Status:** E07 Canvas interaction kernel delivered on 2026-08-16. E08 layout persistence is the exact next slice.

## Canvas view persistence boundary

- **Decision:** E08 persists `custom` / `project-definition` viewport, alignment, deterministic input hash, and node geometry as a separately versioned canvas view. Personal and team records use distinct ownership keys and optimistic `LayoutVersion`; the semantic project revision is only a read baseline and never advances on save or reset.
- **Interface:** Lens Lab adds a theme-aware View Memory dock rather than a CRUD list. Modelers switch personal/team memory, save the live spatial workspace, recover it after reload, see stale baselines explicitly, and reset through visible keyboard-operable commands.
- **Evidence:** Real Kestrel, PostgreSQL, WebAssembly, and headless Chromium prove save, reload, isolation, reset, semantic-revision independence, dark theme, and narrow layout in screenshots 182–187.
- **Finding:** Runtime scope is deliberately limited to `custom` and `project-definition`. Other lenses must establish stable scope identities and map their distinct visual grammar before adopting persistence.
- **Finding:** Personal ownership currently uses the local-development actor subject and team ownership uses a shared team key. Production identity, membership, and team-write authorization remain owned by the security/identity track; E08 does not imply those controls.
- **Finding:** A layout saved against an older semantic revision loads as stale but is not automatically applied. Historical semantic plus matching historical view rendering needs an owned history contract.
- **Owner:** Jerrett Davis (`@JerrettDavis`) with experience, accessibility, security, and persistence owners when assigned.
- **Status:** E08 Canvas view persistence delivered on 2026-08-16. E09 drilldown and navigation is the exact next slice.

## Lens drilldown and location boundary

- **Decision:** E09 uses stable semantic identifiers in the Lens Lab `scope` query parameter. Focused scope is a projection location, not containment, selection, or semantic state; the full immutable `lens/1` response remains the input.
- **Interface:** Enter and a visible inspector command open the selected definition. Breadcrumbs preserve the project-definition root, a pinned context card keeps the parent visible, and explicitly labeled stubs expose definitions outside the current focus. Native browser history owns Back and Forward.
- **Evidence:** Real Kestrel, PostgreSQL, WebAssembly, and headless Chromium prove keyboard opening, copied deep links, lateral scope navigation, browser Back/Forward, dark theme, narrow responsive structure, and invalid-link recovery in screenshots 188–194.
- **Finding:** Cross-scope stubs deliberately do not claim semantic containment or relation provenance. A later destination registry must map supported semantic kinds to specialized Story Map, Scenario Flow, State and Rule, System Context, and Traceability routes without guessing.
- **Finding:** Invalid or removed semantic IDs produce an explicit recovery alert and root action. Incidental selection remains view state and is not encoded until the modeler opens it as a location.
- **Owner:** Jerrett Davis (`@JerrettDavis`) with experience, accessibility, and projection owners when assigned.
- **Status:** E09 Lens drilldown and navigation delivered on 2026-08-16. E10 scenario overlay is the exact next slice.

## Scenario explanation overlay boundary

- **Decision:** E10 extends `scenario-flow/1` with deterministic overlay packets sourced from an explicit Path source transition, state predicates, changed fact references, terminal state, participant observation, and invariant. The primary route explicitly reports when no invariant is linked.
- **Interface:** The Scenario Flow workbench now combines its branch deck and playback controls with a live state-delta card, participant observation monitor, invariant checkpoint, progress rail, and prominent review stop. Play, pause, previous, next, path tabs, keyboard stepping, and continuation are ordinary accessible controls.
- **Evidence:** Projection tests prove byte stability and exact authored overlay provenance. Real Kestrel, PostgreSQL, WebAssembly, and headless Chromium prove play/pause, stepping, branch selection, invariant stop, reviewed continuation, terminal and recovery states, dark theme, and responsive layout in screenshots 195–200.
- **Finding:** The invariant stop is a review checkpoint, not an invariant evaluation. Project Builder does not execute rules or target behavior and therefore never labels the modeled invariant passed or failed from playback alone.
- **Finding:** State deltas are path-level before and terminal snapshots. Intermediate fact changes need explicit step-to-transition bindings before the overlay can truthfully animate them.
- **Owner:** Jerrett Davis (`@JerrettDavis`) with model, experience, accessibility, and projection owners when assigned.
- **Status:** E10 Scenario explanation overlay delivered on 2026-08-16. F01 common interface model is the exact next slice.
