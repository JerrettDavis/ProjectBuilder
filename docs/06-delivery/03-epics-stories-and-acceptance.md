# Epics, Stories, and Acceptance

## How to use this catalog

This is a delivery seed, not a substitute for modeling each feature in Project Builder. Every story must be refined into:

- actor and intended outcome,
- context and starting facts,
- happy, alternate, failure, recovery, and cancellation paths as applicable,
- state transition and invariant claims,
- interface behavior,
- authorization,
- operational properties,
- evidence plan,
- smallest independently reviewable vertical slice.

Stable epic identifiers should be retained in issues and model traceability.

## EPIC PB-001: establish a trustworthy repository

### PB-001.1 Bootstrap the .NET solution

**As a** contributor  
**I need** one deterministic repository entry point  
**So that** local and CI behavior converge.

Acceptance:

- `ProjectBuilder.slnx` targets .NET 10.
- SDK and package policy are centralized.
- clean restore, build, test, and run commands are documented.
- build metadata is visible in the running application.
- warnings and architecture violations fail CI.
- generated artifacts do not require machine-specific paths.

### PB-001.2 Enforce module boundaries

Acceptance:

- Domain references no UI, persistence, web, provider, or framework-specific application package.
- Application depends on Domain and owned port contracts.
- Infrastructure implements ports without leaking provider types into Application.
- Web depends on application contracts rather than persistence.
- architecture tests name the violated rule and reference.

### PB-001.3 Establish evidence-producing CI

Acceptance:

- CI publishes test, coverage, schema, formatting, dependency, security, and build artifacts.
- local scripts invoke the same logical checks.
- test failures retain useful logs and result files.
- change-only optimization never skips a required release check.

## EPIC PB-010: projects, workspaces, and purpose

### PB-010.1 Create a project

**As a** modeler  
**I need** to state what I am building and why  
**So that** every later decision has a scope and intended outcome.

Acceptance:

- name, purpose, intended outcome, and workspace are captured.
- blank and invalid values return semantic errors.
- duplicate names follow workspace policy.
- creation produces revision 1 and a change-set reason.
- the creator receives an allowed next action.
- unauthorized users cannot create in the workspace.

### PB-010.2 Select a modeling purpose

Acceptance:

- user can select Discovery, Interface Design, Architecture, Implementation Ready, Release Ready, or a registered custom profile.
- requirements and guidance change by purpose without deleting existing content.
- profile completion explains every unmet rule.
- changing profiles is revisioned when it changes project governance, not when it is merely a private view preference.

### PB-010.3 Capture constraints and sources

Acceptance:

- constraints have kind, scope, statement, authority, effective period, and knowledge status.
- source links or attachments can support a claim.
- unverified assumptions cannot display as verified decisions.
- expired or superseded constraints produce findings.

## EPIC PB-020: actors, authority, and outcomes

### PB-020.1 Add an actor

Acceptance:

- actor kind and contextual role are required.
- person records and roles are not conflated.
- responsibilities, goals, permissions, and constraints can be recorded separately.
- duplicate suggestions are non-destructive.
- actors can be reused across scenarios by relation.

### PB-020.2 Define authority and participation

Acceptance:

- interactions identify initiator and receiver.
- authority can be allowed, denied, conditional, delegated, or unknown.
- a human role can act through a device or interface without making the device the business authority.
- missing authority for a mutating intent produces a finding.

### PB-020.3 Define an outcome

Acceptance:

- outcome identifies beneficiary and observable success signal.
- output is not accepted as outcome without an observable effect.
- conflicts among stakeholder outcomes can be recorded.
- a scenario can satisfy, partially satisfy, harm, or not affect an outcome.

## EPIC PB-030: narrative behavior

### PB-030.1 Create an episode

Acceptance:

- episode states trigger, end condition, participant scope, outcome, and context.
- an episode can contain scenarios and scenes.
- episodes remain implementation-neutral.
- vague verbs produce coaching, not automatic rejection.

### PB-030.2 Describe a scenario

Acceptance:

- starting facts, trigger, expected outcome, classification, and ordered scenes are explicit.
- scenario has at least one actor or an intentional system-only classification.
- variants can derive from a base scenario without hidden inheritance.
- scenario examples can bind concrete data.

### PB-030.3 Add scenes, interactions, and steps

Acceptance:

- every interaction has initiator, receiver, interface, intent, and observation or explicit no-response expectation.
- steps explain an interaction but cannot silently introduce unmodeled semantic state.
- boundary crossings are visible.
- ordered content can be changed without changing element identity.

