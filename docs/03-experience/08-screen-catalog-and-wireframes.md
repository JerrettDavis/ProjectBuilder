# Screen Catalog and Wireframes

These wireframes describe information and interaction structure. They are not visual-brand specifications.

## S-001: Workspace Home

Purpose:
- Resume work and expose reviews, gaps, and activity.

```text
┌──────────────────────────────────────────────────────────────┐
│ Project Builder     Search                    Help      User  │
├──────────────────────────────────────────────────────────────┤
│ Workspace: Headroom Labs                  [+ New project]     │
│                                                              │
│ Continue                                                     │
│ ┌──────────────────────────┐  ┌──────────────────────────┐   │
│ │ Point of Sale            │  │ Project Builder Dogfood  │   │
│ │ 3 blockers, rev 42       │  │ Review requested         │   │
│ │ Continue item scan       │  │ Open baseline            │   │
│ └──────────────────────────┘  └──────────────────────────┘   │
│                                                              │
│ Reviews        Gaps assigned to me       Recent activity      │
└──────────────────────────────────────────────────────────────┘
```

## S-002: New Project

Purpose:
- Establish intent without demanding technical structure.

```text
What are you trying to make possible?
[ Develop a point-of-sale system                              ]

Who receives value?
[ Add actor or describe beneficiary                           ]

How will you recognize success?
[ Observable outcome                                          ]

Scope
[ Included ] [ Excluded ] [ Unknowns ]

Starting point
(•) Guided empty project
( ) Point-of-sale example
( ) Import existing model
```

## S-003: Project Overview

```text
┌──────────────────────────────────────────────────────────────┐
│ Point of Sale      Exploring     Rev 17     Open Studio       │
├──────────────────────────────────────────────────────────────┤
│ Purpose                                                      │
│ Enable staffed retail sales...                               │
│                                                              │
│ Outcomes                 Readiness by purpose                 │
│ ✓ Complete sale          Discovery: Ready                    │
│ ! Recover safely         Interface: 3 material gaps           │
│                                                              │
│ Recommended next                                            │
│ Model what the clerk sees when a product is not found.       │
│ [Continue in Guide] [Open Scenario]                           │
│                                                              │
│ Actors | Episodes | Interfaces | Systems | Evidence           │
│ 7        3          2            5         18/24 current       │
└──────────────────────────────────────────────────────────────┘
```

## S-004: Studio, Scenario Flow

```text
┌─────────────────────────────────────────────────────────────────────────────┐
│ Point of Sale  Story Flow State Interface System Evidence   Rev 17  Review  │
├──────────────┬────────────────────────────────────────────┬─────────────────┤
│ Narrative    │ Complete Sale / Recognized Item for Cash  │ Interaction     │
│              │ [Select][Add][Connect][Path][Layout]       │ Add product     │
│ ▾ Episode    │                                            │                 │
│   ▾ Scenario │ Clerk        POS            Price Book     │ Initiator Clerk │
│     Scene 1  │  ○ scan ───▶ ○ classify                    │ Intent Add item │
│     Scene 2  │               │                            │ Interface Scan  │
│     Scene 3  │               ├────lookup────▶ ○           │                 │
│              │               ◀────price──────┤             │ Findings 1     │
│              │               ○ add line                    │ Evidence 2     │
│              │               ├─not found─▶ [result]        │                 │
├──────────────┴────────────────────────────────────────────┴─────────────────┤
│ Problems (3) | Evidence | History | Comments | Simulation                  │
└─────────────────────────────────────────────────────────────────────────────┘
```

## S-005: Guide Rail

```text
Stage 7 of 11: Failure paths

What should happen when the Corporate Price Book
cannot be reached?

Why this matters
The current interaction crosses an availability boundary.
The clerk has no modeled observation or recovery action.

[ Use approved cached price ]
[ Ask clerk to retry ]
[ Require manager override ]
[ Stop item add ]

[Describe another path]

[Unknown] [Assume] [Not applicable] [Defer]

This answer will add:
• Exceptional or degraded path
• Interface observation
• Recovery rule
• Evidence requirement
```

## S-006: Actor Editor

Sections:

