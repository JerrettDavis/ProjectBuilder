using System.Collections.Immutable;
using ProjectBuilder.Domain.Modeling.Primitives;

namespace ProjectBuilder.Domain.Modeling.Elements;

public enum StateCategory { Domain, ApplicationWorkflow, Presentation, Infrastructure, ExternalObserved }
public enum FactMutability { Immutable, Transitioned, Derived, Observed }
public enum RuleKind { Validation, Eligibility, Decision, Derivation, Calculation, Policy }
public enum SemanticResultKind { Success, Invalid, Denied, Conflict, Unavailable, Partial, Cancelled, TimedOut, Failed, Duplicate }

public sealed record LogicStatement
{
    public const int MaxLength = Description.MaxLength;
    private LogicStatement(string value) => Value = value;
    public string Value { get; }
    public static SemanticResult<LogicStatement> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return SemanticResult.Reject<LogicStatement>("logic.statement.required", "A state or logic statement is required.");
        var normalized = value.Trim();
        return normalized.EnumerateRunes().Count() > MaxLength
            ? SemanticResult.Reject<LogicStatement>("logic.statement.too_long", $"A state or logic statement cannot exceed {MaxLength} Unicode code points.")
            : SemanticResult.Accept(new LogicStatement(normalized));
    }
}

public sealed record LogicTerm
{
    public const int MaxLength = 500;
    private LogicTerm(string value) => Value = value;
    public string Value { get; }
    public static SemanticResult<LogicTerm> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return SemanticResult.Reject<LogicTerm>("logic.term.required", "A state or logic term cannot be blank.");
        var normalized = value.Trim();
        return normalized.EnumerateRunes().Count() > MaxLength
            ? SemanticResult.Reject<LogicTerm>("logic.term.too_long", $"A state or logic term cannot exceed {MaxLength} Unicode code points.")
            : SemanticResult.Accept(new LogicTerm(normalized));
    }
}

public abstract record StateLogicElement : ModelElement
{
    protected StateLogicElement(ElementId id, ProjectId projectId, ElementId? parentId, ElementName name,
        LogicStatement description, int order, UtcTimestamp createdAt, string createdBy)
        : base(id, projectId, parentId, name, AcceptedDescription(description), DefinitionStatus.Defined,
            KnowledgeStatus.Known, order, createdAt, createdBy, 1)
    { }
    private static Description AcceptedDescription(LogicStatement value) =>
        ((SemanticResult<Description>.Accepted)Description.Create(value.Value)).Value;
}

public sealed record StateDefinition : StateLogicElement
{
    public StateDefinition(ElementId id, ProjectId projectId, ElementName name, StateCategory category,
        ImmutableArray<LogicTerm> structure, ImmutableArray<LogicTerm> values, ElementId ownerId,
        int order, UtcTimestamp createdAt, string createdBy)
        : base(id, projectId, null, name, AcceptedStatement(name.Value), order, createdAt, createdBy)
    {
        if (structure.IsDefaultOrEmpty) throw new ArgumentException("State structure is required.", nameof(structure));
        Category = category; Structure = structure; Values = values; OwnerId = ownerId;
    }
    public StateCategory Category { get; }
    public ImmutableArray<LogicTerm> Structure { get; }
    public ImmutableArray<LogicTerm> Values { get; }
    public ElementId OwnerId { get; }
    private static LogicStatement AcceptedStatement(string value) => ((SemanticResult<LogicStatement>.Accepted)LogicStatement.Create(value)).Value;
}

public sealed record FactDefinition : StateLogicElement
{
    public FactDefinition(ElementId id, ProjectId projectId, ElementId stateDefinitionId, ElementName name,
        LogicTerm valueType, LogicStatement authority, FactMutability mutability,
        ImmutableArray<KnowledgeStatus> allowedKnowledge, int order, UtcTimestamp createdAt, string createdBy)
        : base(id, projectId, stateDefinitionId, name, authority, order, createdAt, createdBy)
    { ValueType = valueType; Authority = authority; Mutability = mutability; AllowedKnowledge = allowedKnowledge; }
    public LogicTerm ValueType { get; }
    public LogicStatement Authority { get; }
    public FactMutability Mutability { get; }
    public ImmutableArray<KnowledgeStatus> AllowedKnowledge { get; }
}

