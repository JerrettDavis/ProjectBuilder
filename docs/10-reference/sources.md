# External Sources and Standards

## Purpose

The Project Builder plan uses official or primary sources for current platform, security, accessibility, and data-store guidance. This file records the source set reviewed when the documentation package was assembled. Product and architecture decisions remain Project Builder decisions; a source does not make a design choice automatically correct.

**Last reviewed:** 2026-08-15

## .NET platform

### .NET 10 support policy

- Microsoft .NET support policy: https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core
- .NET 10 download/release information: https://dotnet.microsoft.com/en-us/download/dotnet/10.0

Used to confirm .NET 10 LTS support status and current servicing expectations.

### .NET 10 and C# 14

- .NET 10 overview: https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-10/overview
- C# 14 overview: https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-14

Used for the technical baseline. Feature adoption still requires repository value and compatibility evidence.

### SLNX solution format

- `dotnet sln` documentation: https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-sln
- SLNX file format: https://learn.microsoft.com/en-us/visualstudio/extensibility/visualstudio.extensibility/inside-the-sdk/slnx-reference

Used to support the `.slnx` repository decision.

### Central Package Management

- NuGet Central Package Management: https://learn.microsoft.com/en-us/nuget/consume-packages/central-package-management

Used for centralized version governance.

## ASP.NET Core and Blazor

- Blazor overview: https://learn.microsoft.com/en-us/aspnet/core/blazor/
- Blazor render modes: https://learn.microsoft.com/en-us/aspnet/core/blazor/components/render-modes
- ASP.NET Core OpenAPI: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/openapi/overview
- ASP.NET Core Identity: https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity
- SignalR overview: https://learn.microsoft.com/en-us/aspnet/core/signalr/introduction

Used for the Studio, API, identity, and collaboration baseline. Render modes and authentication mechanisms are validated again during bootstrap.

## .NET Aspire

- Aspire overview: https://learn.microsoft.com/en-us/dotnet/aspire/get-started/aspire-overview
- Aspire architecture: https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/app-host-overview

Used to establish AppHost as development orchestration rather than a required production runtime.

## Entity Framework Core 10

- EF Core 10 overview: https://learn.microsoft.com/en-us/ef/core/what-is-new/ef-core-10.0/whatsnew
- EF Core testing strategy: https://learn.microsoft.com/en-us/ef/core/testing/

Used for the persistence baseline and real relational testing requirement.

## Testing

- Microsoft.Testing.Platform overview: https://learn.microsoft.com/en-us/dotnet/core/testing/microsoft-testing-platform-intro
- .NET test platform selection: https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-platform-intro
- Playwright for .NET: https://playwright.dev/dotnet/

The selected repository test platform remains subject to a bootstrap compatibility proof.

## Serialization and schema

- System.Text.Json JSON Schema export: https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/extract-schema

Project Builder may use generated schemas where they remain deterministic and compatible with the canonical format contract.

## Observability

- .NET observability with OpenTelemetry: https://learn.microsoft.com/en-us/dotnet/core/diagnostics/observability-with-otel
- OpenTelemetry specification: https://opentelemetry.io/docs/specs/

Used for provider-neutral logs, metrics, and traces.

## PostgreSQL

- PostgreSQL JSON types: https://www.postgresql.org/docs/current/datatype-json.html
- PostgreSQL concurrency control: https://www.postgresql.org/docs/current/mvcc.html
- PostgreSQL backup and restore: https://www.postgresql.org/docs/current/backup.html

Used for JSONB, concurrency, and recovery planning. The schema intentionally keeps identities, relations, ownership, and indexed fields normalized.

## Accessibility

- Web Content Accessibility Guidelines 2.2: https://www.w3.org/TR/WCAG22/
- WAI-ARIA Authoring Practices: https://www.w3.org/WAI/ARIA/apg/

Used for the WCAG 2.2 AA product target, keyboard behavior, focus, tree, dialog, and other interaction semantics. Automated checks do not replace manual assistive-technology testing.

## Application security

- OWASP Application Security Verification Standard 5.0: https://owasp.org/www-project-application-security-verification-standard/
- OWASP threat modeling guidance: https://owasp.org/www-community/Threat_Modeling

Used to structure the security requirements and verification mapping.

## Architecture and modeling references

Project Builder is influenced by established ideas including:

- domain-driven design,
- behavior-driven development,
- test-driven development,
- specification by example,
- event storming,
- C4 model,
- statecharts,
- algebraic data types,
- functional core/imperative shell,
- property-based testing,
- ports and adapters,
- clean architecture,
- human-centered design.

The product does not claim strict compatibility with every notation. It uses a typed canonical model and projects selected views where they help answer a question.
