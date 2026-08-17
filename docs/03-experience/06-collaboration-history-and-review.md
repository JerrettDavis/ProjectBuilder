# Collaboration, History, and Review

## Collaboration principles

Project Builder collaboration must preserve semantic integrity, authorship, and reviewability. A live cursor is useful. A trustworthy change history is essential.

## Presence

Presence shows:

- active users,
- current project and lens,
- optionally selected element,
- editing intent or draft lock where used,
- connection status.

Users can hide detailed presence according to policy.

Presence is ephemeral and not part of model history.

## Comments and discussions

Comments can attach to:

- project,
- model element,
- relation,
- field,
- canvas coordinate,
- finding,
- evidence,
- baseline,
- change set.

A comment records revision context so later readers can see the content under discussion.

Discussion states:

- open,
- resolved,
- reopened,
- outdated,
- converted to gap,
- converted to decision.

Resolving a comment does not resolve an associated model gap automatically.

## Draft collaboration

MVP collaboration uses optimistic concurrency:

1. Each editor reads a project revision and element versions.
2. The client stages typed operations.
3. The server validates authorization and expected versions.
4. Non-conflicting operations commit atomically.
5. Conflicts return current and proposed semantic values.
6. The user rebases, chooses, or creates a distinct element.

Layout-only operations can merge more freely because they do not change semantics.

## Conflict types

### Same-field conflict
Two users change the same semantic field.

### Structural conflict
One user moves or deprecates an element another edits.

### Relationship conflict
Cardinality or endpoint changes make both operations incompatible.

### Invariant conflict
Individually valid operations become invalid together.

### Baseline conflict
A user edits content under active review.

### Extension conflict
An editor lacks the extension version needed to understand the change.

No semantic conflict is silently last-write-wins.

## Future collaboration evolution

Possible stages:

1. Presence and notifications.
2. Optimistic command conflicts.
3. Node or scope editing claims.
4. Operation transformation for known command families.
5. CRDT for selected free-text fields.
6. General graph collaboration only if real use demonstrates need.

The team should not adopt a universal CRDT merely because the product resembles a whiteboard. Model operations have domain invariants that generic merge algorithms cannot settle.

## Change sets

Every committed semantic edit is a change set:

- identifier,
- project and workspace,
- base and result revision,
- author or agent identity,
- reason,
- operations,
- validation results,
- affected elements,
- correlation and causation,
- timestamp,
- client and application version.

A change set can contain model, relation, evidence, and shared-view operations. Personal view operations can use a separate lightweight history.

## Semantic diff

A diff explains meaning:

```text
Scenario "Product not found"
  Added path classification: Exceptional
  Added terminal result: NotRecognized
  Added actor observation: "Item not found"
  Added recovery action: Manual lookup
  Added evidence requirement: End-to-end example
```

It does not make reviewers interpret raw JSON unless they choose to.

## Reviews

A review request selects:

- project revision or baseline,
- scope,
- purpose profile,
- reviewers and required authority categories,
- due date,
- unresolved accepted gaps,
- generated review packet.

Review actions:

- comment,
- request change,
- approve claim category,
- reject,
- abstain,
- waive under authority,
- supersede prior approval.

## Approval semantics

Approval is:

- by a named identity,
- for a declared scope,
- at a specific revision,
- under a role or authority,
- optionally time-bounded,
- invalidated or made stale by relevant changes.

One broad "Approved" badge cannot stand for domain, architecture, security, and evidence authority unless policy explicitly permits it.

## Baselines

A baseline is immutable and named.

Examples:

- `Discovery Review 1`.
- `POS Item Scan Implementation Baseline`.
- `Beta 0.3 Release Model`.

Baseline contents:

- revision,
- scope,
- purpose profile,
- findings snapshot,
- accepted gaps,
- approval records,
- projection versions,
- content hash.

## Branching

Project Builder can later support model branches for substantial alternatives. MVP can use:

- duplicate project with provenance,
- named scenario variants,
- decision options,
- candidate change sets,
- historical restore through new changes.

Branching is introduced only with clear merge and identity semantics.

## Compare

Comparison can be:

- revision to revision,
- baseline to baseline,
- project to imported project,
- scenario variant to variant,
- model to implementation evidence,
- model to runtime observation.

Filters:

- semantic only,
- layout only,
- status,
- affected actors,
- paths,
- boundaries,
- evidence impact.

## Notifications

Users can subscribe to:

- direct mentions,
- comments on owned elements,
- review requests,
- changes to approved claims,
- stale evidence,
- conflicts,
- baseline status,
- projection completion,
- import or migration results.

Notification delivery is a presentation concern and can include in-app, email, or integration adapters.

## Audit

Audit includes:

- authentication and authorization events,
- role and policy changes,
- project creation, export, import, deletion, and restore,
- change set commits,
- baseline and approval actions,
- evidence attachment,
- agent calls and accepted proposals,
- extension installation or execution,
- administrative access.

Audit events are separate from the domain change history but can correlate with it.

## Review acceptance criteria

- A reviewer can understand a change without opening raw storage records.
- Approval is revision- and scope-specific.
- Stale approvals are visible.
- Comments retain original context.
- Conflicts cannot overwrite semantic content.
- Agent proposals remain attributable to requester and agent.
- A baseline can be exported and independently validated.
