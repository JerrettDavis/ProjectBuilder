using System.Globalization;

namespace ProjectBuilder.Domain.Modeling.Primitives;

public sealed record UtcTimestamp
{
    private UtcTimestamp(DateTimeOffset value) => Value = value;

    public DateTimeOffset Value { get; }

    public static UtcTimestamp Create(DateTimeOffset value) => new(value.ToUniversalTime());

    public static SemanticResult<UtcTimestamp> Parse(string? value)
    {
        if (value is null || !HasExplicitOffset(value))
        {
            return InvalidFormat();
        }

        if (!DateTimeOffset.TryParseExact(
                value,
                "O",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var parsed))
        {
            return InvalidFormat();
        }

        return SemanticResult.Accept(Create(parsed));
    }

    public override string ToString() => Value.UtcDateTime.ToString("O", CultureInfo.InvariantCulture);

    private static bool HasExplicitOffset(string value) =>
        value.EndsWith('Z') ||
        (value.Length >= 6 &&
         (value[^6] == '+' || value[^6] == '-') &&
         value[^3] == ':');

    private static SemanticResult<UtcTimestamp> InvalidFormat() =>
        SemanticResult.Reject<UtcTimestamp>(
            "timestamp.format",
            "A timestamp must use the round-trip format with an explicit UTC offset.");
}