## EPIC PB-040: paths, state, rules, and invariants

### PB-040.1 Define path branches

Acceptance:

- branch condition and semantic outcome are explicit.
- path classifications are typed.
- failure and recovery can be connected without pretending recovery is success.
- unreachable branches and unhandled results produce findings.

### PB-040.2 Define state

Acceptance:

- presentation, application-workflow, domain, infrastructure, and externally observed state categories are distinct.
- current state can be Unknown when discovery is incomplete.
- state values and structures are versioned model elements.
- interface layout state does not become business state.

### PB-040.3 Define transitions

Acceptance:

- transition specifies source, trigger, guard, result, target, events, and effects as applicable.
- rejected commands do not imply a successful transition.
- transitions preserve all applicable invariants.
- transition tables can be generated and reviewed.

### PB-040.4 Define rules and invariants

Acceptance:

- rule kind distinguishes validation, eligibility, decision, derivation, calculation, and policy.
- invariant has scope and falsification evidence.
- contradictions and shadowed rules are detectable where decidable.
- every implementation-ready invariant has at least one planned proof.

## EPIC PB-050: revisions, drafts, and portable files

### PB-050.1 Edit a private draft

Acceptance:

- uncommitted changes survive navigation and recoverable browser interruption.
- draft operations support undo and redo.
- draft does not change shared revision.
- user can discard or preview draft.

### PB-050.2 Commit a change set

Acceptance:

- base revision and reason are required.
- server validates authorization, concurrency, schema, model rules, and invariant preservation.
- success produces one atomic revision.
- stale revision returns a structured conflict without overwriting content.
- idempotency prevents duplicate commits.

### PB-050.3 Inspect and compare history

Acceptance:

- history identifies actor, time, reason, affected claims, and operations.
- comparison distinguishes semantic, governance, layout, and evidence changes.
- user can open either revision read-only.
- reversal creates a new change set rather than erasing history.

### PB-050.4 Import and export

Acceptance:

- canonical export is deterministic for a revision.
- schema and semantic validation occur before persistence.
- unknown extension kinds follow explicit policy.
- import cannot execute content.
- format migration is transactional and records provenance.

## EPIC PB-060: guidance and completeness

### PB-060.1 Open contextual guidance

Acceptance:

- Guide Rail reflects project purpose, selection, findings, and recent answers.
- prompt states why it matters and what will change.
- user can answer, link existing, mark Unknown, Assumed, Deferred, Disputed, or Not Applicable.
- reopening preserves position.
- guidance never blocks free studio navigation.

### PB-060.2 Compute purpose-specific completeness

Acceptance:

- result lists satisfied, unmet, waived, deferred, and not-applicable rules.
- weighted score never hides a blocking invariant.
- waivers require authority and reason.
- rule versions are included in baseline results.
- private display preferences do not affect completeness.

### PB-060.3 Show the gap map

Acceptance:

- findings group by scope, severity, knowledge state, purpose profile, and owner.
- each finding links to repair actions and supporting rationale.
- dismissing a finding requires resolution, accepted risk, or rule change.
- closed findings remain traceable to the revision in which they were resolved.

## EPIC PB-070: lenses and canvas

### PB-070.1 Project a model into a lens

Acceptance:

- lens declares supported element and relation kinds.
- projection is deterministic for model revision and lens settings.
- unsupported content is reported, not silently dropped.
- selected node resolves to canonical element identity.

### PB-070.2 Navigate and edit the canvas

Acceptance:

- pointer and keyboard can select, connect, move, open, and inspect.
- semantic outline exposes equivalent content.
- view movement persists without semantic revision.
- edit command previews semantic consequences.
- canvas remains usable at the published reference model size.

### PB-070.3 Drill through abstraction

Acceptance:

- open action enters the selected scope.
- breadcrumbs restore parent context.
- cross-scope links remain visible through boundary stubs.
- back/forward navigation preserves selection and viewport.

## EPIC PB-080: interface modeling

### PB-080.1 Define an interface

Acceptance:

- interface kind, participants, authority, accepted intents, observations, state, errors, constraints, and contract are capturable.
- graphical, CLI, HTTP/RPC, event, MCP, device, document, and human-procedure kinds receive appropriate terminology.
- interface does not own domain truth by default.
- external interfaces are marked as owned, influenced, or merely observed.

### PB-080.2 Design graphical states

Acceptance:

