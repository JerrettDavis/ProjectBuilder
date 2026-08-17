using System.Collections.Immutable;
using ProjectBuilder.Domain.Modeling.Primitives;

namespace ProjectBuilder.Domain.Modeling.Elements;

public enum ActorKind
{
    HumanRole,
    OrganizationRole,
    SystemRole,
    DeviceRole,
    AutomatedRole,
    ExternalProviderRole,
}

public sealed record ContextualRole
{
    public const int MaxLength = Description.MaxLength;

    private ContextualRole(string value) => Value = value;

    public string Value { get; }

    public static SemanticResult<ContextualRole> Create(string? value)
    {
        if (value is null || value.Trim().Length == 0)
        {
            return SemanticResult.Reject<ContextualRole>("actor.contextual_role.required", "An actor contextual role is required.");
        }

        var normalized = value.Trim();
        return normalized.EnumerateRunes().Count() > MaxLength
            ? SemanticResult.Reject<ContextualRole>("actor.contextual_role.too_long", $"An actor contextual role cannot exceed {MaxLength} Unicode code points.")
            : SemanticResult.Accept(new ContextualRole(normalized));
    }

    public override string ToString() => Value;
}

public sealed record ActorStatement
{
    public const int MaxLength = 500;

    private ActorStatement(string value) => Value = value;

    public string Value { get; }

    public static SemanticResult<ActorStatement> Create(string? value)
    {
        if (value is null || value.Trim().Length == 0)
        {
            return SemanticResult.Reject<ActorStatement>("actor.statement.required", "An actor statement cannot be blank.");
        }

        var normalized = value.Trim();
        return normalized.EnumerateRunes().Count() > MaxLength
            ? SemanticResult.Reject<ActorStatement>("actor.statement.too_long", $"An actor statement cannot exceed {MaxLength} Unicode code points.")
            : SemanticResult.Accept(new ActorStatement(normalized));
    }

    public override string ToString() => Value;
}

public sealed record ActorDefinition : ModelElement
{
    public ActorDefinition(
        ElementId id,
        ProjectId projectId,
        ElementName name,
        ContextualRole contextualRole,
        ActorKind actorKind,
        ImmutableArray<ActorStatement> goals,
        ImmutableArray<ActorStatement> responsibilities,
        ImmutableArray<ActorStatement> authority,
        ImmutableArray<ActorStatement> constraints,
        int order,
        UtcTimestamp createdAt,
        string createdBy,
        KnowledgeStatus knowledgeStatus = KnowledgeStatus.Known)
        : base(
            id,
            projectId,
            null,
            name,
            AcceptedDescription(contextualRole),
            DefinitionStatus.Defined,
            knowledgeStatus,
            order,
            createdAt,
            createdBy,
            1)
    {
        ContextualRole = contextualRole;
        ActorKind = actorKind;
        Goals = goals;
        Responsibilities = responsibilities;
        Authority = authority;
        Constraints = constraints;
    }

    public ContextualRole ContextualRole { get; }
    public ActorKind ActorKind { get; }
    public ImmutableArray<ActorStatement> Goals { get; }
    public ImmutableArray<ActorStatement> Responsibilities { get; }
    public ImmutableArray<ActorStatement> Authority { get; }
    public ImmutableArray<ActorStatement> Constraints { get; }

    private static Description AcceptedDescription(ContextualRole role) =>
        ((SemanticResult<Description>.Accepted)Description.Create(role.Value)).Value;
}
