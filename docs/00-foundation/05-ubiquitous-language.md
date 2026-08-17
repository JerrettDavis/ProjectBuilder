# Ubiquitous Language

This glossary is normative for product and code terminology. A term can be refined through an ADR and model migration. Synonyms may appear in the user interface for approachability, but stored model concepts use the canonical names.

## Core scope

### Project
A governed model of a declared universe of discourse created for a stated purpose.

### Workspace
An ownership and collaboration container for projects, members, policy, and billing or administration.

### Context
A bounded area in which terms and rules have a consistent meaning. A context can represent a business domain, organizational area, product, subsystem, or implementation boundary.

### Capability
An ability the organization or system must possess to produce outcomes. Capabilities are stable descriptions of "what," independent of a specific workflow or implementation.

### Outcome
An observable result valuable to a beneficiary. An outcome states how success can be recognized.

## Participants

### Actor
A role that can initiate or participate in behavior. Actors may be human, organizational, system, device, or scheduled/automated roles. "Clerk" is an actor. "Jerrett Davis" is a person who may fulfill an actor role.

### Participant
Any actor or passive party involved in a modeled element. The term is useful when initiation is not implied.

### Persona
A research-backed profile used to reason about needs, context, abilities, and constraints. A persona can fulfill one or more actor roles.

### System
A cohesive set of responsibilities treated as a unit from the current modeling perspective. A system at one level can be opened into many systems at the next.

### External system
A system outside the current ownership or implementation boundary. Its business meaning can still be part of the domain.

### Device
A physical participant or interface, such as a barcode scanner, payment terminal, sensor, or printer.

## Narrative behavior

### Episode
An end-to-end span of activity that produces a meaningful outcome for an actor or organization. "Complete a retail sale" is an episode.

### Scenario
A concrete path through an episode under stated conditions. A scenario has a trigger, starting facts, participants, ordered scenes, expected outcome, and path classification.

### Scene
A contiguous segment of a scenario during which the primary setting, responsibility, interface, or boundary remains stable. "Scan and classify an item" can be a scene.

### Interaction
An observable exchange in which an initiator expresses an intent through an interface and a receiver produces an observation or effect.

### Step
The smallest ordered unit needed to explain an interaction. A step is not necessarily an implementation method call.

### Intent
What an actor or participant is trying to cause. An intent is expressed without assuming the implementation mechanism.

### Command
A validated request for the application or domain to attempt a state change. A command can be derived from an intent.

### Observation
Information made visible or available to a participant after or during behavior.

## State and truth

### Fact
A named assertion accepted within a context at a point in time.

### State
The set of relevant facts at a point in time.

### Domain state
Facts whose meaning belongs to the modeled reality and business rules.

### Application workflow state
Progress, coordination, idempotency, timeout, and orchestration facts required to execute use cases.

### Presentation state
Facts needed to render or operate an interface, such as selection, focus, expansion, draft input, or active tab.

### View state
Layout and personalization facts for a Project Builder lens, including positions, zoom, grouping, and visibility.

### Infrastructure state
Provider, transport, storage, connection, and operational facts that belong to technical mechanisms.

### Event
A named fact that something relevant occurred. Events are past tense and immutable as statements.

### Transition
A rule-governed movement from one valid state to another.

### Rule
A named constraint, derivation, eligibility test, calculation, or decision that applies within a context.

### Invariant
A property that must remain true for every valid state in its declared scope.

### Property
A general behavioral claim that should hold over a range of examples or generated inputs.

### Condition
A proposition evaluated to choose or permit behavior.

## Paths and outcomes

### Happy path
The expected successful path under ordinary valid conditions.

### Alternate path
A valid path that produces the intended outcome differently.

### Exceptional path
A path caused by invalid input, denied authority, unavailable dependency, violated precondition, or unexpected failure.

### Degraded path
A path that preserves partial service or a reduced outcome under constrained conditions.

### Recovery path
Behavior that restores a safe state or resumes progress after failure.

### Compensation
A domain or application action that semantically counteracts a previously completed effect. Compensation is not automatically equivalent to rollback.

### Outcome state
Succeeded, failed, denied, cancelled, timed out, partially completed, deferred, or another explicitly modeled result.

## Boundaries and architecture

### Boundary
A line across which ownership, trust, responsibility, transactionality, deployment, data residency, protocol, or operational control changes.

### Interface
A surface through which an intent, observation, or contract crosses a boundary. Interface kinds include graphical UI, CLI, HTTP API, message/event, MCP, device, document, and human procedure.

### Contract
A versioned agreement about inputs, outputs, semantics, errors, timing, authorization, and compatibility at a boundary.

### Port
A capability required or exposed by the application or domain, expressed without a provider mechanism.

### Adapter
An implementation that maps a port to a concrete interface, provider, protocol, store, or device.

### Vertical slice
A cohesive implementation projection for one behavior from entry surface through application orchestration, domain truth, infrastructure effects, observations, and evidence.

### Domain
The facts, language, rules, invariants, and transitions that model the relevant reality.

### Application
Use-case-specific orchestration and policy that coordinates domain behavior and effects without defining external mechanisms.

### Infrastructure
Concrete external mechanisms, providers, transports, stores, devices, and adapters.

### Presentation
The adapters through which people or systems express intents and receive observations.

## Definition and validation

### Claim
A statement that the project treats as requiring authority and evidence.

### Definition
A governed collection of model claims that constrains acceptable implementation.

### Assumption
A provisional claim accepted for progress but not yet sufficiently validated.

### Decision
A selected choice with context, alternatives, rationale, consequences, and status.

### Evidence
An artifact or observation used to support or refute a claim.

### Gap
A known absence, ambiguity, contradiction, unsupported claim, or incomplete path that matters to a declared purpose.

### Finding
A validator result. A finding can be informational, warning, error, or blocker.

### Baseline
A named, immutable project revision used for review, comparison, generation, or release.

### Change set
An atomic, reviewable collection of model operations with author, reason, base revision, and resulting revision.

### Projection
A deterministic derivation of selected model content into a view or artifact.

### Lens
An interactive projection optimized for a modeling purpose, such as story, state, interface, system, or evidence.

### Evidence status
Unspecified, planned, available, passing, failing, stale, disputed, or waived.

### Definition status
Draft, defined, reviewed, validated, deprecated, or superseded.
