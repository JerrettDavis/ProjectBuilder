using System.Collections.Immutable;
using ProjectBuilder.Domain.Modeling.Elements;
using ProjectBuilder.Domain.Modeling.Primitives;
using ProjectBuilder.Domain.Projects;

namespace ProjectBuilder.Domain.Modeling.Transitions;

public sealed record PathIds(
    ElementId BranchPathId, ElementId BranchConditionId, ElementId EffectId,
    ElementId RecoveryPathId, ElementId RecoveryConditionId);

public sealed record PathDraft(
    ElementId ScenarioId, ElementId SourceTransitionId, ElementId TerminalResultId, ElementId RecoveryResultId,
    ElementId OwnerId,
    ElementName BranchName, PathClassification BranchClassification,
    ElementName BranchConditionName, ConditionKind BranchConditionKind, LogicStatement BranchCondition,
    ImmutableArray<ElementId> BranchFactIds, ImmutableArray<ElementId> BranchRuleIds,
    ImmutableArray<LogicTerm> BranchSegments, LogicStatement BranchTerminalState, LogicStatement BranchObservation,
    ElementName EffectName, EffectKind EffectKind, LogicStatement EffectStatement,
    ElementName RecoveryName, RecoveryStrategy RecoveryStrategy,
    ElementName RecoveryConditionName, LogicStatement RecoveryCondition,
    ImmutableArray<LogicTerm> RecoverySegments, LogicStatement RecoveryTerminalState,
    LogicStatement RecoveryObservation, LogicStatement? RetryPolicy, LogicStatement? IdempotencyAnalysis,
    LogicStatement? ExitCondition, LogicStatement? Reconciliation);

public sealed record PathDefinitionSet(
    PathDefinition Branch, ConditionDefinition BranchCondition, EffectDefinition Effect,
    PathDefinition Recovery, ConditionDefinition RecoveryCondition)
{
    public ImmutableArray<ModelElement> Elements => [Branch, BranchCondition, Effect, Recovery, RecoveryCondition];
}

public abstract record DefinePathTransitionResult
{
    private DefinePathTransitionResult() { }
    public sealed record Accepted(ProjectDefinition Project, PathDefinitionSet Definitions, ProjectModelChangeSet ChangeSet)
        : DefinePathTransitionResult;
    public sealed record Conflict(Revision Expected, Revision Actual, ImmutableArray<SemanticConflict> Conflicts) : DefinePathTransitionResult;
    public sealed record Invalid(IReadOnlyList<SemanticError> Errors) : DefinePathTransitionResult;
}

public static class PathValidation
{
    public static IReadOnlyList<SemanticError> Validate(
        IEnumerable<PathDefinition> paths,
        IEnumerable<ConditionDefinition> conditions,
        IEnumerable<EffectDefinition> effects,
        IEnumerable<SemanticResultDefinition> results)
    {
        var errors = new List<SemanticError>();
        var pathArray = paths.ToArray();
        var conditionArray = conditions.ToArray();
        var effectArray = effects.ToArray();
        var resultArray = results.ToArray();

        foreach (var path in pathArray)
        {
            if (path.ConditionIds.Any(id => conditionArray.All(condition => condition.Id != id)) ||
                conditionArray.Any(condition => condition.ParentId == path.Id && !path.ConditionIds.Contains(condition.Id)))
                errors.Add(new("PB-PATH-001", "Every path condition must be explicit and owned by that path."));

            var resultExists = path.TerminalResultId is { } resultId && resultArray.Any(result => result.Id == resultId);
            var recoveryExists = path.RecoveryPathId is { } recoveryId && pathArray.Any(candidate => candidate.Id == recoveryId);
            if (!resultExists && path.TargetTransitionId is null && !recoveryExists)
                errors.Add(new("PB-PATH-001", "A path must terminate in a typed result, transition, or recovery path."));

            if (path.RecoveryPathId is { } expectedRecovery &&
                pathArray.All(candidate => candidate.Id != expectedRecovery || candidate.RecoversFromPathId != path.Id))
                errors.Add(new("PB-PATH-001", "A recovery link must be reciprocal and remain inside the path definition set."));

            if (path.RecoveryStrategy is RecoveryStrategy.Retry or RecoveryStrategy.CorrectAndRetry &&
                (path.RetryPolicy is null || path.IdempotencyAnalysis is null))
                errors.Add(new("PB-PATH-003", "A retry path requires both retry policy and idempotency analysis."));

            if (path.Classification == PathClassification.Cancellation && string.IsNullOrWhiteSpace(path.TerminalState.Value))
                errors.Add(new("PB-PATH-006", "A cancellation path must state the domain state that remains."));

            if (path.Classification == PathClassification.Degraded &&
                (path.ExitCondition is null || path.Reconciliation is null))
                errors.Add(new("PB-PATH-007", "A degraded path requires an exit condition and reconciliation behavior."));

            if (path.Classification is PathClassification.Exceptional or PathClassification.Degraded or PathClassification.Cancellation &&
                string.IsNullOrWhiteSpace(path.Observation.Value))
                errors.Add(new("PB-PATH-008", "A non-happy path must expose a participant-visible observation."));

            if (path.TerminalResultId is { } terminalId &&
                resultArray.SingleOrDefault(result => result.Id == terminalId)?.ResultKind == SemanticResultKind.Partial &&
                path.RecoveryPathId is null)
                errors.Add(new("PB-PATH-005", "A partial result requires an owned recovery path."));
        }

        foreach (var effect in effectArray)
        {
            if (pathArray.All(path => path.Id != effect.ParentId))
                errors.Add(new("PB-PATH-001", "Every effect must belong to a modeled path."));
            if (effect.Kind == EffectKind.ExternalInteraction &&
                (effect.FailurePathId is null || pathArray.All(path => path.Id != effect.FailurePathId)))
                errors.Add(new("PB-PATH-002", "An external effect requires an explicit failure or recovery path."));
        }

        return errors;
    }
}

