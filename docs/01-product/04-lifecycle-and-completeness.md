# Project Lifecycle and Completeness

## Lifecycle states

A Project Builder project moves through overlapping modeling and governance states. The product should not impose a single linear lifecycle on every organization.

### Exploring
The team is discovering vocabulary, actors, outcomes, and broad behavior. Contradictions and unknowns are expected.

### Defining
Selected episodes or slices are being formalized with state, rules, paths, interfaces, and boundaries.

### Reviewing
A declared scope is frozen into a candidate baseline and routed to people with relevant authority.

### Ready
A bounded slice has enough definition, decisions, contracts, and evidence expectations to begin implementation.

### Implementing
Code, configuration, procedures, or generated artifacts are being produced against a baseline.

### Validating
Evidence is being collected and compared to definitions.

### Operating
The implemented system is producing observations and operational evidence.

### Refining
Learning has caused definitions, decisions, interfaces, or evidence to change.

A project can contain episodes in several states simultaneously.

## Element definition status

| Status | Meaning |
|---|---|
| Draft | Captured but incomplete or not yet reviewed |
| Defined | Required fields and local semantic checks pass |
| Reviewed | Relevant authority has reviewed the current revision |
| Validated | Required evidence or authoritative confirmation exists |
| Deprecated | Retained for history but should not be used for new work |
| Superseded | Replaced by an identified element or revision |

Definition status is not evidence status. A scenario can be reviewed but have no implementation evidence.

## Evidence status

| Status | Meaning |
|---|---|
| Unspecified | No evidence requirement selected |
| Planned | Evidence type and owner identified |
| Available | Artifact exists but result not evaluated |
| Passing | Evidence supports the claim for the stated scope |
| Failing | Evidence contradicts the claim or implementation |
| Stale | Definition, implementation, environment, or dependency changed |
| Disputed | Reviewers disagree about sufficiency or interpretation |
| Waived | Authorized exception with rationale and expiration |

## Purpose profiles

Completeness is evaluated against a declared purpose.

### Discovery profile
Requires clear intent, outcomes, actors, major episodes, known assumptions, and material gaps.

### Interface design profile
Requires scenarios, visible state, intents, feedback, key alternate and failure paths, accessibility constraints, and unresolved domain questions.

### Architecture profile
Requires system context, boundaries, contracts, quality scenarios, data classification, failure behavior, decisions, and risks.

### Implementation-ready profile
Requires a bounded vertical slice, defined state and rules, contracts, fixed decisions, acceptance claims, test strategy, and no blocking gaps.

### Release-ready profile
Requires implementation evidence, security and accessibility checks, operations and recovery readiness, migration plan, and approved baseline.

Teams can create stricter profiles. A profile cannot redefine a structural invalidity as complete.

## Coverage dimensions

Project Builder reports a matrix rather than one percentage.

| Dimension | Questions |
|---|---|
| Purpose | Is the desired outcome and beneficiary clear? |
| Participants | Are initiating, receiving, affected, and authoritative actors known? |
| Behavior | Are trigger, interactions, observations, and outcomes defined? |
| State | Are starting state, changed facts, derived facts, and final state explicit? |
| Rules | Are preconditions, decisions, calculations, and invariants defined? |
| Paths | Are alternate, failure, degraded, cancellation, and recovery paths addressed? |
| Interfaces | Can each intent and observation cross an identified surface? |
| Boundaries | Are ownership, trust, contract, and operational changes explicit? |
| Qualities | Are relevant performance, reliability, security, privacy, and accessibility needs defined? |
| Decisions | Are material choices and consequences recorded? |
| Evidence | Does each material claim have proportionate proof planned or available? |
| Authority | Have the right people reviewed the right claims? |

Each cell can be:

- not started,
- partially defined,
- defined,
- verified,
- not applicable with rationale,
- blocked,
- disputed.

## Gap severity

### Informational
A refinement could improve clarity but does not block the declared purpose.

### Warning
A material question remains, but work can continue with an explicit assumption or controlled risk.

### Error
The current element is inconsistent, invalid, or cannot support the declared purpose.

### Blocker
Continuing would create unacceptable ambiguity, safety, security, data loss, or irreversible rework.

Severity can depend on profile. Missing keyboard behavior is a blocker for release-ready interface work but may be a warning during early discovery.

## Readiness calculation

Readiness is a rule evaluation, not a weighted average.

Example:

```text
ImplementationReady(slice) =
    StructuralValidity(slice)
    AND HasAuthoritativeOutcome(slice)
    AND AllMaterialInteractionsDefined(slice)
    AND AllInvariantOwnersKnown(slice)
    AND AllBoundaryContractsAtLeastDraft(slice)
    AND NoBlockingGaps(slice)
    AND EvidencePlanApproved(slice)
```

The UI can summarize readiness, but it must allow the user to inspect each predicate.

## Baselines

A baseline records:

- immutable revision,
- scope filter,
- purpose profile,
- validation results,
- unresolved accepted gaps,
- required approvals,
- approval records,
- projection versions,
- timestamp and author.

A baseline can be candidate, approved, rejected, superseded, or withdrawn.

## Change after baseline

When a model changes after baseline:

1. The new change set identifies impacted elements.
2. Traceability computes potentially affected claims, views, contracts, generated artifacts, and evidence.
3. Relevant evidence becomes stale until re-evaluated.
4. Approvals remain attached to the old baseline.
5. The team can create a new candidate baseline.

Project Builder should prefer conservative impact warnings over false claims that a change is harmless.