- frames, regions, controls, content, focus order, and responsive constraints can be modeled.
- controls bind to application intents and read-model fields.
- loading, empty, denied, invalid, partial, failed, degraded, and success states are explicit.
- accessible names and keyboard behavior can be defined.
- visual styling remains separable from behavior.

### PB-080.3 Play a scenario over an interface

Acceptance:

- player begins from explicit facts and interface state.
- each step identifies actor action, accepted intent, semantic result, state change, effects, and observation.
- alternate branches can be selected.
- playback never commits changes to the target model.
- failed invariant halts playback and explains the claim.

## EPIC PB-090: architecture and implementation slices

### PB-090.1 Define boundaries

Acceptance:

- boundary type and ownership are explicit.
- crossings can carry contract, security, privacy, reliability, latency, and recovery properties.
- trust-boundary crossings trigger threat prompts.
- vendor boundaries record exit and degradation assumptions.

### PB-090.2 Decompose an interaction

Acceptance:

- an interaction can open an inner context with new actors, systems, scenes, and interactions.
- outer intent and observation trace to inner behavior.
- decomposition does not require technical detail at the outer level.
- unresolved mapping is visible.

### PB-090.3 Project a vertical slice

Acceptance:

- projection identifies Presentation adapter, Application use case, Domain facts/rules/transitions/results, Infrastructure ports/adapters, contracts, and evidence.
- empty layers are allowed when justified.
- external reality is not mislabeled as Domain merely because it is important.
- implementation choices remain separate from behavioral claims.

## EPIC PB-100: specifications, tests, and evidence

### PB-100.1 Generate behavioral specifications

Acceptance:

- output includes scenario identity and revision.
- examples, preconditions, action, outcomes, and invariant checks are preserved.
- generated wording is deterministic and reviewable.
- unsupported ambiguity produces TODO findings rather than invented detail.

### PB-100.2 Generate contract and state artifacts

Acceptance:

- schemas and tables are valid against their target standards or internal schema.
- version and source revision are embedded.
- compatibility changes are classified.
- generated artifacts are clearly marked as projections.

### PB-100.3 Record evidence

Acceptance:

- evidence has type, producer, timestamp, environment, result, artifact location, and covered claims.
- planned, produced, passed, failed, stale, superseded, and accepted-risk statuses are distinct.
- stale evidence is detected after relevant claim changes.
- an agent statement cannot be marked as executable proof.

### PB-100.4 Create a release baseline

Acceptance:

- baseline pins model revision, rule set, projection versions, and evidence.
- review records approver and disposition.
- baseline is immutable; supersession creates another baseline.
- release report exposes unresolved risk.

## EPIC PB-110: collaboration, governance, and administration

### PB-110.1 Invite and authorize users

Acceptance:

- workspace roles are least-privilege.
- project-level roles can narrow but not silently expand workspace authority.
- authorization is server enforced.
- tenant-crossing identifiers do not leak existence.

### PB-110.2 Comment and review

Acceptance:

- comments anchor to project, element, relation, finding, change operation, or projection location.
- comment resolution does not alter semantic truth.
- review can approve, request changes, or record accepted risk.
- notifications honor user and organization policy.

### PB-110.3 Resolve edit conflicts

Acceptance:

- presence is advisory.
- stale commit receives element-level conflict detail where safe.
- user can rebase, select operations, or abandon.
- automatic merge occurs only for proven non-conflicting operations.

### PB-110.4 Administer retention and recovery

Acceptance:

- backups are encrypted, restorable, and rehearsed.
- retention and deletion policies are explicit.
- audit access is authorized and recorded.
- tenant export and deletion include all modeled content and attachments according to policy.

## EPIC PB-120: dogfooding and extension

### PB-120.1 Model Project Builder

Acceptance:

- every shipped user-visible behavior traces to the dogfood project.
- purpose profiles report no unexplained blocking gaps for the release baseline.
- release evidence is linked.
- discrepancies between docs, model, and implementation become tracked findings.

### PB-120.2 Register model extensions

Acceptance:

- extension declares namespace, version, element/relation kinds, schema, prompts, validators, editors, projections, migration, and compatibility.
- unknown extensions are preserved or rejected according to project policy.
- extensions cannot bypass authorization or import safety.
- core domain can run without any third-party extension.

### PB-120.3 Add optional agent proposals

Acceptance:

- proposal is a normal uncommitted change set with provenance.
- user can inspect, edit, partially apply, or reject.
- citations and confidence are retained where provided.
- policy can disable providers, data classes, or all agent features.
- product remains fully operable when agent services are unavailable.
