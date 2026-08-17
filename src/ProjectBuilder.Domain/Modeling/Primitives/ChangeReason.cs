namespace ProjectBuilder.Domain.Modeling.Primitives;

public sealed record ChangeReason
{
    public const int MinLength = 3;
    public const int MaxLength = 2_000;

    private ChangeReason(string value) => Value = value;

    public string Value { get; }

    public static SemanticResult<ChangeReason> Create(string? value)
    {
        if (value is null)
        {
            return SemanticResult.Reject<ChangeReason>("reason.required", "A change reason is required.");
        }

        var normalized = value.Trim();
        var length = UnicodeLength.CountCodePoints(normalized);
        if (length < MinLength)
        {
            return SemanticResult.Reject<ChangeReason>(
                "reason.too_short",
                $"A change reason must contain at least {MinLength} Unicode code points.");
        }

        if (length > MaxLength)
        {
            return SemanticResult.Reject<ChangeReason>(
                "reason.too_long",
                $"A change reason cannot exceed {MaxLength} Unicode code points.");
        }

        return SemanticResult.Accept(new ChangeReason(normalized));
    }

    public override string ToString() => Value;
}
