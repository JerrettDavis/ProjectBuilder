# Deployment and Environments

## Principles

- Local orchestration is code-defined and reproducible.
- Production topology is chosen independently from Aspire's developer control plane.
- The same application artifact moves through environments.
- Configuration and secrets are external.
- Database and model-format migrations are explicit.
- Every deployment can be traced to source, build, dependencies, and product-model baseline.

## Environment set

### Local
Developer machine through Aspire.

Resources:

- Web Host.
- optional Worker.
- PostgreSQL container.
- local object storage adapter or emulator.
- local mail or notification sink.
- OpenTelemetry dashboard or collector.
- seeded dogfood and POS models.

### CI
Ephemeral environment for:

- build and unit tests,
- architecture tests,
- integration tests with real PostgreSQL,
- contract tests,
- import/export fixtures,
- generator compile tests,
- browser E2E,
- security and dependency checks.

### Development
Shared environment for continuous integration and internal dogfooding.

### Preview
Per-PR or per-branch where cost permits, with isolated data and no production credentials.

### Staging
Production-like configuration, migration rehearsal, performance and security validation.

### Production
Customer-facing or self-hosted deployment with backups, monitoring, audit, and support policy.

## Local Aspire model

Conceptual AppHost:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume();

var database = postgres.AddDatabase("projectbuilder");

var web = builder.AddProject<Projects.ProjectBuilder_Web>("web")
    .WithReference(database)
    .WaitFor(database);

builder.AddProject<Projects.ProjectBuilder_Worker>("worker")
    .WithReference(database)
    .WaitFor(database);

builder.Build().Run();
```

The real AppHost adds object storage and observability integrations as chosen. AppHost does not contain production business settings or secrets.

## Packaging

Preferred primary artifact:

- OCI container image for Web.
- optional OCI image for Worker.
- migration bundle or controlled application migration command.
- SBOM.
- provenance or signed build metadata.
- model schema and projection version manifest.

Self-contained non-container deployment can be supported later if customer needs justify it.

## Single-node deployment

For evaluation and small self-hosting:

```text
Reverse proxy
  → Web container
  → Worker container or in-process jobs
  → PostgreSQL
  → Object storage directory or service
```

Document limitations:

- availability,
- backup,
- scaling,
- object-storage durability,
- certificate management,
- upgrade procedure.

## Scaled deployment

```text
Load balancer
  → multiple Web instances
  → shared PostgreSQL
  → shared object storage
  → worker pool
  → SignalR backplane or managed service if needed
  → OpenTelemetry collector
```

Sticky sessions should not be required for the API. Blazor WebAssembly reduces server circuit dependency. SignalR scale strategy is added when multiple instances require it.

## Cloud portability

The application consumes:

- PostgreSQL connection.
- S3-, Azure Blob-, or compatible object-storage adapter.
- OIDC identity provider.
- OTLP endpoint.
- SMTP, webhook, or provider notification adapter.

Provider-specific deployment modules can exist under `eng/` or separate repositories. Domain and Application remain provider-neutral.

## Configuration

Configuration precedence:

1. safe compiled defaults,
2. environment-specific non-secret configuration,
3. environment variables or mounted configuration,
4. secret provider,
5. approved workspace runtime settings where applicable.

All settings are documented with:

- name,
- type,
- default,
- required environments,
- sensitivity,
- reload behavior,
- validation.

Invalid critical configuration fails startup with safe diagnostics.

## Database deployment

Options:

- dedicated migration job before Web rollout,
- application startup migration only for local development,
- backward-compatible expand and contract for production,
- advisory lock to prevent concurrent migration,
- schema version health.

Never let every production instance race to apply migrations.

## Release flow

1. Build once.
2. Generate SBOM and provenance.
3. Run all required evidence.
4. publish immutable artifacts.
5. Deploy to staging.
6. apply migration.
7. run health and smoke checks.
8. execute selected dogfood scenarios.
9. approve release baseline.
10. promote same artifacts.
11. monitor.
12. retain rollback or forward-fix capability.

## Rollback

Rollback analysis distinguishes:

- application image rollback,
- database schema compatibility,
- model-format compatibility,
- background job compatibility,
- generated artifact compatibility.

A destructive model or schema migration can make image rollback unsafe. In that case, use forward recovery and document the condition before deployment.

## Zero- or low-downtime change

Use:

- additive schema first,
- dual read or write only when explicitly tested,
- background backfill,
- feature activation after data readiness,
- old field removal in later release,
- client/server contract compatibility window,
- websocket reconnect behavior.

## Data residency and tenancy

Deployment tiers may support:

- shared multi-tenant database with strong application isolation,
- database per enterprise workspace,
- dedicated stack,
- regional object storage,
- customer-managed keys.

These are product and operations decisions with measurable cost. Do not implement every isolation topology for MVP.

## Backups

- database full and incremental or point-in-time strategy,
- object storage versioning,
- encryption,
- retention,
- offsite or cross-region policy,
- restore automation,
- restore validation against model schema,
- periodic rehearsal,
- customer export for approved baselines.

## Disaster recovery

Define by tier:

- recovery point objective,
- recovery time objective,
- failover authority,
- DNS and certificate behavior,
- identity dependency,
- data reconciliation,
- communication,
- return-to-primary.

DR plans are modeled human and system scenarios in Project Builder.

## Environment data policy

- no production model content copied to lower environments without explicit sanitization and authorization,
- seed synthetic POS and dogfood models,
- credentials unique per environment,
- test agent providers with non-sensitive data,
- preview environments auto-expire,
- logs and exports follow environment retention.

## Infrastructure as code

Deployment definitions are versioned and tested. They can use the organization's selected tool. Project Builder documentation specifies required capabilities rather than mandating Terraform, Bicep, Pulumi, or another mechanism without deployment context.

## Support matrix

Before beta, publish:

- supported .NET runtime and container base,
- PostgreSQL versions,
- browsers,
- identity providers or protocols,
- object storage adapters,
- upgrade paths,
- model-format window,
- backup expectations.

Pin exact production versions in release manifests, not timeless architecture prose.
