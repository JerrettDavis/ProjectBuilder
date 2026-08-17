# Studio Shell

## Purpose

The Studio is a stable spatial environment for modeling. Users should build location memory: model hierarchy on the left, work in the center, properties and guidance on the right, findings and evidence below. The shell changes tools by lens without rearranging the entire application.

## Default layout

```text
┌──────────────────────────────────────────────────────────────────────────────┐
│ Workspace / Project     Lens switcher         Search   Review   Share   User │
├───────────────┬──────────────────────────────────────────────┬───────────────┤
│ Model Explorer│ Toolbar / breadcrumb / scenario / revision   │ Inspector     │
│               ├──────────────────────────────────────────────┤ or Guide Rail │
│ Narrative     │                                              │               │
│ Context       │              Active Lens                     │ Typed fields  │
│ Actors        │                                              │ Relations     │
│ State         │        Canvas or structured editor           │ Sources       │
│ Interfaces    │                                              │ Findings      │
│ Systems       │                                              │               │
│ Evidence      │                                              │               │
├───────────────┴──────────────────────────────────────────────┴───────────────┤
│ Problems | Evidence | History | Comments | Simulation | Output              │
└──────────────────────────────────────────────────────────────────────────────┘
```

## Regions

### Global header

Contains:

- workspace and project switcher,
- project status,
- lens switcher,
- global search,
- command palette,
- baseline or revision indicator,
- review and share controls,
- presence avatars,
- user menu.

The header does not contain lens-specific drawing tools.

### Explorer

Contains semantic navigation. It can be hidden with a stable shortcut. It supports keyboard tree semantics and does not use drag as the only reorder mechanism.

### Lens toolbar

Contains tools relevant to the current lens:

- select,
- connect,
- add typed element,
- frame or group,
- path tool,
- comment,
- fit or zoom,
- layout,
- simulation controls.

Tools execute commands through the same application model used by forms and keyboard actions.

### Work surface

The central region can render:

- canvas,
- table,
- state transition matrix,
- form,
- text projection,
- diff,
- traceability graph,
- interface preview,
- review packet.

The work surface exposes a common selection and command contract.

### Inspector

The Inspector is generated from element type metadata but uses deliberate, hand-designed sections for core types.

Sections:

1. Identity and status.
2. Definition.
3. Context and containment.
4. Participants and authority.
5. State and behavior.
6. Relations.
7. Sources and evidence.
8. Findings.
9. Advanced and extension data.

Edits remain drafts until committed through the application's change-set model. Small edits may auto-stage, but the user can inspect the staged changes.

### Guide rail

The Guide Rail can replace or tab alongside the Inspector.

It shows:

- current modeling objective,
- current question,
- why the question appeared,
- relevant examples,
- answer controls,
- Link Existing,
- Unknown,
- Assumed,
- Not Applicable,
- Defer,
- previous and next,
- unresolved findings in this stage.

The guide never hides authored data behind a summary the user cannot inspect.

### Bottom workbench

Panels are independent views:

- Problems.
- Evidence.
- History.
- Comments.
- Simulation.
- Generated Output.
- Diagnostics for development builds.

Selecting a panel item focuses the related element without changing semantic state.

## Draft and commit behavior

Project Builder combines fluid editing with accountable history.

### Local draft
Field edits and visual operations are staged in a client draft. Validation runs immediately.

### Commit boundary
A user commits one or more related operations as a change set with an inferred or edited reason.

Default commit triggers:

- explicit Save or Commit,
- finishing a guide step,
- leaving a changed element,
- completing a canvas operation group,
- idle checkpoint for crash recovery, not semantic commit.

The product must clearly distinguish locally recovered draft state from server-committed history.

## Undo and redo

Undo operates first on uncommitted draft operations. After commit, Undo proposes an inverse change set and explains conflicts or downstream impact.

Redo is available while the local operation history remains valid.

Do not pretend history deletion occurred. Committed change sets remain auditable.

## Revision awareness

The shell always indicates whether the user is viewing:

- latest editable revision,
- historical revision,
- named baseline,
- comparison,
- review candidate.

Historical views are read-only until the user creates a branch or restores through a new change set.

## Command model

Every operation has:

- stable command identifier,
- label and description,
- required selection types,
- authorization policy,
- keyboard shortcut where appropriate,
- undo semantics,
- telemetry name,
- help link.

Examples:

```text
projectbuilder.element.create.actor
projectbuilder.scenario.add-failure-path
projectbuilder.view.align.left
projectbuilder.selection.open-in.state-lens
projectbuilder.changes.commit
```

This command model supports toolbar, context menu, keyboard, command palette, tutorials, macros, and future agent tools without separate implementation paths.

## Context menus

Context menus are convenience projections of commands. They are not the only place an action exists.

Menus are grouped:

- Open.
- Add related.
- Connect.
- Status.
- Review.
- View.
- Move or reorder.
- Copy or export.
- Delete, deprecate, or detach.

Destructive actions identify semantic consequences.

## Notifications

Notifications are classified:

- transient confirmation,
- actionable warning,
- collaboration change,
- background projection status,
- security or governance event.

A toast does not carry information that disappears before a user can act. Important messages also appear in a durable activity or Problems location.

## Help

Contextual help can open:

- concept definition,
- why this field matters,
- worked POS example,
- model rule explanation,
- keyboard shortcut,
- related guide lesson.

Help must not move focus from an active editor unless the user explicitly opens it.

## Personalization

Personal preferences include:

- panel sizes and visibility,
- preferred explorer tree,
- keyboard bindings,
- reduced motion,
- canvas grid and snapping,
- default lens,
- theme,
- density,
- guide verbosity.

Personal preferences never alter shared model semantics.
