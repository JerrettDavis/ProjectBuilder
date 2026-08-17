# Engineering Standards

## Core stance

Project Builder code should read like the domain and expose its effects. The default is explicit, declarative, functional where practical, and vertically organized. Abstractions are earned by repeated stable behavior, not added to make the repository look architectural.

## Language and platform

- .NET 10.
- C# 14.
- Nullable reference types enabled.
- Implicit usings allowed but reviewed for domain clarity.
- Async all the way at I/O boundaries.
- `ValueTask` only where profiling or interface shape justifies it.
- Cancellation tokens propagated through asynchronous boundaries.
- Invariant culture for machine formats.
- UTC or explicit time-zone semantics.
- Precise decimals and currency types for financial values.
- `.slnx` solution.

## Domain types

Prefer refined types:

```csharp
public readonly record struct ProjectId(Guid Value)
{
    public static ProjectId New(TimeProvider timeProvider)
        => new(Guid.CreateVersion7(timeProvider.GetUtcNow()));
}

public sealed record ElementName
{
    public const int MaxLength = 200;
    public string Value { get; }

    private ElementName(string value) => Value = value;

    public static ParseResult<ElementName> Parse(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? ParseResult<ElementName>.Invalid("Name is required.")
            : value.Trim().Length > MaxLength
                ? ParseResult<ElementName>.Invalid($"Name is limited to {MaxLength} characters.")
                : ParseResult<ElementName>.Valid(new(value.Trim()));
}
```

Do not wrap every primitive. Create a domain type when it:

- enforces validity,
- carries meaning,
- participates in rules,
- prevents unit or identifier confusion,
- needs stable serialization.

## Immutability

- Domain records and collections are immutable by default.
- Transitions return new state and explicit results.
- Mutable builders are allowed at parsing, persistence, UI draft, and generation boundaries.
- Mutable domain objects require a documented invariant strategy.

## Results

Expected semantic outcomes use closed result types, not exceptions or boolean-plus-message tuples.

Exceptions represent:

- programming defects,
- violated internal assumptions,
- unexpected infrastructure failure at a boundary,
- cancellation.

Map exceptions once at presentation and operations boundaries.

## Pure rules

Rules should be deterministic when supplied the same facts.

Bad:

```csharp
public async Task<bool> IsCouponValidAsync(Coupon coupon)
{
    var campaign = await _client.GetCampaignAsync(coupon.Code);
    return campaign.ExpiresAt > DateTime.UtcNow;
}
```

Better:

```csharp
public static CouponEligibility Evaluate(
    Coupon coupon,
    CampaignFacts campaign,
    Instant evaluatedAt)
    => ...;
```

The application obtains `CampaignFacts` and `evaluatedAt`.

## Effects

Make effects explicit through ports or effect values. Do not hide network or persistence work in property getters, constructors, implicit conversion, or domain event handlers.

## Composition

Prefer small named functions and explicit pipelines:

```csharp
var result =
    CaptureScan.Parse(request)
        .Bind(ClassifyCapturedValue)
        .Bind(ResolveProductFacts)
        .Bind(transaction.AddProduct)
        .Map(ToResponse);
```

Use fluent composition only when it improves the domain narrative. Avoid chains that obscure async, branching, or side effects.

PatternKit-style Decorator, Composer or Builder, Proxy, and Facade vocabulary can be used when the actual GoF responsibility exists. Name the domain policy before the pattern.

## Application handlers

A handler should reveal:

1. authorization,
2. state loaded,
3. external facts obtained,
4. domain transition,
5. persistence,
6. effects or events,
7. semantic result.

Cross-cutting decorators must not make this sequence unknowable.

## Dependency injection

- Constructor injection.
- No service locator.
- No static mutable service access.
- Register by module through explicit extension methods.
- Validate DI at startup in development and CI.
- Keep service lifetimes deliberate.
- Avoid injecting `IServiceProvider` except composition and approved factories.

## Mapping

Write explicit mappings at meaningful boundaries. Avoid automatic mapping for domain transitions and contracts where missing or renamed fields matter.

Generated mappings are acceptable when:

- source and target contracts are explicit,
- diagnostics catch unmapped fields,
- generated code is readable,
- behavior is tested.

## Persistence

- No lazy loading.
- No domain logic in EF entities if separate persistence models are used.
- No generic repository abstraction.
- Use repository or store interfaces named for application needs.
- Queries return read models.
- Transactions are explicit.
- Migrations are reviewed.
- N+1 and unbounded query patterns are tested.

## Serialization

- `System.Text.Json`.
- source-generated serialization metadata for stable contracts and performance-sensitive paths.
- reject or handle unknown core properties according to contract version.
- explicit polymorphism.
- no type-name-based arbitrary instantiation.
- deterministic options for project exports.

## APIs

- Minimal APIs organized by feature.
- typed request and response contracts.
- Problem Details with stable codes.
- exhaustive semantic result mapping.
- OpenAPI.
- ETags and expected revision.
- idempotency for relevant writes.
- no raw exception output.
- no direct EF entity exposure.

## Client state

Studio state categories:

- server model cache,
- uncommitted draft operations,
- selection and navigation,
- view layout,
- command history,
- transient component state.

Use explicit immutable state transitions. Do not place all client state in one global bag or spread project revision state across components.

## Browser interop

JavaScript is permitted for browser-native concerns:

- pointer capture and coalescing,
- resize and intersection observation,
- clipboard,
- file system and download APIs,
- text measurement,
- high-frequency rendering adapter if needed.

Interop modules are small, typed, tested, and contain no domain rules.

## Naming

Names express domain behavior:

- `CommitProjectChangeSet`.
- `ResolveStorePrice`.
- `EvidenceBecameStale`.

Avoid:

- `ProjectService`.
- `DataManager`.
- `CommonHelper`.
- `ProcessAsync`.
- `HandleStuff`.
- `BaseRepository`.

## Comments

Comments explain:

- why a constraint exists,
- why an alternative was rejected,
- surprising provider behavior,
- proof or source,
- compatibility or migration issue.

Do not narrate obvious syntax.

## Error messages

Messages state:

- what could not be done,
- domain reason where safe,
- state that remains,
- next action,
- reference or correlation where needed.

Messages never expose secrets, stack traces, SQL, or provider credentials.

## Logging

- structured, not interpolated blobs,
- stable event names,
- no model prose or sensitive payload by default,
- trace and correlation,
- semantic result category,
- log once at the owning boundary.

## Analyzers and warnings

- warnings as errors in CI,
- latest recommended analysis level,
- security analyzers,
- architecture tests,
- custom analyzers only for stable, high-value rules,
- documented suppressions with rationale.

## Performance

- measure before optimization,
- define budget and representative data,
- optimize the actual bottleneck,
- preserve semantic tests,
- record tradeoff as ADR if architecture changes.

## Definition of clean code

Clean code in this project:

- makes domain meaning visible,
- makes invalid state difficult to represent,
- makes effects explicit,
- keeps boundaries directional,
- supports deterministic proof,
- changes at the rate of the concept it models,
- contains no abstraction whose purpose cannot be explained with a real scenario.
