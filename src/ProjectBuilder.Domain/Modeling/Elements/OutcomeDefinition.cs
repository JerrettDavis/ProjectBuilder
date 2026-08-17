using System.Collections.Immutable;
using ProjectBuilder.Domain.Modeling.Primitives;

namespace ProjectBuilder.Domain.Modeling.Elements;

public sealed record OutcomeStatement
{
    public const int MaxLength = Description.MaxLength;

    private OutcomeStatement(string value) => Value = value;

    public string Value { get; }

    public static SemanticResult<OutcomeStatement> Create(string? value)
    {
        if (value is null || value.Trim().Length == 0)
        {
            return SemanticResult.Reject<OutcomeStatement>("outcome.statement.required", "An observable outcome statement is required.");
        }

        var normalized = value.Trim();
        return normalized.EnumerateRunes().Count() > MaxLength
            ? SemanticResult.Reject<OutcomeStatement>("outcome.statement.too_long", $"An outcome statement cannot exceed {MaxLength} Unicode code points.")
            : SemanticResult.Accept(new OutcomeStatement(normalized));
    }

    public override string ToString() => Value;
}

public sealed record SuccessSignal
{
    public const int MaxLength = 500;

    private SuccessSignal(string value) => Value = value;

    public string Value { get; }

    public static SemanticResult<SuccessSignal> Create(string? value)
    {
        if (value is null || value.Trim().Length == 0)
        {
            return SemanticResult.Reject<SuccessSignal>("outcome.success_signal.required", "At least one observable success signal is required.");
        }

        var normalized = value.Trim();
        return normalized.EnumerateRunes().Count() > MaxLength
            ? SemanticResult.Reject<SuccessSignal>("outcome.success_signal.too_long", $"A success signal cannot exceed {MaxLength} Unicode code points.")
            : SemanticResult.Accept(new SuccessSignal(normalized));
    }

    public override string ToString() => Value;
}

public sealed record OutcomeDefinition : ModelElement
{
    public OutcomeDefinition(
        ElementId id,
        ProjectId projectId,
        ElementName name,
        OutcomeStatement statement,
        ImmutableArray<SuccessSignal> successSignals,
        int order,
        UtcTimestamp createdAt,
        string createdBy,
        KnowledgeStatus knowledgeStatus = KnowledgeStatus.Known)
        : base(
            id,
            projectId,
            null,
            name,
            AcceptedDescription(statement),
            DefinitionStatus.Defined,
            knowledgeStatus,
            order,
            createdAt,
            createdBy,
            1)
    {
        if (successSignals.IsDefaultOrEmpty)
        {
            throw new ArgumentException("An outcome requires at least one success signal.", nameof(successSignals));
        }

        Statement = statement;
        SuccessSignals = successSignals;
    }

    public OutcomeStatement Statement { get; }
    public ImmutableArray<SuccessSignal> SuccessSignals { get; }

    private static Description AcceptedDescription(OutcomeStatement statement) =>
        ((SemanticResult<Description>.Accepted)Description.Create(statement.Value)).Value;
}
