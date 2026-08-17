# API, Realtime, and Integration Contracts

## API principles

- Same-origin browser traffic uses secure cookies by default.
- APIs express application use cases, not raw database CRUD.
- Contracts are versioned and source-generated for serialization.
- Semantic results map consistently to HTTP and realtime messages.
- OpenAPI is generated from first-party ASP.NET Core support.
- Every state-changing operation supports correlation and appropriate idempotency.
- Authorization is server-side.
- Errors use a stable Problem Details extension model.

## Base API shape

```text
/api/v1/workspaces
/api/v1/workspaces/{workspaceId}/projects
/api/v1/projects/{projectId}
/api/v1/projects/{projectId}/model
/api/v1/projects/{projectId}/change-sets
/api/v1/projects/{projectId}/validate
/api/v1/projects/{projectId}/views
/api/v1/projects/{projectId}/reviews
/api/v1/projects/{projectId}/baselines
/api/v1/projects/{projectId}/evidence
/api/v1/projects/{projectId}/projections
/api/v1/projects/{projectId}/exports
```

Versioning strategy is selected before public beta. URI versioning is shown for clarity, not mandated without ADR.

## Headers

Recommended:

```text
X-Correlation-Id
Idempotency-Key
If-Match
ETag
Traceparent
```

Correlation is generated when absent and returned. Sensitive internal identifiers are not exposed unnecessarily.

## Problem contract

```json
{
  "type": "https://projectbuilder.dev/problems/model-conflict",
  "title": "The project changed before this change set could be committed.",
  "status": 409,
  "code": "PB-CONCURRENCY-001",
  "correlationId": "0191...",
  "projectRevision": 42,
  "conflicts": [
    {
      "elementId": "0191...",
      "field": "name",
      "base": "Price lookup",
      "current": "Resolve store price",
      "proposed": "Get item price"
    }
  ]
}
```

Problem responses are safe for the caller and do not include stack traces or provider secrets.

## Project query

Example:

```http
GET /api/v1/projects/{projectId}/model?root={elementId}&depth=2
If-None-Match: "project-42-scope-..."
```

Response includes:

- project revision,
- elements and relations in requested scope,
- continuation token if needed,
- findings summary,
- content ETag.

## Commit change set

```http
POST /api/v1/projects/{projectId}/change-sets
If-Match: "project-42"
Idempotency-Key: 0191...
```

The body conforms to the change-set contract. Successful response returns revision 43, changed identifiers, findings, and impact.

## Bulk operations

Bulk commands remain typed:

- bulk status change,
- add tags,
- move scope,
- import elements,
- resolve findings through declared actions.

A generic JSON Patch endpoint is not the canonical semantic API.

## SignalR

Realtime channels:

### Project hub
- revision committed,
- finding summary changed,
- evidence status changed,
- comment or review activity,
- presence,
- projection status,
- shared-view layout updates.

### User hub
- assigned gap,
- review requested,
- export ready,
- integration error,
- security notification.

Clients subscribe to authorized project groups. Joining a group rechecks access.

## Realtime message example

```json
{
  "type": "project.revision-committed",
  "schemaVersion": 1,
  "projectId": "0191...",
  "revision": 43,
  "changeSetId": "0191...",
  "changedElementIds": ["0191..."],
  "summary": "Added product-not-found recovery path",
  "correlationId": "0191..."
}
```

Realtime messages notify. Clients query authoritative state or apply verified operation deltas.

## Presence protocol

Presence messages are throttled and ephemeral:

- joined project,
- left project,
- active lens,
- selected element if allowed,
- editing scope,
- heartbeat.

Do not persist cursor streams or place them on the outbox.

## Integration architecture

Integrations implement ports:

```csharp
public interface ISourceControlEvidenceProvider
{
    ValueTask<ImplementationReferenceResult> ResolveAsync(
        SourceReference reference,
        CancellationToken cancellationToken);
}

public interface INotificationSink
{
    ValueTask SendAsync(
        Notification notification,
        CancellationToken cancellationToken);
}
```

Integrations live behind explicit user connections and permissions.

## Source control and CI

Potential capabilities:

- link repository, PR, commit, file, symbol, test, and workflow run,
- verify references,
- import test results as candidate evidence,
- publish baseline projections,
- open a bounded implementation work item,
- detect changed implementation references.

Imported data is not automatically considered sufficient evidence.

## Webhooks

Outbound webhook event families:

- baseline approved,
- review requested,
- projection completed,
- evidence changed,
- high-severity gap opened,
- project archived.

Webhook requirements:

- signing secret or asymmetric signature,
- timestamp and replay window,
- event identifier,
- retries with backoff,
- delivery history,
- secret rotation,
- endpoint disable after repeated failure,
- no sensitive model content unless configured.

Inbound webhooks are provider-specific adapters and validate signature before parsing content.

## MCP surface for Project Builder

A later Project Builder MCP interface can expose:

### Resources
- project overview,
- model element,
- readiness report,
- review packet,
- generated projection.

### Tools
- search model,
- propose change set,
- validate scope,
- request projection,
- attach candidate evidence.

Tools are side-effect classified. Committing a semantic change requires explicit authorization and preferably human confirmation.

## Import/export API

Long-running export:

```text
POST /projects/{id}/exports
→ 202 Accepted with job id
GET /exports/{jobId}
→ status
GET /exports/{jobId}/content
→ authorized short-lived download
```

Export content has hash, revision, format, and generator metadata.

## Rate and size policies

Different policies for:

- interactive reads,
- interactive writes,
- search,
- large import,
- projection,
- agent operations,
- webhook ingress.

Limits are returned in documented errors. Large uploads stream to quarantine rather than buffering in memory.

## Contract testing

Tests cover:

- OpenAPI compatibility,
- JSON serialization snapshots,
- semantic error mapping,
- authorization,
- ETag and concurrency,
- idempotency,
- webhook signatures,
- realtime message compatibility,
- provider adapter contracts,
- import schema and malicious inputs.

Contracts are versioned artifacts connected to model claims.
