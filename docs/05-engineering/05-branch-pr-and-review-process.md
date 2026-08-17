# Branch, PR, and Review Process

## Branching model

Use trunk-based development with short-lived branches and draft pull requests.

Draft PRs are encouraged for visibility, CI, early architectural review, and stacked work. Draft does not mean "ready for formal review."

## Branch names

```text
feature/<model-id>-short-description
fix/<model-id>-short-description
spike/<decision-id>-short-description
docs/<short-description>
release/<version>
hotfix/<version>-short-description
```

Model or issue identifiers are included when available.

## Stacked work

Stacked branches are allowed when a feature decomposes into independently reviewable vertical slices.

Example:

```text
main
└── feature/create-project
    └── feature/add-actor
        └── feature/add-scenario
```

Rules:

- each branch is buildable and testable against its parent,
- each PR states its parent and stack order,
- each slice has independent model scope and evidence,
- later branches do not conceal required fixes in earlier branches,
- generated lock or schema changes are isolated where practical,
- formal review starts from the lowest ready PR.

## Squash merge handling

When the organization uses squash merges:

1. Record the squash commit from the merged PR.
2. Rebase the next branch onto updated `main`.
3. Drop commits already represented by the squash using `rebase --onto` or recreate through the branch's unique range.
4. Verify semantic diff, not only Git conflict resolution.
5. Retarget the PR to `main`.
6. rerun evidence.
7. Update stack metadata.

Recommended helper script can compute:

```text
old parent tip
new squash commit
merge base
unique commits and patch-id matches
```

The script must stop on ambiguity and never force-push another contributor's branch without explicit action.

## Draft PR lifecycle

### Open early
Open after the first coherent commit or model/work plan exists.

Draft description includes:

- goal,
- model scope and baseline,
- planned slices,
- open decisions,
- current evidence,
- dependencies,
- not-ready reasons.

### Iterate
Push bounded commits. CI provides continuous evidence. Use comments for decisions that should become ADRs or model changes.

### Ready for review
Mark ready only when:

- PR scope is complete,
- model is current,
- tests pass,
- self-review is complete,
- generated output is reviewed,
- no known blocker remains,
- stack dependencies are stable,
- review guide is updated.

## PR template

```markdown
## Goal
What observable behavior changes and for whom?

## Model
Project:
Baseline or revision:
Elements:
Model diff:

## Scope
Included:
Excluded:

## Behavior
Happy path:
Material alternate and failure paths:

## Architecture
Fixed decisions:
New or changed boundaries:
ADR:

## Evidence
Commands:
Results:
Artifacts:
Claim links:

## Risk
Migration:
Security:
Accessibility:
Performance:
Operations:

## Stack
Parent PR:
Next PR:

## Reviewer path
Suggested file and model review order.
```

## Commit policy

Commits should be coherent and reviewable:

- model or specification,
- domain behavior,
- application or infrastructure,
- presentation,
- evidence,
- cleanup.

Do not require artificial one-commit purity. Before merge, squash policy can produce one mainline commit while PR history preserves iteration.

Commit messages explain behavior:

```text
model: define product-not-found recovery path
feat: add semantic result for unknown product
test: prove duplicate request does not add second line
```

## Review roles

### Domain reviewer
Checks meaning, rules, and paths.

### Experience reviewer
Checks interface state, feedback, and accessibility.

### Architecture reviewer
Checks boundaries, dependency direction, qualities, and operations.

### Validation reviewer
Checks evidence sufficiency.

One person can fill several roles, but the PR shows which authority was exercised.

## Review order

1. Model diff and purpose.
2. behavioral tests.
3. domain types and transitions.
4. application orchestration.
5. adapters and persistence.
6. presentation.
7. security, accessibility, performance, operations.
8. style and cleanup.

## Review comments

Classify comments:

- Blocker.
- Correctness.
- Design.
- Security.
- Evidence.
- Maintainability.
- Suggestion.
- Question.
- Nit.

A nit should not hold a PR unless repository policy makes it a machine-enforced rule.

## Conflict resolution

After resolving code conflicts:

- run model diff,
- rerun affected tests,
- recheck generated outputs,
- inspect database migration order,
- verify no sibling stack behavior was lost,
- update base revision references.

A clean Git merge does not prove a clean semantic merge.

## Merge criteria

- approved by required reviewers,
- required CI passes,
- branch current with target according to policy,
- model baseline or revision linked,
- no unresolved blocker conversations,
- migration and deployment notes complete,
- stack descendants identified.

## Post-merge

- verify main pipeline,
- update or rebase stack descendants,
- close or update linked gaps,
- ingest evidence if automated,
- deploy preview or development,
- exercise dogfood scenario.

## Emergency exception

An emergency merge can bypass ordinary gates only with:

- incident reference,
- authorized approver,
- explicit skipped evidence,
- rollback or containment plan,
- follow-up deadline,
- post-merge full validation,
- model and process correction.

Emergency cannot mean undocumented.
