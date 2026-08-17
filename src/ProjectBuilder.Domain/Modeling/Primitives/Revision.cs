using System.Globalization;

namespace ProjectBuilder.Domain.Modeling.Primitives;

public sealed record Revision
{
    private Revision(long value) => Value = value;

    public static Revision Initial { get; } = new(1);

    public long Value { get; }

    public static SemanticResult<Revision> Create(long value) =>
        value < 1
            ? SemanticResult.Reject<Revision>("revision.out_of_range", "A revision must be at least one.")
            : SemanticResult.Accept(new Revision(value));

    public static SemanticResult<Revision> Parse(string? value) =>
        long.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var parsed)
            ? Create(parsed)
            : SemanticResult.Reject<Revision>(
                "revision.format",
                "A revision must be a positive base-10 integer.");

    public SemanticResult<Revision> Next() =>
        Value == long.MaxValue
            ? SemanticResult.Reject<Revision>("revision.exhausted", "The revision cannot be advanced beyond its supported range.")
            : SemanticResult.Accept(new Revision(Value + 1));

    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
}
