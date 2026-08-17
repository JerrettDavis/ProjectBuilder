using ProjectBuilder.Domain.Modeling.Primitives;

namespace ProjectBuilder.Domain.Projects;

public sealed record IntendedOutcome
{
    public const int MaxLength = Description.MaxLength;

    private IntendedOutcome(string value) => Value = value;

    public string Value { get; }

    public static SemanticResult<IntendedOutcome> Create(string? value) =>
        ProjectStatement.Create(value, "intended_outcome", text => new IntendedOutcome(text));

    public override string ToString() => Value;
}
