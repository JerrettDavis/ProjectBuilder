# Canvas, Lenses, and Drilldown

## Principle

The canvas is a visual editor over typed model projections. It is not the database and not an unstructured drawing surface.

A lens decides:

- which semantic elements are relevant,
- how they are represented,
- which relationships become edges,
- which commands are available,
- which findings are overlaid,
- which layout is persisted.

## Lens contract

```csharp
public interface IModelLens
{
    LensType Type { get; }
    ValueTask<LensProjection> ProjectAsync(
        CanonicalModel model,
        LensRequest request,
        CancellationToken cancellationToken);

    ImmutableArray<LensCommandDescriptor> GetCommands(
        LensContext context);
}
```

A `LensProjection` contains:

- nodes,
- ports,
- edges,
- groups,
- labels,
- semantic references,
- derived annotations,
- selection mappings,
- inspector schema,
- accessibility tree,
- layout constraints.

## Core lenses

### Story Map

Purpose:
- Preserve outcomes, capabilities, episodes, scenarios, and actor value.

Visual grammar:
- outcomes as top-level goal cards,
- capabilities as lanes or groups,
- episodes as narrative bands,
- scenarios as paths or cards,
- actors as participation markers,
- findings as status badges.

Key commands:
- add outcome,
- add actor,
- add episode,
- split into scenarios,
- identify alternate path,
- open flow.

### Scenario Flow

Purpose:
- Show ordered scenes, interactions, decisions, branches, joins, outcomes, and boundaries.

Visual grammar:
- scene frames,
- interaction nodes,
- actor swimlanes when useful,
- condition diamonds only when the branch is semantically a decision,
- path colors supplemented with labels and patterns,
- boundary bands,
- terminal result nodes.

Key commands:
- add interaction,
- add branch,
- add failure path,
- connect recovery,
- classify path,
- step through scenario,
- open state.

### State and Rule

Purpose:
- Show facts, states, transitions, commands, events, rules, and invariants.

Representations:
- state machine,
- transition table,
- rule decision table,
- fact dependency graph,
- invariant panel.

The user can switch representation without recreating definitions.

### Interface

Purpose:
- Design an interaction surface and map scenario state to visible or exposed state.

Graphical interface:
- frames and controls.

CLI:
- command tree, arguments, terminal states.

API:
- endpoints, operations, messages, results.

MCP:
- tools, resources, prompts, authorization, and side-effect labels.

Device:
- signals, controls, statuses, and timing.

Human procedure:
- roles, steps, forms, handoffs, and acknowledgment.

### System Context

Purpose:
- Show actors, systems, external systems, devices, boundaries, interfaces, contracts, and data movement.

The lens can render C4-like levels without making C4 the canonical model.

### Data and Contract

Purpose:
- Show concepts, messages, stores, ownership, transformations, classifications, and retention.

### Decision and Risk

Purpose:
- Show open decisions, selected options, assumptions, risks, evidence, and impacted elements.

### Traceability and Evidence

Purpose:
- Show claims, implementation references, evidence status, staleness, gaps, and baselines.

### Implementation Slice

Purpose:
- Project one interaction into Presentation, Application, Domain, Infrastructure, and Evidence lanes.

## Canvas interaction model

### Navigation
- Space plus pointer or middle pointer pans.
- Wheel or pinch zooms around pointer.
- keyboard commands pan and zoom.
- Fit Selection and Fit Scope commands.
- Breadcrumb and mini-map for large views.
- Back and Forward location history.

### Selection
- click selects.
- Shift modifies selection.
- marquee has keyboard-accessible equivalent through explorer filtering and Select All in Scope.
- selection is reflected in explorer, inspector, and panels.
- relation selection exposes qualifiers and path semantics.

### Creation
Elements are created through:

- toolbar,
- command palette,
- context menu,
- keyboard shortcut,
- guide,
- drag from typed palette.

Dropping creates a command preview. It does not mutate the model until placement and required minimum fields are valid.

### Connection
Connections use typed ports. The editor previews allowed relationship kinds based on source and target.

Invalid relationships are blocked with explanation. Ambiguous valid relationships prompt the user to choose.

### Movement
Movement changes layout only. Semantic move or reparent is a separate command with explicit impact.

### Grouping
A visual frame can be:

- layout-only group,
- projection of semantic containment,
- boundary,
- scene,
- context,
- actor lane.

The UI distinguishes them.

### Deletion
Commands are:

- Remove from view.
- Detach relation.
- Move to another parent.
- Deprecate element.
- Delete draft element.

A referenced committed element is rarely hard-deleted. The command explains affected relations and baselines.

## Drilldown

Open behavior:

- Double-click or Enter opens the selected abstraction.
- A menu offers relevant lens choices.
- Breadcrumbs preserve parent context.
- The parent remains visible as a pinned context card when useful.
- Child view selection can be shared or personal.

Example:

```text
Story card: Resolve item and price
  → Scenario Flow: capture, classify, lookup, add, observe
  → State Lens: transaction and priced-product transitions
  → System Lens: scanner, POS, price book, store cache
  → Slice Lens: presentation, application, domain, infrastructure, evidence
```

## Roll-up

A parent abstraction displays derived roll-up information:

- child count,
- unresolved blocker count,
- path coverage,
- evidence status,
- impacted boundaries,
- last change.

Roll-ups are summaries. The parent definition still needs a meaningful description and outcome.

## Scenario playback

Playback is a read-only projection:

1. Load selected scenario and example facts.
2. Highlight current actors, interface state, and relevant facts.
3. Advance through interactions.
4. show rule and branch decisions.
5. Show state changes and events.
6. Show external effects and waiting states.
7. End in semantic outcome.
8. Allow switching to alternate path.

Playback does not execute production code in MVP. It explains the authored model and can later consume implementation traces.

## Layout engine

Initial layout supports:

- manual layout,
- align and distribute,
- horizontal or vertical flow,
- hierarchical layout,
- swimlanes,
- orthogonal edge routing,
- grid and snap,
- auto-layout preview before commit.

Auto-layout affects view state only.

## Rendering strategy

MVP uses accessible SVG and HTML overlays:

- semantic model and geometry calculated in C#,
- thin JavaScript interop for pointer capture, resize observation, clipboard, text measurement, and browser file APIs,
- viewport culling,
- batched updates,
- simplified rendering at low zoom,
- worker or server assistance for expensive layout when needed.

A renderer abstraction allows later Canvas or WebGL implementation without changing lens semantics.

## Large-model behavior

- show only the selected scope by default,
- collapse groups,
- lazy-load non-visible details,
- render summary nodes at high abstraction,
- cap expensive labels and edge decoration by zoom,
- search and filter before rendering,
- warn when a view becomes cognitively unreadable,
- preserve full model access through structured editors.

## Accessibility model

Every canvas has a synchronized structured outline:

```text
Scenario: Recognized item for cash
  Scene 1: Capture item
    Interaction: Clerk scans item
      Initiator: Clerk
      Receiver: POS
      Result: Scan captured
  Scene 2: Resolve price
    Branch: Product found
    Branch: Product not found
```

Keyboard users can:

- move among elements,
- select,
- create related elements,
- connect through a dialog,
- reorder,
- change parent,
- inspect relations,
- open drilldown,
- announce changes.

## Canvas acceptance criteria

- A visual move cannot change semantic containment.
- Every canvas command is available through a non-drag mechanism.
- Selection remains synchronized.
- Invalid relationships cannot be committed.
- View layout can be personal or shared.
- Opening a historical revision renders the historical semantic and view state.
- A 1,000-node representative graph remains operable within measured latency budgets.