public sealed record RuleDefinition : StateLogicElement
{
    public RuleDefinition(ElementId id, ProjectId projectId, ElementId stateDefinitionId, ElementName name,
        RuleKind kind, LogicStatement statement, ElementId authorityOwnerId, int order,
        UtcTimestamp createdAt, string createdBy)
        : base(id, projectId, stateDefinitionId, name, statement, order, createdAt, createdBy)
    { Kind = kind; Statement = statement; AuthorityOwnerId = authorityOwnerId; }
    public RuleKind Kind { get; }
    public LogicStatement Statement { get; }
    public ElementId AuthorityOwnerId { get; }
}

public sealed record InvariantDefinition : StateLogicElement
{
    public InvariantDefinition(ElementId id, ProjectId projectId, ElementId stateDefinitionId, ElementName name,
        LogicStatement statement, ImmutableArray<ElementId> scopeIds, LogicStatement falsifyingExample,
        ImmutableArray<LogicTerm> proofExpectation, int order, UtcTimestamp createdAt, string createdBy)
        : base(id, projectId, stateDefinitionId, name, statement, order, createdAt, createdBy)
    {
        if (scopeIds.IsDefaultOrEmpty) throw new ArgumentException("Invariant scope is required.", nameof(scopeIds));
        if (proofExpectation.IsDefaultOrEmpty) throw new ArgumentException("Invariant proof expectation is required.", nameof(proofExpectation));
        Statement = statement; ScopeIds = scopeIds; FalsifyingExample = falsifyingExample; ProofExpectation = proofExpectation;
    }
    public LogicStatement Statement { get; }
    public ImmutableArray<ElementId> ScopeIds { get; }
    public LogicStatement FalsifyingExample { get; }
    public ImmutableArray<LogicTerm> ProofExpectation { get; }
}

public sealed record SemanticResultDefinition : StateLogicElement
{
    public SemanticResultDefinition(ElementId id, ProjectId projectId, ElementId stateDefinitionId,
        ElementName name, SemanticResultKind resultKind, LogicStatement meaning, int order,
        UtcTimestamp createdAt, string createdBy)
        : base(id, projectId, stateDefinitionId, name, meaning, order, createdAt, createdBy)
    { ResultKind = resultKind; Meaning = meaning; }
    public SemanticResultKind ResultKind { get; }
    public LogicStatement Meaning { get; }
}

public sealed record TransitionDefinition : StateLogicElement
{
    public TransitionDefinition(ElementId id, ProjectId projectId, ElementId stateDefinitionId,
        ElementName name, LogicStatement sourcePredicate, LogicStatement trigger,
        LogicStatement targetPredicate, ImmutableArray<ElementId> changedFactIds,
        ImmutableArray<ElementId> ruleIds, ImmutableArray<ElementId> invariantIds,
        ImmutableArray<ElementId> resultIds, int order, UtcTimestamp createdAt, string createdBy)
        : base(id, projectId, stateDefinitionId, name, targetPredicate, order, createdAt, createdBy)
    {
        SourcePredicate = sourcePredicate; Trigger = trigger; TargetPredicate = targetPredicate;
        ChangedFactIds = changedFactIds; RuleIds = ruleIds; InvariantIds = invariantIds; ResultIds = resultIds;
    }
    public LogicStatement SourcePredicate { get; }
    public LogicStatement Trigger { get; }
    public LogicStatement TargetPredicate { get; }
    public ImmutableArray<ElementId> ChangedFactIds { get; }
    public ImmutableArray<ElementId> RuleIds { get; }
    public ImmutableArray<ElementId> InvariantIds { get; }
    public ImmutableArray<ElementId> ResultIds { get; }
}
