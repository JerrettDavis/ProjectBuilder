using System.Globalization;

namespace ProjectBuilder.Domain.Modeling.Primitives;

public sealed record ProjectId
{
    private ProjectId(Guid value) => Value = value;

    public Guid Value { get; }

    public static SemanticResult<ProjectId> Create(Guid value) =>
        value == Guid.Empty
            ? SemanticResult.Reject<ProjectId>("identity.empty", "A project identifier cannot be empty.")
            : SemanticResult.Accept(new ProjectId(value));

    public static SemanticResult<ProjectId> Parse(string? value) =>
        IdentifierParser.Parse(value, Create, "project");

    public override string ToString() => Value.ToString("D", CultureInfo.InvariantCulture);
}

public sealed record WorkspaceId
{
    private WorkspaceId(Guid value) => Value = value;

    public Guid Value { get; }

    public static SemanticResult<WorkspaceId> Create(Guid value) =>
        value == Guid.Empty
            ? SemanticResult.Reject<WorkspaceId>("identity.empty", "A workspace identifier cannot be empty.")
            : SemanticResult.Accept(new WorkspaceId(value));

    public static SemanticResult<WorkspaceId> Parse(string? value) =>
        IdentifierParser.Parse(value, Create, "workspace");

    public override string ToString() => Value.ToString("D", CultureInfo.InvariantCulture);
}

public sealed record ElementId
{
    private ElementId(Guid value) => Value = value;

    public Guid Value { get; }

    public static SemanticResult<ElementId> Create(Guid value) =>
        value == Guid.Empty
            ? SemanticResult.Reject<ElementId>("identity.empty", "An element identifier cannot be empty.")
            : SemanticResult.Accept(new ElementId(value));

    public static SemanticResult<ElementId> Parse(string? value) =>
        IdentifierParser.Parse(value, Create, "element");

    public override string ToString() => Value.ToString("D", CultureInfo.InvariantCulture);
}

public sealed record RelationId
{
    private RelationId(Guid value) => Value = value;

    public Guid Value { get; }

    public static SemanticResult<RelationId> Create(Guid value) =>
        value == Guid.Empty
            ? SemanticResult.Reject<RelationId>("identity.empty", "A relation identifier cannot be empty.")
            : SemanticResult.Accept(new RelationId(value));

    public static SemanticResult<RelationId> Parse(string? value) =>
        IdentifierParser.Parse(value, Create, "relation");

    public override string ToString() => Value.ToString("D", CultureInfo.InvariantCulture);
}

public sealed record ChangeSetId
{
    private ChangeSetId(Guid value) => Value = value;

    public Guid Value { get; }

    public static SemanticResult<ChangeSetId> Create(Guid value) =>
        value == Guid.Empty
            ? SemanticResult.Reject<ChangeSetId>("identity.empty", "A change-set identifier cannot be empty.")
            : SemanticResult.Accept(new ChangeSetId(value));

    public static SemanticResult<ChangeSetId> Parse(string? value) =>
        IdentifierParser.Parse(value, Create, "change-set");

    public override string ToString() => Value.ToString("D", CultureInfo.InvariantCulture);
}

internal static class IdentifierParser
{
    internal static SemanticResult<T> Parse<T>(
        string? value,
        Func<Guid, SemanticResult<T>> create,
        string identityKind)
        where T : notnull
    {
        if (!Guid.TryParseExact(value, "D", out var parsed))
        {
            return SemanticResult.Reject<T>(
                "identity.format",
                $"The {identityKind} identifier must use the canonical GUID 'D' format.");
        }

        return create(parsed);
    }
}