```text
Identity
  Name, actor kind, contexts, description

Role
  Goals, responsibilities, knowledge, constraints

Authority
  Initiates, approves, overrides, supports

Interfaces
  Uses, receives, inaccessible or environmental constraints

Participation
  Episodes, scenarios, interactions

Sources and review
  Source references, domain authority, status
```

## S-007: State and Rule Lens

```text
State table: Transaction

Current        Trigger              Condition             Next          Result
Open           Add product          Price resolved        Open+Line     Added
Open           Add product          Product unknown       Open          NotFound
Open           Add product          Service unavailable   Open/Pending  Unavailable
Completed      Add product          Any                   Completed     Closed

Invariants
✓ Completed transaction cannot accept lines
✓ Active line quantity is positive
! Tax treatment not defined
```

## S-008: Interface Designer

```text
┌─────────────────────────────────────────────────────────────────────────────┐
│ Interface: Staffed POS      Scenario: Product added      State 3 of 6       │
├──────────────┬────────────────────────────────────────────┬─────────────────┤
│ Components   │ ┌────────────────────────────────────────┐ │ Bindings        │
│ Frame        │ │ Sale                                   │ │ Text            │
│ Text         │ │ Item A                       $2.49      │ │ ← Line.Name     │
│ Data         │ │                                        │ │ ← Line.Price    │
│ Input        │ │ Total                        $2.49      │ │                 │
│ Action       │ │                                        │ │ Intent          │
│ Status       │ │ [Scan ready]                           │ │ Add scanned     │
│              │ └────────────────────────────────────────┘ │                 │
│              │ ① Scan  ② Pending  ③ Added  ④ Announce    │ A11y             │
├──────────────┴────────────────────────────────────────────┴─────────────────┤
│ Paths: Happy | Not found | Price unavailable | Transaction closed          │
└─────────────────────────────────────────────────────────────────────────────┘
```

## S-009: System Context

```text
[Clerk] ─scan─> [POS Application] ─price request─> [Corporate Price Book]
                        │
                        ├─authorize─> [Payment Provider]
                        ├─print─> [Receipt Printer]
                        └─support event─> [Support Platform]

Boundaries:
• Store device trust boundary
• Corporate network boundary
• Vendor payment boundary
• Local transaction boundary
```

Selecting a crossing opens contract, data, failure, quality, and evidence details.

## S-010: Problems Panel

```text
Scope: Recognized Item for Cash     Profile: Implementation Ready

Blockers (1)
PB-BOUND-002  Price Book crossing has no contract.

Errors (2)
PB-PATH-008   Product-not-found result has no clerk observation.
PB-EVID-001   Transaction invariant has no evidence requirement.

Warnings (3)
PB-OPS-003    Price Book availability objective is unspecified.
...
```

Actions:
- open,
- explain,
- fix in guide,
- create gap,
- suppress if allowed,
- assign.

## S-011: Evidence Matrix

```text
Claim                         Example  Property  Contract  E2E  Ops  Status
Recognized product is added      ✓        ✓          ·      ✓    ·   Current
Completed sale rejects add       ✓        ✓          ·      ·    ·   Current
Price Book mapping               ✓        ·          !      ·    ·   Missing
Offline price policy             ·        ·          ·      ·    ?   Unknown
```

## S-012: Review Packet

Sections:

- baseline purpose and scope,
- changed scenarios,
- changed rules and invariants,
- boundary and contract changes,
- findings and accepted gaps,
- evidence status,
- semantic diff,
- approval controls.

## S-013: Conflict Resolution

```text
Element: Price unavailable path
Base revision: 41

Current committed
Observation: "Price unavailable. Retry."
Recovery: Retry only

Your draft
Observation: "Corporate pricing offline."
Recovery: Use cached price with manager approval

[Keep current] [Use draft] [Combine as separate paths] [Open full context]
```

The product does not present raw JSON as the primary conflict interface.

## S-014: History

```text
Rev 42  JD Davis  Model offline price policy
  + Degraded path
  + Manager authorization
  ~ Price Book boundary
  ! 4 evidence items potentially stale

[Open diff] [Create baseline] [Export] [Propose inverse change]
```

## S-015: Generated Output

Tabs:

- Behavioral specification.
- State tables.
- Interface contract.
- Vertical-slice plan.
- Test plan.
- C# scaffold preview.
- Traceability matrix.

Each output shows source revision, generator version, warnings, and copy or export controls.
