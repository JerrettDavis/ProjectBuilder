namespace ProjectBuilder.Domain.Modeling.Primitives;

public sealed record Description
{
    public const int MaxLength = 20_000;

    private Description(string value) => Value = value;

    public static Description Empty { get; } = new(string.Empty);

    public string Value { get; }

    public static SemanticResult<Description> Create(string? value)
    {
        if (value is null)
        {
            return SemanticResult.Reject<Description>(
                "description.required",
                "A description value is required; use an empty string when no description is supplied.");
        }

        if (UnicodeLength.CountCodePoints(value) > MaxLength)
        {
            return SemanticResult.Reject<Description>(
                "description.too_long",
                $"A description cannot exceed {MaxLength} Unicode code points.");
        }

        return SemanticResult.Accept(new Description(value));
    }

    public override string ToString() => Value;
}
