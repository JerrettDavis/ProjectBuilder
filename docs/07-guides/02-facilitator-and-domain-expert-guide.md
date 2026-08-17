# Facilitator and Domain Expert Guide

## Purpose

This guide explains how to use Project Builder in collaborative discovery and definition sessions. The facilitator protects flow, precision, and participation. Domain experts provide lived reality, rules, exceptions, vocabulary, and sources. Neither role is expected to design the entire software architecture.

The goal is not to finish every field. The goal is to turn tacit understanding into an inspectable model whose decisions, assumptions, unknowns, and disputes are visible.

## Roles in a session

### Facilitator

- sets scope and intended outcome,
- keeps the conversation at the current abstraction level,
- asks for examples and counterexamples,
- distinguishes facts, rules, preferences, assumptions, and implementation ideas,
- records parking-lot topics,
- ensures minority and operational perspectives are heard,
- stops false consensus,
- summarizes and confirms model changes.

### Domain expert

- describes what actually happens,
- identifies authority and responsibility,
- names concepts in domain language,
- supplies examples, edge cases, and historical failures,
- identifies sources and policy owners,
- distinguishes local practice from universal rule,
- marks uncertainty honestly.

### Modeler

The facilitator may also model, but a separate modeler can reduce interruption. The modeler translates statements into elements, relations, paths, and knowledge states without quietly changing their meaning.

### Decision owner

Some disputes need an accountable owner. Their role is to choose or authorize risk, not to erase evidence of disagreement.

### Observer or reviewer

Security, operations, support, accessibility, legal, finance, and engineering participants may observe and contribute when their boundary becomes material.

## Preparing a workshop

### Choose one outcome-bearing episode

A strong workshop scope is:

> A store clerk adds a scanned product to an active sale and sees the correct price.

A weak scope is:

> Model the whole point-of-sale platform.

### Prepare sources

Collect:

- current procedures,
- screenshots or forms,
- policies,
- examples and logs,
- contracts,
- support tickets,
- known incidents,
- regulatory or accessibility constraints,
- vocabulary and abbreviations.

Sources can be incomplete. Record their authority and date.

### Prepare participants

Invite people who can speak to:

- primary execution,
- policy and exceptions,
- downstream effects,
- support and recovery,
- system boundaries,
- user experience,
- authorization and risk.

Avoid a room composed only of managers or only of implementers.

### Configure the project

- create or select the project,
- choose Discovery purpose,
- create a workshop view,
- set the target episode,
- open Guide Rail and Problems,
- prepare a Parking Lot,
- confirm recording and information-classification rules.

## Recommended workshop structure

### 1. Frame the outcome, 5 to 10 minutes

Ask:

- Who needs what result?
- What observable change indicates success?
- Where does this episode begin and end?
- What are we explicitly not modeling today?
- What would make this session worthwhile?

Record purpose, outcome, scope, and constraints.

### 2. Identify actors and authority, 10 minutes

Ask:

- Who starts the work?
- Who benefits?
- Who approves, blocks, advises, or supports?
- Which systems or devices participate?
- Who owns the policy?
- Who can override it?
- Who is affected when it fails?

Capture roles rather than names unless a named legal or organizational entity is the relevant actor.

### 3. Tell the ordinary story, 15 minutes

Ask one participant to narrate a real example from trigger to observable completion.

The facilitator should resist premature branches. Capture the happy scenario first while marking possible deviations in the Parking Lot.

Use scenes when:

- location or channel changes,
- responsibility changes,
- interface changes,
- time is discontinuous,
- a boundary is crossed.

### 4. Clarify interactions, 15 minutes

For each exchange ask:

- What initiates this?
- What exactly is sent, shown, said, scanned, or observed?
- What does the receiver understand it to mean?
- Is the initiator authorized?
- What response is observable?
- Is silence meaningful?
- What boundary is crossed?
- What timing matters?

Avoid compressing several decisions into "the system validates it."

### 5. Expose state and rules, 20 minutes

Ask:

- What must already be true?
- What facts are read?
- What can change?
- What must never become false?
- Which values are calculated?
- Who owns the rule?
- Is this policy, physical reality, convention, or implementation?
- Does the rule vary by store, customer, time, jurisdiction, product, or channel?
- How do we know it is current?

Use examples and counterexamples. Convert recurring decisions into explicit rules.

### 6. Walk the paths, 20 minutes

Use a path checklist:

