using ProjectBuilder.Domain.Modeling.Primitives;

namespace ProjectBuilder.Domain.Projects;

internal static class ProjectStatement
{
    internal static SemanticResult<T> Create<T>(string? value, string field, Func<string, T> factory)
        where T : notnull
    {
        if (value is null || value.Trim().Length == 0)
        {
            return SemanticResult.Reject<T>($"project.{field}.required", $"Project {field.Replace('_', ' ')} is required.");
        }

        var normalized = value.Trim();
        if (normalized.EnumerateRunes().Count() > Description.MaxLength)
        {
            return SemanticResult.Reject<T>(
                $"project.{field}.too_long",
                $"Project {field.Replace('_', ' ')} cannot exceed {Description.MaxLength} Unicode code points.");
        }

        return SemanticResult.Accept(factory(normalized));
    }
}
