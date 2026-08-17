# Security, Privacy, and Threat Model

## Security posture

Project Builder stores the definitions of systems, organizations, actors, boundaries, vulnerabilities, and implementation plans. This content can be more sensitive than ordinary project-management data. Security is therefore a product capability and a modeled concern.

The initial verification target is aligned with OWASP ASVS 5.0 at a level selected by deployment context, with explicit threat modeling and secure-development evidence.

## Assets

- project model content,
- proprietary business rules,
- system and vendor topology,
- security and trust boundaries,
- interface contracts,
- implementation references,
- evidence and test results,
- credentials and integration tokens,
- identity and membership data,
- audit records,
- exports and backups,
- agent prompts and responses.

## Trust boundaries

1. Browser to Web Host.
2. Web Host to database and object storage.
3. Web Host or Worker to identity provider.
4. Application to source control and CI.
5. Application to notification services.
6. Application to external agent provider.
7. plugin or projection worker to core application.
8. workspace tenant to another workspace.
9. administrator to ordinary member.
10. public or shared export to private project.

These boundaries should exist in Project Builder's own dogfood model.

## Authentication

Default browser authentication:

- ASP.NET Core Identity for standalone deployments.
- secure, HTTP-only, SameSite cookies.
- anti-forgery protection.
- email confirmation and recovery policy.
- TOTP or stronger multi-factor support.
- lockout and abuse controls.
- session revocation.

Enterprise:

- OpenID Connect or SAML through a supported identity provider adapter.
- group-to-role mapping with explicit review.
- just-in-time provisioning policy.
- domain and tenant restrictions.
- single logout limitations documented.

Do not build a custom OAuth authorization server unless product requirements demand it.

## Authorization

Authorization is resource- and claim-based:

```text
Actor
+ Workspace membership
+ Project role
+ Claim category authority
+ Element scope
+ Operation
+ Policy
→ Decision
```

Server-side checks occur for every command, query, realtime subscription, export, and object-storage download.

UI visibility is not authorization.

## Tenant isolation

- Every tenant-owned table includes workspace scope.
- Queries require workspace and project scope.
- composite keys and database constraints reduce cross-tenant reference risk.
- integration credentials are workspace-scoped.
- caches include tenant scope.
- background jobs carry tenant scope.
- logs avoid raw tenant content.
- isolation tests attempt cross-workspace identifier substitution.

Row-level security can be evaluated as defense in depth but does not replace application authorization.

## Content classification

Workspaces can classify projects and elements:

- Public.
- Internal.
- Confidential.
- Restricted.

Classification affects:

- sharing,
- export,
- agent access,
- telemetry redaction,
- integration sync,
- retention,
- reviewer authority,
- object-storage policy.

Sensitive data can also be tagged at field or artifact level.

## Agent security

Agent integration is optional and disabled by default for restricted content.

Controls:

- user-selected scope,
- explicit provider connection,
- minimum required model content,
- redaction or pseudonymization,
- tool allowlist,
- no credential exposure,
- no automatic semantic commit,
- proposal diff,
- full audit,
- retention disclosure,
- output marked untrusted until reviewed,
- prompt-injection defense for imported content.

Imported project text and attachments are data, not trusted instructions to the agent.

## Import threats

Mitigations:

- size and nesting limits,
- streaming parser,
- ZIP entry count and compression-ratio limits,
- path traversal prevention,
- SVG sanitization or rasterization,
- no active HTML execution,
- URI scheme allowlist,
- content-type verification,
- malware scanning where available,
- extension allowlist,
- quarantine before validation,
- no plugin installation from project file,
- timeout and memory limits.

## Injection

Protect:

- SQL through parameterization and EF Core,
- HTML through encoded rendering and sanitized rich content,
- command or shell through no string concatenation and strict process APIs,
- template injection through constrained templates,
- log injection through structured logging,
- graph query injection through typed queries,
- prompt injection through data/instruction separation.

Markdown rendering uses a safe allowlist and does not permit arbitrary scripts or event attributes.

## CSRF, CORS, and browser security

- Same-origin hosted WebAssembly client.
- Cookie-authenticated writes require anti-forgery.
- restrictive CORS, usually same-origin only.
- Content Security Policy.
- frame-ancestors policy.
- HTTPS and HSTS in production.
- secure headers.
- no secrets in WebAssembly.
- short-lived signed download URLs.
- protected local storage; sensitive data remains server-side where possible.

## Realtime security

- authenticate SignalR connections,
- authorize project group join,
- revalidate access after role changes,
- limit message size and frequency,
- do not trust client presence state,
- disconnect revoked users,
- avoid broadcasting sensitive draft content,
- correlate but do not log full payloads.

## Audit

Audit-worthy actions:

- login, logout, MFA, recovery,
- role and policy change,
- project access grant,
- export, import, delete, restore,
- model commit,
- baseline approval and waiver,
- evidence attachment,
- integration connection and token rotation,
- agent invocation and proposal disposition,
- extension install or execution,
- administrative data access.

Audit records should be append-only within application control and protected from ordinary project editors.

## Secrets

- never store secrets in project model fields,
- use platform secret stores or protected configuration,
- encrypt integration tokens at rest with key rotation,
- display token metadata, not secret value,
- isolate development secrets,
- scan repository and artifacts,
- document revocation.

## Privacy

Privacy capabilities:

- data inventory,
- retention profiles,
- export of personal account data,
- account deletion and content ownership policy,
- pseudonymization in audit where required,
- consent and notice for analytics,
- agent-provider disclosure,
- regional storage choices for enterprise deployments,
- attachment and evidence retention,
- model-field classification.

The product should discourage unnecessary personal data in actor definitions. Actors are roles; named people belong in source and authority records only when necessary.

## Threat scenarios

### Cross-tenant model access
Attacker substitutes a project identifier.

Evidence:
- authorization integration tests,
- database scope tests,
- cache key tests,
- security review.

### Malicious model import
Attacker uploads a ZIP bomb or active SVG.

Evidence:
- fuzzing,
- size-limit tests,
- sanitizer tests,
- quarantine integration test.

### Stolen integration credential
Attacker uses source-control token.

Controls:
- least scope,
- encryption,
- rotation,
- provider audit,
- revocation,
- connection health.

### Agent prompt injection
Imported content tells agent to exfiltrate context.

Controls:
- treat content as untrusted,
- tool allowlist,
- scope isolation,
- no secret context,
- human review,
- audit.

### Approval impersonation
User attempts to approve outside authority.

Controls:
- claim-category authorization,
- immutable approval record,
- step-up authentication for sensitive approvals if policy requires.

### Evidence forgery
A passing status is submitted without authentic CI provenance.

Controls:
- signed provider webhook,
- source run identifier,
- immutable evidence metadata,
- reviewer sufficiency,
- no self-attestation as automated proof.

## Secure-development evidence

- dependency scanning,
- secret scanning,
- static analysis,
- threat-model review,
- ASVS checklist,
- authorization tests,
- fuzz tests for import and parsers,
- dynamic scan,
- penetration test before broad external release,
- restore and incident rehearsals,
- security regression tests.

## Incident response

Document:

- security contact,
- severity,
- containment,
- credential rotation,
- tenant notification,
- forensic preservation,
- audit export,
- patch and deployment,
- post-incident model refinement.

Security findings and incidents become evidence and gaps in Project Builder's dogfood model.