public static class PathTransition
{
    public static DefinePathTransitionResult Define(
        ProjectDefinition project, Revision expectedRevision, PathIds ids, PathDraft draft,
        SemanticResultDefinition terminalResult, SemanticResultDefinition recoveryResult,
        int firstOrder, ChangeSetId changeSetId, ChangeReason reason,
        UtcTimestamp occurredAt, string createdBy)
    {
        if (project.Revision != expectedRevision)
            return new DefinePathTransitionResult.Conflict(
                expectedRevision, project.Revision,
                ProjectChangeSetTransition.RevisionConflicts(expectedRevision, project.Revision));
        if (terminalResult.ProjectId != project.Id || recoveryResult.ProjectId != project.Id ||
            terminalResult.Id != draft.TerminalResultId || recoveryResult.Id != draft.RecoveryResultId)
            return new DefinePathTransitionResult.Invalid([new("PB-PATH-001", "Path terminal results must exist in the current project.")]);

        var branch = new PathDefinition(ids.BranchPathId, project.Id, draft.ScenarioId, draft.BranchName,
            draft.BranchClassification, draft.SourceTransitionId, [ids.BranchConditionId], draft.BranchSegments,
            terminalResult.Id, null, draft.BranchTerminalState, draft.BranchObservation, draft.OwnerId,
            ids.RecoveryPathId, null, null, null, null, draft.ExitCondition, draft.Reconciliation,
            firstOrder, occurredAt, createdBy);
        var branchCondition = new ConditionDefinition(ids.BranchConditionId, project.Id, branch.Id,
            draft.BranchConditionName, draft.BranchConditionKind, draft.BranchCondition,
            draft.BranchFactIds, draft.BranchRuleIds, firstOrder + 1, occurredAt, createdBy);
        var effect = new EffectDefinition(ids.EffectId, project.Id, branch.Id, draft.EffectName,
            draft.EffectKind, draft.EffectStatement,
            draft.EffectKind == EffectKind.ExternalInteraction ? ids.RecoveryPathId : null,
            firstOrder + 2, occurredAt, createdBy);
        var recovery = new PathDefinition(ids.RecoveryPathId, project.Id, draft.ScenarioId, draft.RecoveryName,
            PathClassification.Recovery, draft.SourceTransitionId, [ids.RecoveryConditionId], draft.RecoverySegments,
            recoveryResult.Id, draft.SourceTransitionId, draft.RecoveryTerminalState, draft.RecoveryObservation,
            draft.OwnerId, null, branch.Id, draft.RecoveryStrategy, draft.RetryPolicy,
            draft.IdempotencyAnalysis, draft.ExitCondition, draft.Reconciliation,
            firstOrder + 3, occurredAt, createdBy);
        var recoveryCondition = new ConditionDefinition(ids.RecoveryConditionId, project.Id, recovery.Id,
            draft.RecoveryConditionName, ConditionKind.Entry, draft.RecoveryCondition,
            draft.BranchFactIds, draft.BranchRuleIds, firstOrder + 4, occurredAt, createdBy);

        var validation = PathValidation.Validate(
            [branch, recovery], [branchCondition, recoveryCondition], [effect], [terminalResult, recoveryResult]);
        if (validation.Count > 0)
            return new DefinePathTransitionResult.Invalid(validation);

        var definitions = new PathDefinitionSet(branch, branchCondition, effect, recovery, recoveryCondition);
        var committed = ProjectChangeSetTransition.Commit(
            project,
            expectedRevision,
            new(changeSetId, branch.Id, "path.defined", reason,
                ProjectChangeSetTransition.AddedElements(definitions.Elements)),
            occurredAt,
            createdBy);
        var accepted = (ProjectChangeSetTransitionResult.Accepted)committed;
        return new DefinePathTransitionResult.Accepted(accepted.Project, definitions, accepted.ChangeSet);
    }
}
