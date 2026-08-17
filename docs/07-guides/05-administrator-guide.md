# Administrator Guide

## Purpose

This guide defines the administrative responsibilities for a self-hosted or managed Project Builder deployment. Exact screens and commands will be finalized with implementation, but the operational contracts in this guide are product requirements.

Administrators govern access, data handling, integrations, retention, recovery, security posture, and release compatibility. Administrators do not gain unrestricted visibility into tenant content merely by operating infrastructure.

## Deployment profiles

### Local contributor

- single developer,
- local PostgreSQL,
- development identity,
- Aspire AppHost for orchestration,
- disposable data permitted,
- not suitable for sensitive production models.

### Team development

- shared non-production environment,
- representative identity and authorization,
- isolated test workspaces,
- synthetic or approved data,
- automated deployment,
- backup suitable for development continuity.

### Production single organization

- one organization with multiple workspaces,
- production identity provider,
- encrypted storage and transport,
- formal backup and restore,
- monitoring and alerting,
- retention and audit policy,
- upgrade and rollback or forward-fix plan.

### Managed multi-tenant

- strict tenant isolation,
- organization-level data and agent policies,
- metering and support controls,
- stronger audit and incident procedures,
- tenant export and deletion,
- regional or residency options when promised,
- independent security review.

## Initial configuration

Configure:

- public origin and trusted proxies,
- database connection and pool,
- identity provider and callback origins,
- encryption keys,
- object storage,
- email or notification provider if enabled,
- telemetry exporters,
- data-protection key storage,
- allowed file types and limits,
- project import limits,
- agent providers, classifications, and consent policy,
- feature flags,
- retention,
- backup target,
- support contact.

Secrets must come from a supported secret store or deployment mechanism. They must not be committed to appsettings files.

## Identity and access

### Workspace roles

Recommended starting roles:

| Role | Core authority |
|---|---|
| Workspace Owner | governance, membership, policy, billing or subscription |
| Administrator | operational workspace configuration and recovery requests |
| Project Lead | project governance, baselines, review |
| Modeler | semantic edit and commit |
| Contributor | draft, comment, source contribution according to policy |
| Reviewer | review and approval without default edit |
| Viewer | read approved or allowed content |
| Auditor | scoped audit and evidence access |

Organizations can define custom policies, but server-side permissions remain based on explicit capabilities.

### Access principles

- deny by default,
- least privilege,
- no client-side-only authorization,
- tenant scope on every query and command,
- separation of model access from operational audit access,
- time-bounded support access,
- explicit break-glass procedure,
- regular membership review.

### Service identities

Automations and integrations use dedicated service identities with:

- minimum scopes,
- rotation,
- owner,
- expiration or review date,
- audit,
- no shared human credentials.

## Organization policies

Policy categories:

- permitted project classifications,
- required purpose profiles,
- baseline approval rules,
- accepted extension namespaces,
- attachment types and sizes,
- retention and legal hold,
- export and sharing,
- agent use and provider routing,
- required evidence,
- source-link domains,
- authentication assurance,
- session duration,
- audit retention,
- API limits.

Policy changes are versioned and can make existing projects non-compliant without rewriting their historical revisions.

## Data classification

At minimum support:

- Public.
- Internal.
- Confidential.
- Restricted.

Classification can apply to workspace, project, element, source, attachment, projection, and export. More specific classification cannot be weakened by a child without authorized declassification.

The organization should define:

- permitted storage regions,
- export restrictions,
- agent-provider restrictions,
- logging redaction,
- support access,
- retention,
- encryption requirements.

## Attachments and active content

Administrative controls:

- size and count limits,
- accepted MIME types,
- content sniffing,
- malware scanning,
- decompression limits,
- HTML and SVG sanitization,
- isolated object storage,
- signed short-lived downloads,
- Content-Disposition policy,
- no direct execution,
- quarantine and review workflow.

## Import and extension policy

Project imports can contain rich model content and extension references. Configure:

- maximum uncompressed size,
- nesting depth,
- element/relation/operation counts,
- supported format versions,
- migration behavior,
- unknown-extension preserve/reject policy,
- extension allowlist,
- signature requirement,
- schema retrieval policy,
- timeout and memory limits.

An import is validated before any durable project mutation.

## Agent policy