- invalid input,
- unknown identity,
- ineligible actor or object,
- missing or stale data,
- duplicate action,
- concurrency conflict,
- unavailable dependency,
- timeout,
- partial success,
- cancellation,
- override,
- retry,
- compensation,
- downstream rejection,
- late-arriving result,
- support intervention.

For each, ask what the actor sees and what remains true.

### 7. Review gaps and decisions, 10 minutes

Open Problems and classify each material issue:

- answer now,
- assign research,
- mark assumption,
- record dispute,
- defer with reason,
- accept risk,
- not applicable.

Do not chase low-value completeness while a critical invariant remains unclear.

### 8. Confirm and commit, 10 minutes

Read back:

- outcome,
- actors,
- scenario,
- state change,
- invariant,
- major failures,
- assumptions,
- decisions,
- next owner.

Preview the change set, add the session reason, and commit.

## Facilitation techniques

### Ask for a recent concrete instance

Instead of "How do coupons work?" ask:

> Tell me about the last time a manufacturer coupon was rejected at the register.

Concrete instances expose hidden state and handoffs.

### Use the counterexample loop

1. State a rule.
2. Ask for an example that satisfies it.
3. Ask for an example that should not.
4. Ask whether the wording distinguishes them.
5. refine the rule or split contexts.

### Separate observed reality from desired design

Record both:

- **Current:** clerk calls a manager because the register gives no explanation.
- **Desired:** register shows the policy result and required override authority.

Do not overwrite the current-state model with the proposed future state.

### Track levels of abstraction

When discussion dives into a lower layer, either:

- open the interaction as a child context,
- record a boundary question,
- place it in the Parking Lot.

Return to the outer outcome before the group loses the story.

### Challenge universal words

Words such as always, never, all, only, immediately, unique, valid, and complete often imply invariants or unexamined exceptions. Ask for authority and counterexamples.

### Preserve disagreement

A disputed claim is better than a falsely agreed claim. Record viewpoints, sources, affected decision, and owner.

### Give quiet participants structured entry points

Ask role-specific questions:

- support: "How does this fail in production?"
- operations: "What happens during maintenance?"
- security: "Who should not be able to do this?"
- accessibility: "Can a user complete this without vision or a pointer?"
- finance: "When is the monetary effect considered final?"
- domain operator: "What workaround do people use today?"

## Modeling current and future states

Use separate contexts, baselines, or explicit relations such as **replaces**, **migrates from**, and **proposed alternative**. Never use canvas color alone to distinguish current from future truth.

A useful sequence:

1. capture current behavior,
2. identify pain and outcome,
3. model proposed behavior,
4. compare changed actors, rules, state, boundaries, and risks,
5. record transition and rollout behavior.

## Remote workshop mode

- publish the project scope before the session,
- use one designated editor,
- allow participants to add comments and sources,
- show the Guide Rail and current selection,
- keep a visible Parking Lot,
- pause after each scene for correction,
- use reaction or turn-taking policy,
- commit small coherent revisions rather than one opaque workshop dump.

## Workshop artifacts

At completion, produce:

- model revision link,
- workshop summary,
- actor and outcome map,
- selected scenario path,
- state and invariant summary,
- decisions,
- assumptions and unknowns,
- disputes,
- evidence and sources,
- assigned follow-ups,
- purpose-profile status.

These are projections from the canonical model wherever possible.

## Anti-patterns

### Filling templates silently

A facilitator who fills gaps by inference creates a polished but false model. Use explicit knowledge states.

### Letting the loudest participant define reality

Seek operational examples and sources. Record disputes.

### Modeling organizational titles instead of roles

Titles vary. Model the authority and responsibility used in the episode.

### Treating the current application as the domain

A legacy screen may encode accidental constraints. Ask what outcome and rule it serves.

### Solving every problem during discovery

Capture decisions that are ripe. Create bounded spikes for uncertain technical or policy questions.

### Ending without ownership

Every blocking Unknown, Assumption, Dispute, and accepted risk needs an owner or explicit governance decision.

## Session quality checklist

- Did we model an outcome rather than a feature list?
- Did a real operator tell a concrete story?
- Are actors and authority explicit?
- Are state and interface state separate?
- Is at least one invariant stated in falsifiable language?
- Did we examine meaningful failure and recovery?
- Are external boundaries and sources visible?
- Are assumptions and disputes preserved?
- Can the model explain what should be tested next?
- Did participants confirm the final summary?
