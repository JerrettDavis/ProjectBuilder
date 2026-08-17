using System.Collections.Immutable;
using ProjectBuilder.Domain.Modeling.Elements;
using ProjectBuilder.Domain.Modeling.Primitives;

namespace ProjectBuilder.Application.Modeling;

internal static class ModelInputValidation
{
    internal const int MaxStatements = 100;

    internal static ImmutableArray<ActorStatement> ActorStatements(
        string? input,
        List<SemanticError> errors,
        string field)
    {
        var values = Lines(input);
        if (values.Length > MaxStatements)
        {
            errors.Add(new SemanticError($"actor.{field}.too_many", $"Actor {field} cannot contain more than {MaxStatements} entries."));
            return [];
        }

        var builder = ImmutableArray.CreateBuilder<ActorStatement>(values.Length);
        foreach (var value in values)
        {
            Accept(ActorStatement.Create(value), errors, builder);
        }

        return builder.ToImmutable();
    }

    internal static ImmutableArray<SuccessSignal> SuccessSignals(string? input, List<SemanticError> errors)
    {
        var values = Lines(input);
        if (values.Length == 0)
        {
            errors.Add(new SemanticError("outcome.success_signal.required", "At least one observable success signal is required."));
            return [];
        }

        if (values.Length > MaxStatements)
        {
            errors.Add(new SemanticError("outcome.success_signal.too_many", $"An outcome cannot contain more than {MaxStatements} success signals."));
            return [];
        }

        var builder = ImmutableArray.CreateBuilder<SuccessSignal>(values.Length);
        foreach (var value in values)
        {
            Accept(SuccessSignal.Create(value), errors, builder);
        }

        return builder.ToImmutable();
    }

    internal static ImmutableArray<NarrativeFact> NarrativeFacts(
        string? input, List<SemanticError> errors, string field, bool required = true)
    {
        var values = Lines(input);
        if (required && values.Length == 0)
        {
            errors.Add(new SemanticError($"narrative.{field}.required", $"At least one {field.Replace('_', ' ')} is required."));
            return [];
        }
        if (values.Length > MaxStatements)
        {
            errors.Add(new SemanticError($"narrative.{field}.too_many", $"Narrative {field.Replace('_', ' ')} cannot exceed {MaxStatements} entries."));
            return [];
        }
        var builder = ImmutableArray.CreateBuilder<NarrativeFact>(values.Length);
        foreach (var value in values) Accept(NarrativeFact.Create(value), errors, builder);
        return builder.ToImmutable();
    }

    internal static T? Accept<T>(SemanticResult<T> result, List<SemanticError> errors)
        where T : class
    {
        if (result is SemanticResult<T>.Accepted accepted)
        {
            return accepted.Value;
        }

        errors.Add(((SemanticResult<T>.Rejected)result).Error);
        return null;
    }

    private static void Accept<T>(
        SemanticResult<T> result,
        List<SemanticError> errors,
        ImmutableArray<T>.Builder builder)
        where T : class
    {
        var accepted = Accept(result, errors);
        if (accepted is not null)
        {
            builder.Add(accepted);
        }
    }

    private static string[] Lines(string? input) =>
        (input ?? string.Empty)
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
