using ProjectBuilder.Domain.Modeling.Primitives;

namespace ProjectBuilder.Domain.Projects;

public sealed record ProjectPurpose
{
    public const int MaxLength = Description.MaxLength;

    private ProjectPurpose(string value) => Value = value;

    public string Value { get; }

    public static SemanticResult<ProjectPurpose> Create(string? value) =>
        ProjectStatement.Create(value, "purpose", text => new ProjectPurpose(text));

    public override string ToString() => Value;
}
