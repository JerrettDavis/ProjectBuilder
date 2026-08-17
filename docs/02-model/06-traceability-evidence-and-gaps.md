# Traceability, Evidence, and Gaps

## Purpose

Traceability is the ability to explain why an element exists, what it affects, and what proves it. Project Builder treats traceability as a graph derived from stable model identity, not as a spreadsheet assembled at the end of a project.

## Traceability chain

A typical chain is:

```text
Outcome
  → Capability
  → Episode
  → Scenario
  → Interaction
  → Intent
  → State Transition
  → Rule / Invariant
  → Interface
  → Vertical Slice
  → Implementation Reference
  → Evidence
```

Real chains can branch. One invariant can constrain many scenarios. One contract can serve many interactions. One test can support several claims, provided its scope is explicit.

## Claim model

A Claim states:

- proposition,
- scope,
- type,
- authority,
- status,
- source,
- applicable revisions,
- required evidence,
- linked definitions,
- disputes or waivers.

Claim types:

- outcome,
- behavioral,
- state,
- invariant,
- contract,
- quality,
- security,
- privacy,
- accessibility,
- operational,
- compatibility,
- architectural.

Many elements contain implicit claims. A first-class Claim is created when the proposition needs independent review, evidence, or lifecycle.

## Evidence model

Evidence records:

- type,
- source URI or attachment,
- producer,
- timestamp,
- model revision,
- implementation revision,
- environment,
- input or dataset,
- result,
- status,
- freshness rule,
- covered claims,
- limitations,
- reviewer.

Evidence types:

### Human authority
Interview approval, workshop review, policy owner confirmation, legal interpretation.

### Example proof
A concrete scenario or test showing expected behavior for selected facts.

### Property proof
A generated or exhaustive test over a defined input domain.

### Contract proof
Consumer/provider compatibility or adapter behavior.

### Integration proof
Behavior across assembled components or real infrastructure.

### End-to-end proof
Observable behavior through the initiating interface and relevant boundaries.

### Static proof
Compiler, analyzer, schema, type, dependency, or formal check.

### Experiment
Performance, reliability, usability, accessibility, or security study.

### Operational observation
Production metric, trace, log, reconciliation, incident rehearsal, or support evidence.

## Sufficiency

Evidence is sufficient only relative to a claim.

A UI snapshot can prove that text rendered. It cannot prove a transaction invariant.

A unit test can prove a pure rule example. It cannot prove the production adapter uses the rule.

An end-to-end example can prove one path. It cannot search a broad numeric input space as efficiently as a property test.

Project Builder should explain these limitations and encourage layered evidence.

## Freshness

Evidence becomes potentially stale when:

- a covered definition changes,
- implementation reference changes,
- contract version changes,
- environment or provider changes,
- generator version changes,
- test input authority changes,
- an assumption is invalidated.

Staleness propagation is conservative. A reviewer can re-evaluate and mark evidence current without rerunning it only when the change cannot affect the claim, with rationale.

## Implementation references

The model can reference:

- repository,
- commit,
- branch or PR,
- project and namespace,
- type or member,
- configuration key,
- database migration,
- infrastructure resource,
- deployment version,
- test case,
- CI run,
- issue.

References are external links with optional integration metadata. The canonical model does not import source code identity as domain identity.

## Gap model

A Gap contains:

- category,
- description,
- affected scope,
- severity,
- discovery source,
- owner,
- disposition,
- consequence,
- target milestone,
- related assumptions or decisions,
- resolution change set.

Gap categories:

- missing definition,
- ambiguity,
- contradiction,
- unsupported claim,
- incomplete path,
- missing authority,
- missing contract,
- missing quality requirement,
- missing evidence,
- stale evidence,
- model limitation,
- implementation divergence,
- operational divergence.

## Gap dispositions

- Open.
- Investigating.
- Assumed.
- Deferred.
- Accepted risk.
- Not applicable.
- Resolved.
- Superseded.
- Reopened.

Each non-open disposition requires rationale and, where relevant, authority.

## Impact analysis

A model change can affect:

- descendants in containment,
- semantic dependents,
- derived views,
- generated artifacts,
- claims,
- evidence,
- decisions,
- baselines,
- implementation work packages.

Impact edges have propagation policies. For example:

- Rename: display projections update, behavioral evidence usually remains current.
- Invariant change: all implementing slices and evidence become potentially stale.
- Canvas move: no semantic impact.
- Contract version change: adapter evidence and dependent scenarios become potentially stale.
- Actor description change: interface and accessibility claims may require review.
- Boundary trust classification change: security threat model and authorization claims become stale.

## Traceability views

### Outcome trace
Shows how an outcome is realized and evidenced.

### Claim-evidence matrix
Rows are claims, columns are evidence types and statuses.

### Change impact
Shows directly and transitively affected elements with reason.

### Orphan view
Shows elements with no meaningful inbound or outbound trace.

### Evidence debt
Shows material claims with missing, failing, stale, disputed, or waived evidence.

### Implementation divergence
Shows model definitions without implementation references and implementation references not connected to definitions.

## Validator findings

- material claim has no authority,
- evidence has no covered claim,
- evidence predates a breaking definition change,
- implementation reference points to missing revision,
- test name is linked but result source is absent,
- gap marked resolved without a resolving change set,
- waiver has expired,
- generated artifact revision differs from baseline,
- orphaned element contributes to no outcome,
- scenario outcome has no evidence requirement under release profile,
- one evidence item claims incompatible scopes.

## Review packet

A baseline review packet should contain:

1. Purpose and scope.
2. Changed outcomes and scenarios.
3. New or changed invariants.
4. Boundary and contract changes.
5. unresolved gaps and accepted risks.
6. evidence status.
7. impacted generated artifacts.
8. required authorities.
9. comparison to prior baseline.
10. explicit approval statements.
