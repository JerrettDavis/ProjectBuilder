# Persistence, Revisions, and Concurrency

## Persistence strategy

Project Builder uses:

- normalized relational tables for identity, ownership, indexing, relations, revisions, and governance,
- JSONB payloads for versioned type-specific element content,
- append-only change sets and operations for audit and reconstruction,
- current-state tables for efficient reads,
- snapshots for historical access and large-project optimization,
- object storage for large binary artifacts.

This is not full event sourcing. The change log is authoritative history, while current state is a first-class persisted representation.

## Core tables

### Workspaces and access

```text
workspaces
workspace_members
workspace_roles
workspace_policies
projects
project_members
project_roles
```

### Model

```text
model_elements
model_relations
project_revisions
change_sets
change_operations
model_snapshots
```

### Views

```text
view_definitions
view_layouts
personal_view_preferences
```

### Definition and evidence

```text
claims
evidence_requirements
evidence_records
claim_evidence_links
gaps
assumptions
decisions
source_references
implementation_references
```

### Collaboration

```text
comments
discussion_threads
review_requests
review_assignments
approvals
baselines
```

### Operations

```text
outbox_messages
background_jobs
idempotency_records
audit_event_references
integration_connections
```

## Model element table

Conceptual shape:

```sql
create table model_elements (
    workspace_id uuid not null,
    project_id uuid not null,
    element_id uuid not null,
    kind text not null,
    parent_id uuid null,
    name text not null,
    description text not null,
    definition_status text not null,
    payload_version integer not null,
    payload jsonb not null,
    element_version bigint not null,
    created_at timestamptz not null,
    created_by uuid not null,
    modified_at timestamptz not null,
    modified_by uuid not null,
    deprecated_at timestamptz null,
    primary key (project_id, element_id)
);
```

Indexes:

- project and parent,
- project and kind,
- project and status,
- GIN where justified for selected JSONB queries,
- normalized search vector,
- workspace and modified time.

Do not index every JSON path speculatively.

## Relation table

```text
project_id
relation_id
relation_type
source_element_id
target_element_id
qualifier_version
qualifier jsonb
relation_version
created and modified audit
```

Foreign keys ensure endpoints exist inside the project unless an explicit cross-project reference type is introduced.

Unique constraints enforce relation cardinality where possible. Semantic cardinality remains domain-validated.

## Project revision

`projects.current_revision` is incremented under optimistic locking in the same transaction as:

- current-state changes,
- change set,
- operations,
- outbox,
- impacted-evidence status updates that must be immediate.

The transaction compares expected revision:

```sql
update projects
set current_revision = current_revision + 1
where project_id = @projectId
  and current_revision = @expectedRevision;
```

Zero affected rows means conflict.

## Element versions

Element versions support more precise conflict reporting. The command can include expected versions for changed elements. The project revision remains the total ordering authority.

A non-conflicting operation based on an older project revision can be automatically rebased only when typed conflict rules prove independence. MVP can conservatively require user rebase.

## Change sets

Change sets store:

- reason,
- actor,
- source client,
- base and result revision,
- operation count,
- semantic summary,
- impacted elements,
- content hash,
- validation profile and findings summary.

Operations store typed canonical payloads. They are not SQL patches.

## Snapshots

Snapshots support:

- historical reads,
- export,
- projections,
- recovery,
- faster reconstruction for diagnostics.

Snapshot policy:

- every approved baseline,
- every N revisions or size threshold,
- before major format migration,
- on demand for export.

Snapshots use the portable model format or an internal versioned equivalent. They are validated on creation.

## Delete semantics

### Draft, unreferenced element
Can be hard-deleted before commit.

### Committed model element
Prefer deprecate or supersede. Hard deletion is a change-set operation only when policy allows and references are resolved.

### Project deletion
Soft-delete with retention window, audit, export option, and explicit purge job.

### Right-to-erasure
Personal data removal can require redaction in model content and audit-preserving pseudonymization. Policy must distinguish authored business model content from user-account data.

## EF Core mappings

- Domain types remain persistence-ignorant.
- Infrastructure maps records through explicit configuration.
- Use value converters only where semantics remain clear.
- Store money and precise quantities as decimal with explicit scale and currency.
- Store timestamps with UTC semantics and domain wrappers where appropriate.
- Use owned or complex types only when their update and query behavior is understood.
- Avoid lazy loading.
- Queries use no-tracking read models by default.
- Compiled queries are considered only after measurement.
- Migrations are reviewed as production code.

## JSONB use

JSONB is appropriate for:

- type-specific element payload,
- extension payload,
- typed relation qualifiers,
- immutable operation payload,
- generated metadata.

Relational columns remain appropriate for:

- identifiers,
- ownership,
- kind,
- parent,
- status,
- versions,
- timestamps,
- common search and authorization fields,
- relation endpoints.

The model is not stored as one giant JSON document for every edit.

## Historical reads

Options:

1. Load exact snapshot at or before revision, then apply later operations.
2. Use a materialized revision snapshot for approved baselines.
3. For recent comparisons, read change-set semantic summaries plus affected current values.

Historical reads are immutable and cacheable by project and revision.

## Import transaction

Large imports use:

1. upload to quarantine,
2. parse and schema validate,
3. semantic validate in a staging model,
4. show report,
5. commit project creation or replacement atomically through staging tables or controlled transaction,
6. generate snapshot and indexes,
7. publish notification.

The system never streams unvalidated elements directly into live current-state tables.

## Migrations

Database migration workflow:

- migration generated locally,
- SQL reviewed,
- backward and forward compatibility assessed,
- representative data migration test,
- backup and restore plan,
- online or maintenance-window strategy,
- deploy application compatibility before destructive schema change,
- monitor,
- remove old schema in later release.

Model-format migration and database migration are separate concerns.

## Concurrency scenarios

### Two users edit different descriptions
Can rebase automatically if element versions and rules show independence.

### Two users rename the same actor
Conflict with both values and references shown.

### One user deprecates an interaction while another adds a failure path
Structural conflict. Offer restore, move path, or cancel.

### Two users move canvas nodes
Merge by view ownership. Personal views never conflict. Shared view can use last-operation ordering with visible history because semantics are unaffected.

### Evidence sync marks evidence stale while validator reviews it
Evidence record version conflict. Preserve both system finding and reviewer disposition.

## Backup and recovery

Requirements:

- encrypted database backups,
- point-in-time recovery where supported,
- object-storage versioning or equivalent,
- export of approved baselines,
- periodic restore rehearsal,
- documented recovery point and recovery time objectives,
- corruption detection through hashes and validation,
- audit of restore operations.

A backup is not proven until restored and model validation passes.