Agent features remain optional. Organization policy can specify:

- disabled,
- only organization-hosted models,
- approved providers and regions,
- permitted project classifications,
- permitted tasks,
- redaction,
- retention,
- human approval,
- logging,
- per-user quotas,
- evaluation requirements.

Every agent proposal records provenance. Disabling the agent must leave all human workflows operational.

## Backup and recovery

### Backup scope

Include:

- PostgreSQL data,
- object storage and attachments,
- encryption and data-protection keys according to secure key-recovery policy,
- organization configuration,
- extension packages or immutable references,
- projection artifacts when they cannot be regenerated,
- release and audit evidence.

### Recovery objectives

Define and publish:

- Recovery Point Objective.
- Recovery Time Objective.
- backup frequency.
- retention.
- geographic separation.
- encryption.
- test cadence.

### Restore rehearsal

At scheduled intervals:

1. select a recent backup,
2. restore into an isolated environment,
3. validate database integrity,
4. verify object digests,
5. authenticate with test identity,
6. open representative projects,
7. validate revision chains,
8. export a project and compare,
9. run health and smoke evidence,
10. record achieved recovery time and gaps.

A successful backup job is not proof of recoverability.

### Project-level recovery

Semantic history is append-only. Normal user reversal creates a new change set. Administrative point-in-time restoration is reserved for data loss or corruption and must preserve audit records.

## Retention, export, and deletion

Retention policy distinguishes:

- active model content,
- drafts,
- change history,
- comments,
- evidence,
- audit,
- attachments,
- agent interactions,
- operational telemetry,
- backups.

Deletion must account for:

- tenant request,
- legal hold,
- shared evidence,
- immutable release records,
- backup expiration,
- search indexes and caches,
- provider copies under contract.

Exports include a manifest, format version, digests, and scope explanation.

## Monitoring

### Service health

Monitor:

- HTTP and realtime availability,
- database connectivity and saturation,
- object storage,
- background queue,
- identity dependencies,
- projection worker,
- notification providers,
- migration state,
- certificate and key expiration.

### User-impact metrics

Monitor:

- project load latency,
- change-set commit latency and conflict rate,
- validation latency,
- lens projection latency,
- import/export success,
- stale draft recovery,
- collaboration disconnects,
- baseline generation,
- error results by semantic category.

### Security signals

Monitor:

- repeated authorization denial,
- cross-tenant identifier probing,
- import limit violations,
- malware detections,
- unusual exports,
- service-identity anomalies,
- agent policy denials,
- audit access,
- secret and dependency alerts.

Logs must not contain model content by default. Use identifiers, classifications, safe summaries, and trace correlation.

## Incident response

Incident procedures cover:

- availability,
- data corruption,
- cross-tenant exposure,
- credential or key compromise,
- malicious extension or import,
- agent-provider exposure,
- supply-chain compromise,
- backup failure.

The runbook must include:

- detection and severity,
- incident commander,
- containment,
- evidence preservation,
- communication,
- recovery,
- tenant notification obligations,
- post-incident model and control updates.

## Upgrades and migrations

Before production upgrade:

1. read release and compatibility notes,
2. verify current version is supported,
3. back up and verify the backup,
4. run migration rehearsal on representative data,
5. verify extension compatibility,
6. deploy to staging,
7. execute smoke, migration, security, and performance evidence,
8. schedule rollout and observation,
9. prepare rollback or forward-fix,
10. update operational baseline.

Database migrations should be applied by a controlled deployment identity, not opportunistically by every application replica.

## Extension administration

For each extension record:

- publisher,
- namespace and version,
- signature,
- license,
- permissions,
- model kinds and migrations,
- editors and projections,
- network or storage needs,
- compatibility range,
- support owner,
- approval and expiration.

Disable or quarantine an extension without deleting preserved project data.

## Operational checklist

Daily or automated:

- health and alert review,
- backup completion,
- failed jobs,
- certificate/key warnings,
- critical security advisories.

Per release:

- restore rehearsal status,
- migration rehearsal,
- extension compatibility,
- evidence packet,
- capacity and performance,
- incident and accepted-risk review.

Quarterly or policy cadence:

- access review,
- service identity rotation,
- retention enforcement,
- agent/provider review,
- disaster recovery exercise,
- threat-model review,
- dependency and license review.
