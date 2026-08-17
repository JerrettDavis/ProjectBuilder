namespace ProjectBuilder.Domain.Modeling.Primitives;

internal static class UnicodeLength
{
    internal static int CountCodePoints(string value)
    {
        var count = 0;
        foreach (var _ in value.EnumerateRunes())
        {
            count++;
        }

        return count;
    }
}
