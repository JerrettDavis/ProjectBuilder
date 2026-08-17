# Accessibility, Keyboard, and Focus

## Target

Project Builder targets WCAG 2.2 AA and treats authoring-tool accessibility as part of the product model. The product should also model accessibility constraints for the interfaces its users design.

## Non-negotiables

- Every core action is operable by keyboard.
- Dragging is never the sole interaction.
- Focus is visible.
- Background updates do not steal focus.
- Opening panels does not reset the active editor unless the user selected that panel.
- Status is not conveyed by color alone.
- Canvas content has a structured equivalent.
- Motion respects reduced-motion preferences.
- Timeouts are avoidable, extendable, or clearly communicated.
- Error messages identify the problem and a recovery action.

## Keyboard model

### Global

| Command | Default |
|---|---|
| Command palette | `Ctrl+K` |
| Global search | `Ctrl+Shift+F` |
| Commit change set | `Ctrl+S` |
| Undo draft operation | `Ctrl+Z` |
| Redo | `Ctrl+Y` or `Ctrl+Shift+Z` |
| Toggle explorer | `Ctrl+Shift+E` |
| Toggle inspector | `Ctrl+Shift+I` |
| Toggle guide | `Ctrl+Shift+G` |
| Problems panel | `Ctrl+Shift+M` |
| Go back | `Alt+Left` |
| Go forward | `Alt+Right` |
| Open selected | `Enter` |
| Context menu | `Shift+F10` |
| Escape current mode | `Escape` |

Shortcuts are configurable and checked for browser and assistive-technology conflicts.

### Canvas

| Command | Behavior |
|---|---|
| Arrow keys | Move focus among spatially related nodes |
| `Tab` | Move through logical interactive order |
| `Shift+Tab` | Reverse |
| `Space` | Select or toggle selection based on mode |
| `Enter` | Open or drill down |
| `Ctrl+Enter` | Open inspector |
| `Alt+Arrow` | Move selected layout element |
| `Alt+Shift+Arrow` | Resize where applicable |
| `C` | Connect selected element through accessible dialog |
| `A` | Add related element menu |
| `Delete` | Open semantic remove choices |
| `F2` | Rename |
| `Home` | First element in scope |
| `End` | Last element in scope |

Single-letter shortcuts are disabled while typing and configurable.

## Focus architecture

The application maintains a focus service with stable focus targets:

```text
ElementFocus(elementId)
FieldFocus(elementId, fieldKey)
PanelFocus(panelId, itemId)
CanvasCoordinateFocus(viewId, virtualNodeId)
CommandFocus(commandId)
```

After operations:

- rename returns focus to the renamed element,
- creating a related element focuses its first required field,
- closing a dialog returns focus to its invoker,
- deleting or deprecating focuses the nearest logical sibling,
- resolving a finding returns focus to the originating field or next finding,
- a collaboration refresh preserves current focus target.

## Modal policy

Use modal dialogs only for:

- destructive or high-consequence confirmation,
- conflict resolution,
- authentication or permission boundary,
- bounded choice that cannot coexist with current context.

Prefer non-modal inspector, guide, and panels for ordinary editing.

No modal opens automatically because another user changed the project.

## Canvas accessibility tree

Canvas elements expose:

- role,
- type,
- name,
- status,
- position in logical sequence,
- inbound and outbound relationship count,
- findings,
- expanded or collapsed state,
- commands.

Edges appear in the selected element's relationship list. Users do not have to traverse decorative edge paths.

## Drag alternatives

| Drag action | Alternative |
|---|---|
| Move node | Cut/paste, Move command, arrow movement |
| Reparent | Move to Context dialog |
| Connect | Connect command with type and target search |
| Reorder | Move Up/Down/To Position |
| Resize | Size fields or keyboard resize |
| Marquee select | Filter, Select All in Scope, multi-select list |
| Pan | Scroll bars, arrow pan, mini-map, Fit commands |
| Drop from palette | Add command and choose location |

## Screen reader announcements

Announce:

- element created or removed from view,
- semantic relation added,
- validation finding added or resolved,
- commit succeeded or conflicted,
- scenario playback step and state changes,
- collaboration lock or conflict,
- background projection completed,
- import progress at meaningful intervals.

Do not announce every pointer movement, pan, zoom, or presence cursor.

## Color and status

Status uses:

- text label,
- icon or shape,
- optional color,
- accessible description.

Path types use label and line pattern in addition to color.

## Target size and spacing

Interactive targets meet WCAG 2.2 AA expectations. Dense expert mode can reduce visual padding only while preserving operable target behavior through spacing, keyboard access, and zoom.

## Forms and errors

- Labels remain visible.
- Required fields are identified in text.
- Error summaries link to fields.
- Validation does not erase input.
- Async validation indicates pending status.
- Errors describe the violated model rule and possible resolutions.
- Unknown and Not Applicable controls are not hidden in an overflow menu.

## Reduced motion

Disable or simplify:

- animated zoom,
- auto-layout transitions,
- scenario-flow motion,
- pulsing collaboration indicators,
- panel slide transitions.

Scenario playback can use discrete step changes with narrated state.

## Cognitive accessibility

- Plain-language mode.
- Definitions available in context.
- One recommended action at a time.
- Consistent terminology.
- Stable spatial layout.
- Examples that can be toggled.
- Ability to save and resume.
- No forced countdowns.
- Clear distinction among error, warning, suggestion, and unknown.

## Testing

Accessibility evidence includes:

- automated checks,
- keyboard-only scripted tests,
- screen-reader walkthroughs,
- focus restoration tests,
- high-contrast and zoom tests,
- reduced-motion tests,
- target-size review,
- usability sessions with participants who use assistive technology.

Automated checks are necessary but not sufficient.

## Product modeling support

The Interface Designer should allow users to model:

- focus order,
- accessible name and description,
- role and state,
- keyboard interactions,
- announcements,
- non-drag alternatives,
- contrast and non-color cues,
- time limits,
- reduced-motion behavior,
- error identification.

These become claims and evidence requirements in the target project.
