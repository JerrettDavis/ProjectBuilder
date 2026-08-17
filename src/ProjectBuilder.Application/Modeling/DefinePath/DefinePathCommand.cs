namespace ProjectBuilder.Application.Modeling.DefinePath;

public sealed record DefinePathCommand(
    string ProjectId, string ExpectedRevision, string OperationId,
    string ScenarioId, string SourceTransitionId, string TerminalResultId, string RecoveryResultId,
    string OwnerId, string BranchName, string BranchClassification,
    string BranchConditionName, string BranchConditionKind, string BranchCondition,
    string BranchFactIds, string BranchRuleIds, string BranchSegments,
    string BranchTerminalState, string BranchObservation,
    string EffectName, string EffectKind, string EffectStatement,
    string RecoveryName, string RecoveryStrategy, string RecoveryConditionName, string RecoveryCondition,
    string RecoverySegments, string RecoveryTerminalState, string RecoveryObservation,
    string RetryPolicy, string IdempotencyAnalysis, string ExitCondition, string Reconciliation,
    string Reason);
