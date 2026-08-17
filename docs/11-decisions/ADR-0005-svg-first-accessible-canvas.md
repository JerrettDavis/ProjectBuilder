# ADR-0005: Use an SVG-First Canvas Behind a Renderer Abstraction

## Status

Accepted for the first visual-lens implementation, contingent on performance evidence.

## Context

Project Builder needs typed nodes, connectors, nested scopes, labels, keyboard interaction, accessible alternatives, overlays, and diagram export. Early models are expected to benefit more from semantic clarity and accessibility than extreme rendering scale.

HTML/SVG integrates with browser semantics and styling. Canvas/WebGL can render larger graphs but requires separate hit testing, text, accessibility, and export systems.

## Decision

Implement the initial canvas in SVG with:

- immutable lens graph input,
- viewport culling,
- batched updates,
- command-based interaction,
- semantic outline,
- keyboard equivalents,
- persisted layout separate from semantic state,
- renderer-neutral selection and geometry contracts.

Use thin JavaScript interop for pointer capture and browser measurement where necessary.

Do not bind domain semantics to SVG elements. A WebGL renderer may be introduced later behind the same contracts.

## Consequences

### Benefits

- inspectable DOM and text,
- easier semantic grouping and export,
- better starting point for focus and accessibility,
- lower initial renderer complexity,
- deterministic screenshots and SVG output.

### Costs

- very large visible graphs may become slow,
- nested transforms and connector routing need discipline,
- browser differences need testing,
- semantic outline remains necessary because visual SVG alone is not a complete accessible interaction.

## Reference performance cases

Measure:

- 250, 1,000, and 5,000 projected nodes,
- selection and pan latency,
- incremental relation update,
- auto-layout application,
- scenario overlay,
- memory and accessibility-tree cost.

The product should encourage scope and drilldown rather than render the entire enterprise at once.

## Rejected alternatives

- untyped third-party whiteboard as the canonical editor,
- Canvas 2D without an accessibility plan,
- WebGL before reference measurements,
- diagram library whose license or data model constrains product ownership.

## Review triggers

- reference model interactions exceed latency budget,
- DOM/accessibility-tree size is unacceptable,
- required visual features are impractical in SVG,
- a suitable renderer can be added without semantic compromise.
