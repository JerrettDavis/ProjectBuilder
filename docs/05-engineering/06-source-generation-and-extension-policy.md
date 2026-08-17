# Source Generation and Extension Policy

## Purpose

Project Builder will eventually generate registries, schemas, tests, and application scaffolding. Generation should reduce repetition while preserving understandable code and explicit architecture.

## Generator rules

1. Incremental Roslyn generators where compile-time integration is required.
2. Standalone CLI generators for repository or cross-language artifacts.
3. No network access during compilation.
4. No hidden machine-global configuration.
5. AdditionalFiles and MSBuild properties are explicit inputs.
6. Deterministic output.
7. stable ordering.
8. readable idiomatic C#.
9. diagnostics at source locations.
10. no runtime dependency on generator assemblies.
11. no reflection requirement introduced merely for generation convenience.
12. sync and async API parity only when domain behavior actually supports both.
13. generated APIs fail fast on invalid registration.

## Generator project isolation

A generator project:

- targets an appropriate analyzer-compatible framework,
- minimizes package dependencies,
- does not reference runtime projects when metadata parsing can avoid it,
- parses attribute constants without loading target assemblies,
- packages as analyzer assets,
- has dedicated snapshot, compile, and diagnostic tests.

Consumers reference abstractions normally and the generator as analyzer.

## Candidate generators

### Meta-model registry generator
Input:

```csharp
[ModelElementKind("scenario")]
public sealed partial record ScenarioElement;
```

Output:

- descriptor registration,
- exhaustive kind mapping,
- serializer metadata,
- visitor,
- diagnostics for duplicate keys.

### Relation builder generator
Produces typed, discoverable relation APIs:

```csharp
model.Relate(actor)
    .Initiates(interaction)
    .Under(condition);
```

Only if the API remains clearer than constructors.

### JSON context generator
Generates `JsonSerializerContext` registrations for project contracts and extension payloads.

### Analyzer catalog generator
Builds stable rule registries from rule definitions.

### Model-reference analyzer
Validates code-to-model identifier annotations or manifests.

### Projection scaffold generator
Produces explicit C# source from a selected Project Builder baseline. This is normally a CLI or background projection, not a compiler source generator.

## Generated API principles

Generated APIs should:

- expose the modeled vocabulary,
- use refined values,
- preserve required ordering,
- support immutable composition,
- report missing requirements,
- avoid ambiguous overloads,
- make strategy selection explicit,
- preserve cancellation and async behavior,
- not require consumers to understand generator internals.

## Builder paradigms

### Mutable instance builder
Appropriate for staged construction with validation at `Build`.

```text
New
→ add steps
→ configure policies
→ validate requirements
→ Build
```

### State projection builder
Appropriate for immutable state transformations:

```text
New state
→ transform
→ validate
→ project
```

Select based on lifecycle and invariants, not preference.

## Extension types

### Data-only extension
Adds namespaced element or relation descriptors, prompts, and schemas. Safest and preferred.

### Projection extension
Consumes an immutable snapshot and produces artifacts out of process.

### Integration extension
Connects an external system through declared capabilities and credentials.

### UI extension
Adds inspector or lens behavior. Requires a stronger trust and compatibility model.

### Runtime semantic extension
Adds validators or model operation behavior. Highest risk and not open to untrusted packages in MVP.

## Extension manifest

```json
{
  "id": "com.example.domain-pack",
  "version": "1.2.0",
  "projectBuilderRange": ">=1.0 <2.0",
  "capabilities": [
    "element-descriptors",
    "guidance-rules",
    "projection"
  ],
  "permissions": [
    "read:model-snapshot",
    "write:artifact"
  ],
  "schemas": [],
  "entrypoints": [],
  "publisher": {},
  "signature": {}
}
```

## Trust levels

- Core.
- Workspace-approved.
- Signed third party.
- Local development.
- Quarantined or unknown.

Trust controls execution and data access.

## Plugin execution

Executable plugins:

- run out of process,
- receive a scoped immutable snapshot,
- use explicit RPC,
- have CPU, memory, time, and output limits,
- cannot access database or secrets directly,
- cannot commit model changes,
- return proposed change sets or artifacts,
- emit telemetry and audit.

## Schema extensions

Namespaced keys:

```text
com.example.retail.coupon-campaign
```

Extension schema defines:

- fields,
- knowledge semantics,
- relation permissions,
- validation,
- display fallback,
- migration.

Core format preserves unknown safe payload.

## Compatibility

Extensions declare:

- Project Builder range,
- model-format range,
- schema versions,
- migration path,
- generator version,
- dependencies.

The application blocks incompatible execution but preserves content read-only where safe.

## Licensing

Every dependency, template, extension, and generated scaffold records license metadata. Do not embed code with incompatible or unclear licensing.

## Removal

Removing an extension:

1. inventory dependent model elements,
2. export or migrate content,
3. disable execution,
4. preserve opaque payload if safe,
5. remove credentials,
6. audit action,
7. retain package metadata needed to understand history.

No uninstall silently deletes model content.
