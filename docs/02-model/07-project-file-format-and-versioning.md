# Project File Format and Versioning

## Goals

The Project Builder interchange format must be:

- open and documented,
- deterministic,
- human-readable for review,
- strongly versioned,
- lossless for known content,
- preservative for safe unknown extension content,
- independent of database schema,
- suitable for source control,
- streamable for large projects,
- cryptographically hashable.

## Canonical JSON document

The base export is a UTF-8 JSON document with media type:

```text
application/vnd.projectbuilder.project+json
```

Recommended extension:

```text
.project-builder.json
```

A packaged bundle containing assets and evidence manifests may use:

```text
.project-builder
```

The bundle is a ZIP container with a deterministic manifest. The plain JSON format remains the authoritative portable core.

## Top-level shape

```json
{
  "format": "project-builder",
  "formatVersion": "1.0.0",
  "exportedAt": "2026-08-15T12:00:00Z",
  "generator": {
    "name": "Project Builder",
    "version": "0.1.0"
  },
  "project": {},
  "elements": [],
  "relations": [],
  "views": [],
  "claims": [],
  "evidence": [],
  "extensions": {}
}
```

See [the JSON schema](../schemas/project-builder-model.schema.json).

## Deterministic serialization

Canonical export rules:

1. UTF-8 without byte-order mark.
2. Unix line endings.
3. Two-space indentation for review exports.
4. Properties in schema-defined order.
5. Arrays ordered by semantic order, then stable identifier where order has no meaning.
6. ISO 8601 timestamps normalized to UTC.
7. Decimal values serialized without binary floating-point conversion.
8. Enumerations serialized as canonical strings.
9. No transient database keys, row versions, or server paths.
10. Unknown safe extension content preserved verbatim where canonicalization permits.
11. Final newline.
12. SHA-256 computed over canonical bytes when a content hash is requested.

## Versioning

`formatVersion` uses semantic versioning for the interchange contract.

- Patch: clarification or additive schema metadata that does not change valid content.
- Minor: backward-compatible additive element, relation, or field.
- Major: breaking semantic or structural change.

Generator version and projection version are separate. A new C# projection can change without changing the project format.

## Migrations

A migration is:

- explicit,
- deterministic,
- one direction per step,
- idempotent where practical,
- tested against fixtures,
- able to report semantic changes,
- non-destructive by default.

Migration pipeline:

```text
Parse safely
→ validate envelope
→ identify format version
→ preserve original
→ apply ordered migrations in memory
→ validate target schema
→ validate semantic model
→ show migration report
→ commit as one change set
```

Import never partially persists a failed migration.

## Unknown content

When a newer minor version introduces an extension the current application does not understand:

- preserve the raw extension payload,
- show it in a read-only fallback inspector,
- prevent edits that would require semantic understanding,
- retain it on export,
- report compatibility findings.

Unknown core major-version semantics cause import rejection or read-only quarantine.

## Package bundle

Example:

```text
example.project-builder
├── manifest.json
├── project.json
├── assets/
│   ├── <content-hash>.png
│   └── <content-hash>.svg
├── evidence/
│   └── manifest.json
└── signatures/
    └── manifest.sha256
```

Evidence files can be embedded or referenced. Sensitive or proprietary evidence should default to references with integrity hashes rather than inclusion.

## Change-set format

A change set contains semantic operations:

- add element,
- update element,
- move element,
- deprecate element,
- add relation,
- update relation,
- remove relation,
- add or update view layout,
- attach evidence,
- resolve gap,
- establish baseline.

Operations include expected element version for concurrency.

Example:

```json
{
  "changeSetId": "0191...",
  "projectId": "0191...",
  "baseRevision": 17,
  "reason": "Model product-not-found path",
  "operations": [
    {
      "op": "addElement",
      "element": {
        "id": "0191...",
        "kind": "path",
        "parentId": "0191...",
        "name": "Product not found",
        "definitionStatus": "draft",
        "payload": {
          "classification": "exceptional"
        }
      }
    }
  ]
}
```

See [the change-set schema](../schemas/project-builder-changeset.schema.json).

## Revision semantics

- Project revision increments once per committed change set.
- All operations in a change set succeed or none do.
- A no-op change set is rejected unless it records an explicit review or baseline event.
- Element version increments when semantic content changes.
- View version increments separately when layout changes.
- Evidence and comments have their own revisions.
- Import can preserve original model identifiers but creates local change-set history.

## Merge

Merging two exports is a model operation, not a text merge.

The merge engine uses:

- common baseline when available,
- element identity,
- element version,
- field-level typed differences,
- relation identity,
- semantic conflict rules,
- view-state independence.

Conflicts are presented as explicit choices. The system must not resolve semantic conflicts through last-write-wins.

## Source control

Text exports can be stored in Git for review and backup. Recommended practice:

- export approved baselines,
- avoid committing transient personal views,
- include model format and generator versions,
- run schema and semantic validation in CI,
- generate review projections in CI,
- use Project Builder's semantic diff for complex changes.

## Import security

Import validation must protect against:

- decompression bombs,
- excessive nesting,
- oversized fields,
- duplicate identifiers,
- path traversal,
- external entity expansion,
- malicious SVG or HTML,
- unsafe URI schemes,
- unbounded relationship graphs,
- schema resource exhaustion,
- extension code execution.

Assets are content-scanned and served from isolated origins or safe rendering pipelines.
