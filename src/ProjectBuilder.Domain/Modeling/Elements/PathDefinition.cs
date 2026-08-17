using System.Collections.Immutable;
using ProjectBuilder.Domain.Modeling.Primitives;

namespace ProjectBuilder.Domain.Modeling.Elements;

public enum PathClassification { Happy, Alternate, Exceptional, Degraded, Recovery, Cancellation, Compensation }
public enum ConditionKind { Entry, Guard, Branch, Exit, Cancellation }
public enum EffectKind { Observation, ExternalInteraction, DomainMutation, Audit, Notification, ManualIntervention }
public enum RecoveryStrategy { CorrectAndRetry, Retry, Resume, Reconcile, Escalate, Compensate, SafeStop }

public abstract record PathElement : ModelElement
{
    protected PathElement(ElementId id, ProjectId projectId, ElementId parentId, ElementName name,
        LogicStatement description, int order, UtcTimestamp createdAt, string createdBy)
        : base(id, projectId, parentId, name, AcceptedDescription(description), DefinitionStatus.Defined,
            KnowledgeStatus.Known, order, createdAt, createdBy, 1)
    {
    }

    private static Description AcceptedDescription(LogicStatement value) =>
        ((SemanticResult<Description>.Accepted)Description.Create(value.Value)).Value;
}

public sealed record ConditionDefinition : PathElement
{
    public ConditionDefinition(ElementId id, ProjectId projectId, ElementId pathId, ElementName name,
        ConditionKind kind, LogicStatement statement, ImmutableArray<ElementId> factIds,
        ImmutableArray<ElementId> ruleIds, int order, UtcTimestamp createdAt, string createdBy)
        : base(id, projectId, pathId, name, statement, order, createdAt, createdBy)
    {
        Kind = kind;
        Statement = statement;
        FactIds = factIds;
        RuleIds = ruleIds;
    }

    public ConditionKind Kind { get; }
    public LogicStatement Statement { get; }
    public ImmutableArray<ElementId> FactIds { get; }
    public ImmutableArray<ElementId> RuleIds { get; }
}

public sealed record EffectDefinition : PathElement
{
    public EffectDefinition(ElementId id, ProjectId projectId, ElementId pathId, ElementName name,
        EffectKind kind, LogicStatement statement, ElementId? failurePathId,
        int order, UtcTimestamp createdAt, string createdBy)
        : base(id, projectId, pathId, name, statement, order, createdAt, createdBy)
    {
        Kind = kind;
        Statement = statement;
        FailurePathId = failurePathId;
    }

    public EffectKind Kind { get; }
    public LogicStatement Statement { get; }
    public ElementId? FailurePathId { get; }
}

public sealed record PathDefinition : PathElement
{
    public PathDefinition(
        ElementId id, ProjectId projectId, ElementId scenarioId, ElementName name,
        PathClassification classification, ElementId sourceTransitionId,
        ImmutableArray<ElementId> conditionIds, ImmutableArray<LogicTerm> segments,
        ElementId? terminalResultId, ElementId? targetTransitionId,
        LogicStatement terminalState, LogicStatement observation, ElementId ownerId,
        ElementId? recoveryPathId, ElementId? recoversFromPathId, RecoveryStrategy? recoveryStrategy,
        LogicStatement? retryPolicy, LogicStatement? idempotencyAnalysis,
        LogicStatement? exitCondition, LogicStatement? reconciliation,
        int order, UtcTimestamp createdAt, string createdBy)
        : base(id, projectId, scenarioId, name, observation, order, createdAt, createdBy)
    {
        if (conditionIds.IsDefaultOrEmpty)
            throw new ArgumentException("A path requires an explicit entry or branch condition.", nameof(conditionIds));
        if (segments.IsDefaultOrEmpty)
            throw new ArgumentException("A path requires at least one ordered segment.", nameof(segments));

        Classification = classification;
        SourceTransitionId = sourceTransitionId;
        ConditionIds = conditionIds;
        Segments = segments;
        TerminalResultId = terminalResultId;
        TargetTransitionId = targetTransitionId;
        TerminalState = terminalState;
        Observation = observation;
        OwnerId = ownerId;
        RecoveryPathId = recoveryPathId;
        RecoversFromPathId = recoversFromPathId;
        RecoveryStrategy = recoveryStrategy;
        RetryPolicy = retryPolicy;
        IdempotencyAnalysis = idempotencyAnalysis;
        ExitCondition = exitCondition;
        Reconciliation = reconciliation;
    }

    public PathClassification Classification { get; }
    public ElementId SourceTransitionId { get; }
    public ImmutableArray<ElementId> ConditionIds { get; }
    public ImmutableArray<LogicTerm> Segments { get; }
    public ElementId? TerminalResultId { get; }
    public ElementId? TargetTransitionId { get; }
    public LogicStatement TerminalState { get; }
    public LogicStatement Observation { get; }
    public ElementId OwnerId { get; }
    public ElementId? RecoveryPathId { get; }
    public ElementId? RecoversFromPathId { get; }
    public RecoveryStrategy? RecoveryStrategy { get; }
    public LogicStatement? RetryPolicy { get; }
    public LogicStatement? IdempotencyAnalysis { get; }
    public LogicStatement? ExitCondition { get; }
    public LogicStatement? Reconciliation { get; }
}
