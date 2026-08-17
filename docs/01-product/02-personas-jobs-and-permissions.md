# Personas, Jobs, and Permissions

## Role model

Project Builder separates product personas from authorization roles.

A persona describes needs and context. An authorization role grants capabilities. A real user may match several personas and hold different roles in different workspaces or projects.

## Primary personas

### The Founder or Product Originator

**Situation:** Has a product or process idea but incomplete formal requirements.

**Jobs:**
- Explain what outcome should exist.
- Identify who benefits and who participates.
- Discover missing interactions.
- understand what must be decided before implementation.
- Produce a reviewable plan for design and engineering.

**Needs:**
- Ordinary language first.
- Examples and prompts.
- Visual progress without false certainty.
- Freedom to leave technical choices open.
- A coherent handoff package.

**Failure modes to prevent:**
- Selecting technologies before behavior is understood.
- Treating a feature list as a system model.
- Accepting agent-generated detail that has no authority.

### The Facilitator or Business Analyst

**Situation:** Leads discovery across stakeholders with different vocabularies and incentives.

**Jobs:**
- Capture episodes and scenarios.
- Reconcile terms.
- expose assumptions and contradictions.
- Identify unhappy paths and organizational boundaries.
- Prepare review sessions.

**Needs:**
- Fast structured capture during conversation.
- Parking lot and unknown states.
- Multiple stakeholder views over shared facts.
- Comments, decisions, and source references.
- A clear gap map.

### The Domain Expert

**Situation:** Knows the work, policies, exceptions, and operational reality but may not speak in software terms.

**Jobs:**
- Validate terminology and behavior.
- Explain invariants and exceptions.
- Identify authority and consequences.
- Review generated scenarios in recognizable language.

**Needs:**
- Minimal technical jargon.
- Scenario playback.
- State and rule explanations tied to examples.
- Ability to contest or annotate claims without editing architecture.

### The Experience Designer

**Situation:** Translates actor goals and system state into usable interfaces.

**Jobs:**
- Decide what state should be visible.
- Design interaction surfaces.
- Map controls to intents.
- represent validation, loading, empty, degraded, and error states.
- Verify accessibility and journey continuity.

**Needs:**
- Figma-like composition.
- Direct access to scenario and state context.
- Separation of domain and presentation state.
- Reusable components and variants.
- Scenario overlays and focus-order tools.

### The Architect

**Situation:** Determines boundaries, qualities, contracts, and system structure.

**Jobs:**
- Identify bounded contexts and ownership.
- Map external systems and trust boundaries.
- Select architecture based on behavior and quality needs.
- Record alternatives and consequences.
- Derive deployment and operational concerns.

**Needs:**
- System, data, decision, and risk lenses.
- Cross-boundary path analysis.
- Quality attribute scenarios.
- Impact analysis.
- ADR generation.

### The Engineer

**Situation:** Implements a vertical slice and needs precise scope without drowning in the entire model.

**Jobs:**
- Understand the behavior to implement.
- identify domain rules and state.
- Implement ports, adapters, and interface behavior.
- return evidence.
- Report discovered divergence.

**Needs:**
- Bounded work package.
- Stable model identifiers.
- generated contracts and test candidates.
- clear fixed versus open decisions.
- traceability from code and tests back to claims.

### The Validator or Quality Engineer

**Situation:** Decides how claims will be tested and whether evidence is persuasive.

**Jobs:**
- Classify claims.
- choose evidence layers.
- identify missing properties and contracts.
- validate happy, failure, recovery, security, accessibility, and operational behavior.
- mark evidence stale when definitions change.

**Needs:**
- Traceability matrix.
- test and evidence status.
- model diff impact.
- environment and data requirements.
- distinction between examples and broader properties.

### The Workspace Administrator

**Situation:** Governs access, policy, retention, integrations, and extensions.

**Jobs:**
- Manage users and roles.
- configure identity.
- set validation policies.
- manage export and retention.
- audit activity.
- approve plugins or agent connections.

**Needs:**
- Least-privilege controls.
- clear policy inheritance.
- audit search.
- backup and restore.
- extension isolation.

## Secondary personas

### The Support or Operations Specialist
Models incident, recovery, degradation, escalation, and operational procedures.

### The Compliance or Security Reviewer
Reviews sensitive data, authority, trust boundaries, controls, evidence, and waivers.

### The Educator or Mentor
Uses the product to teach how domains become systems through guided examples.

### The Maintainer of an Existing System
Imports or reconstructs behavior from code, documents, logs, tickets, and interviews.

## Jobs to be done

### Discovery job
"When I have an idea or poorly documented process, help me expose the actors, outcomes, conditions, and exceptions so that I can tell what is known and what remains unresolved."

### Design job
"When behavior is understood, help me decide what each participant sees and does without confusing interface state with domain truth."

### Architecture job
"When interactions cross responsibilities or systems, help me identify boundaries, contracts, qualities, and risks before selecting mechanisms."

### Delivery job
"When a slice is ready to build, give the implementer exactly the relevant model, decisions, contracts, and evidence expectations."

### Validation job
"When implementation exists, show which claims have persuasive evidence and which have changed, failed, or gone stale."

### Change job
"When reality or requirements change, show what definitions, interfaces, contracts, code, tests, and operations are impacted."

## Permission model

### Workspace roles

| Role | Core permissions |
|---|---|
| Owner | Full control, billing, deletion, identity, export, policy |
| Administrator | Members, policy, integrations, templates, audit |
| Member | Create projects subject to policy |
| Guest | Access explicitly shared projects |
| Auditor | Read approved content, history, evidence, and audit logs |

### Project roles

| Role | Model | Comment | Review | Baseline | Generate | Admin |
|---|---:|---:|---:|---:|---:|---:|
| Project Owner | edit | yes | approve | create | yes | project |
| Modeler | edit | yes | request | no | preview | no |
| Designer | interface edit | yes | request | no | preview | no |
| Architect | architecture edit | yes | request | no | preview | no |
| Engineer | implementation fields | yes | request | no | execute allowed projections | no |
| Validator | evidence edit | yes | approve evidence | no | test projections | no |
| Reviewer | no | yes | approve/reject | no | preview | no |
| Viewer | no | optional | no | no | no | no |

Permissions are claims-based and scoped. Roles are conveniences, not hard-coded authorization logic.

## Approval authority

A baseline can require approvals by claim category. For example:

- Domain behavior: domain expert and project owner.
- Interface behavior: designer and domain expert.
- Architecture: architect and engineering owner.
- Security-sensitive boundary: security reviewer.
- Evidence sufficiency: validator.
- Release baseline: project owner.

The product must show missing authority rather than treating any approval as interchangeable.

## Agent identity

Agentic actions use a distinct service identity and never inherit broad user access implicitly. Every proposal records:

- requesting user,
- agent or model,
- tool permissions,
- source context,
- generated operations,
- human disposition,
- cost and latency metadata when available.

An agent cannot approve its own proposal or mark its own output as evidence.
