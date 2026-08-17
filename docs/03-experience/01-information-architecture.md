# Information Architecture

## Experience model

Project Builder has two coordinated modes:

1. **Guide mode**, which asks context-aware questions and explains the next useful modeling action.
2. **Studio mode**, which exposes the full project through structured editors and visual lenses.

These are not separate products. The guide is a rail that can open inside the studio, focus the current scope, and show unresolved work. A user can leave the guide, edit directly, and return without losing place.

## Primary navigation

```text
Workspace
├── Home
├── Projects
│   └── Project
│       ├── Overview
│       ├── Model
│       ├── Interfaces
│       ├── Systems
│       ├── Evidence
│       ├── Reviews
│       ├── History
│       └── Settings
├── Templates
├── Integrations
├── Members
├── Audit
└── Workspace settings
```

Inside a project, the lens switcher is more important than page navigation. The user works on the same selected model scope through different representations.

## Project navigation concepts

### Overview
Purpose, outcomes, recent activity, model findings, readiness by profile, and recommended next actions.

### Model
Narrative, state, rule, and path lenses.

### Interfaces
Graphical, CLI, API, message, MCP, device, document, and human-procedure designs.

### Systems
Contexts, boundaries, components, contracts, data flow, decisions, qualities, and risks.

### Evidence
Claims, evidence requirements, artifacts, status, freshness, and traceability.

### Reviews
Candidate baselines, comments, approvals, waivers, and review packets.

### History
Change sets, revisions, semantic diffs, named baselines, import/export, and impact analysis.

### Settings
Project roles, profiles, templates, integrations, retention, and extension compatibility.

## Model explorer

The left-side Model Explorer supports several trees over the same graph:

- Narrative.
- Contexts.
- Participants.
- State and Rules.
- Interfaces.
- Systems and Boundaries.
- Claims and Evidence.
- Gaps.
- Saved Views.

The user selects the preferred tree without changing the underlying model.

Explorer features:

- filter by status, tag, type, owner, and finding severity,
- search,
- inbound and outbound reference badges,
- drag for view organization only where semantics permit,
- keyboard reordering,
- context menu,
- inline rename,
- stable deep links,
- multi-select,
- open in another lens.

## Lens navigation

A lens is selected through a persistent top bar:

```text
Story | Flow | State | Interface | System | Data | Decision | Evidence | Slice
```

Each lens receives:

- current root scope,
- selected elements,
- active scenario or baseline,
- filters,
- personal or shared view,
- revision.

Switching a lens should preserve selection where a meaningful representation exists. Otherwise, it opens the closest related scope and explains the mapping.

## Breadcrumbs

Breadcrumbs show semantic containment, not browser route alone:

```text
Point of Sale
/ Store Sales
/ Sell Merchandise
/ Complete Staffed Sale
/ Recognized Item for Cash
/ Resolve Item and Store Price
/ Add Scanned Product
```

Each segment can:

- open,
- reveal siblings,
- open in a different lens,
- copy link,
- show status and findings.

## Global work surfaces

### Command palette
Search commands, elements, lenses, recent locations, templates, and help. All important operations have stable command identifiers and discoverable shortcuts.

### Problems panel
Shows model findings for current element, scope, project, or baseline.

### Evidence panel
Shows claims and proof connected to the current selection.

### History panel
Shows recent change sets, uncommitted draft operations, and revision comparison.

### Guide rail
Shows current stage, prompt, rationale, answer options, and related gaps.

### Inspector
Shows typed fields, relations, sources, status, authority, and advanced details.

## Selection model

Selection is one shared concept across explorer, canvas, inspector, guide, and panels.

Selection states:

- no selection,
- one element,
- many homogeneous elements,
- many heterogeneous elements,
- relation,
- path,
- canvas frame,
- change set,
- evidence item,
- finding.

The application should avoid hidden component-local selection that diverges from the studio.

## URL design

Stable routes identify project and optionally element, lens, view, revision, or baseline.

Example:

```text
/workspaces/{workspaceId}/projects/{projectId}
/model/{elementId}?lens=scenario-flow&view={viewId}&revision=42
```

Sensitive titles do not need to appear in URLs.

## Search model

Search results include:

- title and type,
- context path,
- definition status,
- relevant excerpt,
- matching properties,
- inbound and outbound references,
- findings,
- open command.

Search must distinguish exact identifier lookup from text search.

## Empty states

Empty states teach the model:

### New project
"Begin with the outcome. What should be possible when this project succeeds?"

### No actors
"Who can initiate, participate in, approve, support, or be affected by this outcome?"

### No failure paths
"The successful route is defined. Which conditions could prevent, alter, or partially complete it?"

### No evidence
"This model states claims. Decide what would persuade a reviewer that the material claims are true."

Empty states should offer structured choices and a plain-language explanation, not illustrations alone.

## Responsive behavior

The primary authoring experience targets desktop and large tablets. Narrow layouts support review, comments, guide steps, and structured editing but do not attempt to compress the full studio into a phone-sized canvas.

Panel behavior:

- panels collapse to tabs before overlaying content,
- inspector and guide can share the right region,
- bottom panels can dock or open as separate full-height panes,
- layout is user-configurable and persisted as personal view state,
- no panel opening steals focus unless initiated by the user.
