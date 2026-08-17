# ADR-0007: Publish an Open, Deterministic, Versioned Project Format

## Status

Accepted.

## Context

Project models must be portable, reviewable, source-controllable, migratable, and independent of one database deployment. A proprietary opaque format would undermine long-term ownership. A direct database dump would couple users to internal storage and leak operational details.

## Decision

Define a canonical UTF-8 JSON interchange format with:

- explicit media type and `.project-builder.json` extension,
- semantic `formatVersion`,
- stable GUID identity,
- deterministic property and array ordering,
- UTC timestamps,
- decimal-safe values,
- typed elements and relations,
- separate views, claims, evidence, and extensions,
- versioned safe extension envelopes,
- JSON Schema plus semantic validation,
- deterministic SHA-256 content hash,
- transactional migration.

A deterministic ZIP bundle may package assets and evidence manifests, but plain JSON remains the portable semantic core.

## Consequences

### Benefits

- source control and review,
- reliable diff/hash,
- external tooling,
- import/export safety,
- migration fixtures,
- reduced vendor lock-in.

### Costs

- compatibility promises,
- canonical serializer,
- migration and unknown-extension policy,
- schema does not replace semantic validation,
- large projects may need streaming/package support.

## Security

Imports enforce size, depth, count, path, active-content, URI, extension, and resource limits. Import never executes code. Unsafe or unsupported content is rejected or quarantined before persistence.

## Rejected alternatives

- database backup as interchange,
- binary-only project file,
- editor-specific diagram documents,
- JSON with arbitrary unversioned payloads,
- text merge as the semantic merge engine.

## Validation

- export twice yields byte-identical content,
- export-import-export round trip,
- supported-version migration fixtures,
- unknown extension preservation,
- malicious and resource-exhaustion fixtures,
- semantic reference resolution.

## Review triggers

- streaming requirements exceed one JSON document,
- signing/encryption becomes a core format feature,
- executable models require a separate package contract,
- major-version semantics change.
