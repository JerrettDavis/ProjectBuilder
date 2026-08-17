namespace ProjectBuilder.Domain.Modeling.Primitives;

public sealed record ElementName
{
    // The versioned project schema currently permits 500 Unicode code points.
    public const int MaxLength = 500;

    private ElementName(string value) => Value = value;

    public string Value { get; }

    public static SemanticResult<ElementName> Create(string? value)
    {
        if (value is null)
        {
            return SemanticResult.Reject<ElementName>("name.required", "A name is required.");
        }

        var normalized = value.Trim();
        if (normalized.Length == 0)
        {
            return SemanticResult.Reject<ElementName>("name.required", "A name is required.");
        }

        if (UnicodeLength.CountCodePoints(normalized) > MaxLength)
        {
            return SemanticResult.Reject<ElementName>(
                "name.too_long",
                $"A name cannot exceed {MaxLength} Unicode code points.");
        }

        return SemanticResult.Accept(new ElementName(normalized));
    }

    public override string ToString() => Value;
}
