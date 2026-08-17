# Agent Operating Model

## Purpose

Agents can accelerate Project Builder delivery, but they operate inside the same Definition-Validated Delivery system as human contributors. An agent receives a bounded definition, proposes or implements a change, produces evidence, and reports uncertainty. It does not become an alternate source of truth.

The repository must remain operable without any agent.

## Core contract

An agent may:

- inspect repository and model context,
- implement a bounded slice,
- add or refine tests,
- draft documentation,
- generate an uncommitted model change set,
- identify contradictions and gaps,
- compare architectural options,
- run approved local validation,
- prepare a review packet.

An agent may not:

- silently broaden scope,
- invent missing business truth,
- mark its own assertion as evidence,
- commit secrets,
- weaken tests or controls to pass,
- bypass architecture rules,
- perform destructive external actions without explicit authority,
- change public compatibility without recording the decision,
- hide failed validation,
- treat generated output as authoritative when it conflicts with the model.

## Definition-first dispatch

Every task begins with a task definition containing:

1. **Outcome:** the observable behavior to deliver.
2. **Scope:** included and excluded work.
3. **Source model:** project, revision, elements, and findings.
4. **Invariants:** properties that must remain true.
5. **Interfaces:** user/API/device behavior.
6. **Boundaries:** systems and contracts involved.
7. **Evidence:** commands and expected proof.
8. **Constraints:** architecture, security, accessibility, focus/window behavior, files not to touch.
9. **Stop conditions:** ambiguity or failure states that require a finding rather than invention.
10. **Handoff:** required summary.

The task should be executable without loading the entire documentation suite.

## Progressive disclosure

Agents receive only the context needed for the current phase.

### Level 0: repository invariant card

Read every session:

- root `AGENTS.md`,
- current task or goal prompt,
- repository README command table.

### Level 1: work-area contract

Read the one or two documents for the affected area:

- model,
- experience,
- architecture,
- engineering,
- delivery,
- guide or example.

### Level 2: feature definition

Read:

- selected model slice or fixture,
- issue/story,
- relevant ADRs,
- neighboring code.

### Level 3: implementation details

Inspect:

- concrete types and tests,
- generated outputs,
- migrations,
- provider contracts.

### Level 4: exceptional context

Load broader documents only when a detected conflict or decision requires them.

An agent should not preload the whole `docs` folder. The progressive disclosure map identifies the shortest route.

## Session workflow

### 1. Orient

- confirm branch and worktree,
- read instructions,
- inspect current status,
- identify affected module and model elements,
- run the smallest baseline check.

### 2. Restate the truth

In the work log or PR draft, state:

- actor outcome,
- starting state,
- trigger,
- semantic results,
- invariant,
- expected observation,
- evidence.

This is not a hidden chain of thought. It is a concise engineering contract.

### 3. Inspect before editing

Search for:

- existing feature folders,
- result types,
- conventions,
- extension registries,
- test builders,
- generated code,
- architectural boundaries.

Do not create a second abstraction because the first was not immediately visible.

### 4. Implement vertically

Prefer one complete behavior through Domain, Application, Infrastructure, Presentation, and evidence as needed. Empty layers are acceptable. Horizontal framework construction needs an explicit enabling outcome.

### 5. Validate continuously

Run focused tests after each coherent change. Run repository verification before handoff. Preserve logs for failure diagnosis.

### 6. Update definition

When implementation reveals a changed fact, rule, result, path, interface, boundary, or decision:

- update the dogfood model or source fixture,
- update relevant docs,
- add a finding if authority is missing.

Do not reinterpret the model silently.

### 7. Review diff and generated artifacts

- inspect all changed files,
- remove accidental formatting churn,
- verify deterministic generated output,
- confirm no secrets or local paths,
- verify migration and contract changes,
- verify tests fail for the intended defect when practical.

### 8. Handoff

Report:

- behavior delivered,
- model changes,
- evidence and exact commands,
- decisions,
- assumptions,
- risks,
- unresolved findings,
- exact next entry point.

## Human and agent parity

Every agent-facing command must correspond to a human-usable mechanism:

- command palette action,
- CLI or script,
- API contract,
- structured change set,
- documented validation command.

An agent-only hidden endpoint is prohibited for essential behavior.

Inside Project Builder, future agent proposals use the same command/change-set pipeline as human edits. A proposal can be inspected, edited, partially applied, and rejected.

## Repository safety

### File scope

A session should name expected paths. The agent may inspect broadly but edits narrowly. Changes outside scope are reported before being included unless required to keep the build coherent.

### Generated files

- edit declarations, not generated outputs,
- generator output is committed only if repository policy requires it,
- no hardcoded analyzer DLL path,
- no machine-specific copy step,
- generator projects are self-contained and packaged through normal project references or NuGet.

### Database

- never rewrite applied production migration history,
- add forward migrations,
- use isolated test databases,
- do not run destructive commands against unconfirmed targets,
- include migration evidence.

### External systems

- use test/sandbox accounts,
- do not send private model content to unapproved providers,
- no email, deployment, publication, issue mutation, or release action unless the task explicitly authorizes it,
- prefer draft artifacts for review.

### UI automation

- do not steal focus,
- do not move windows between monitors,
- do not use global mouse or keyboard injection,
- use headless or isolated browser execution by default,
- when headed execution is required, keep it within the assigned window and monitor,
- never interfere with the user's active session.

## Uncertainty protocol

When the task lacks business authority:

1. preserve the ambiguous behavior as a finding,
2. implement only a safe, reversible seam if required,
3. use explicit result such as Unknown or Unsupported,
4. add examples that show the unresolved alternatives,
5. do not choose policy based on convenience.

When a technical choice is reversible and does not alter product truth, make the narrowest conventional choice and record it in the handoff.

## Validation tiers

### Focused

- affected unit/property/component tests,
- affected project build,
- schema or generator test.

### Slice

- application/infrastructure contract,
- migration,
- browser/API scenario,
- architecture rules.

### Repository

- restore,
- build,
- all tests,
- formatting/analyzers,
- schema,
- dependency/security checks,
- deterministic generation.

### Release

- supported migrations,
- compatibility,
- performance,
- security,
- accessibility,
- backup/restore,
- deployment smoke,
- dogfood baseline.

A session prompt states the required tier.

## Parallel agents

Parallel work is safe when:

- modules and files do not overlap,
- contracts are agreed,
- one branch owns shared registry changes,
- integration order is explicit,
- each worker has a distinct proof target.

Avoid concurrent edits to:

- element/relation unions,
- serialization settings,
- change-set operation types,
- central package versions,
- root Studio shell,
- migrations,
- source-generator wiring,
- public contracts.

Use stacked branches when work depends sequentially on unmerged behavior. After a squash merge, restack from the new main using a documented range-diff or patch-replay workflow and verify the semantic diff.

## Agent evaluation

Measure agent contribution by:

- accepted behavior,
- defects escaped,
- evidence quality,
- unnecessary churn,
- architecture violations,
- model drift,
- review effort,
- token or cost efficiency,
- clarity of handoff,
- ability to operate from bounded context.

Do not optimize only for code volume or task closure.

## Internal product agent design

When agent assistance enters Project Builder:

```text
User request
  -> classify allowed task and project policy
  -> retrieve bounded model context
  -> provider-neutral structured request
  -> structured proposal with provenance
  -> deterministic schema/model validation
  -> user review and selective application
  -> ordinary change-set commit
  -> audit and evaluation
```

Agent proposals are separate from evidence. A suggested invariant remains a proposal until a human accepts it and suitable proof is produced.
