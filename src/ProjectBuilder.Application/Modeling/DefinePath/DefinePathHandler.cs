using System.Collections.Immutable;
using ProjectBuilder.Application.Projects.CreateProject;
using ProjectBuilder.Domain.Modeling.Elements;
using ProjectBuilder.Domain.Modeling.Primitives;
using ProjectBuilder.Domain.Modeling.Transitions;

namespace ProjectBuilder.Application.Modeling.DefinePath;

public sealed class DefinePathHandler(
    IProjectCreationStore projects, IProjectElementStore elements, IProjectEditAuthorizer authorizer,
    IModelIdentitySource identities, IApplicationClock clock)
{
    public async ValueTask<DefinePathResult> HandleAsync(
        DefinePathCommand command, ProjectActor actor, CancellationToken cancellationToken = default)
    {
        var validation = Validate(command);
        if (validation.Errors.Count > 0) return new DefinePathResult.Invalid(validation.Errors);
        var project = await projects.FindByIdAsync(validation.ProjectId!, cancellationToken);
        if (project is null) return new DefinePathResult.ProjectNotFound(command.ProjectId);
        var access = await authorizer.AuthorizeEditAsync(actor, project.WorkspaceId, cancellationToken);
        if (!access.IsAllowed) return new DefinePathResult.Denied(access.Reason);

        var model = await elements.LoadModelAsync(project.Id, cancellationToken);
        if (model.Actors.All(item => item.Id != validation.Draft!.OwnerId))
            return new DefinePathResult.ReferenceNotFound("path owner");
        if (model.Narratives.All(item => item.ScenarioId != validation.Draft!.ScenarioId.ToString()))
            return new DefinePathResult.ReferenceNotFound("source scenario");
        var stateLogic = model.StateLogic.SingleOrDefault(item => item.TransitionId == validation.Draft!.SourceTransitionId.ToString());
        if (stateLogic is null) return new DefinePathResult.ReferenceNotFound("source transition");
        if (validation.Draft!.BranchFactIds.Any(id => id.ToString() != stateLogic.FactId))
            return new DefinePathResult.ReferenceNotFound("condition fact");
        if (validation.Draft.BranchRuleIds.Any(id => id.ToString() != stateLogic.RuleId))
            return new DefinePathResult.ReferenceNotFound("condition rule");
        if (stateLogic.Results.All(item => item.Id != validation.Draft.TerminalResultId.ToString()))
            return new DefinePathResult.ReferenceNotFound("terminal result for the source transition");
        if (stateLogic.Results.All(item => item.Id != validation.Draft.RecoveryResultId.ToString()))
            return new DefinePathResult.ReferenceNotFound("recovery result for the source transition");

        var terminalResult = await elements.FindSemanticResultAsync(project.Id, validation.Draft.TerminalResultId, cancellationToken);
        if (terminalResult is null) return new DefinePathResult.ReferenceNotFound("terminal result");
        var recoveryResult = await elements.FindSemanticResultAsync(project.Id, validation.Draft.RecoveryResultId, cancellationToken);
        if (recoveryResult is null) return new DefinePathResult.ReferenceNotFound("recovery result");

        var fingerprint = ModelRequestFingerprint.Create(
            command.ProjectId, command.ExpectedRevision, command.ScenarioId, command.SourceTransitionId,
            command.TerminalResultId, command.RecoveryResultId, command.OwnerId,
            command.BranchName, command.BranchClassification, command.BranchConditionName,
            command.BranchConditionKind, command.BranchCondition, command.BranchFactIds,
            command.BranchRuleIds, command.BranchSegments, command.BranchTerminalState,
            command.BranchObservation, command.EffectName, command.EffectKind, command.EffectStatement,
            command.RecoveryName, command.RecoveryStrategy, command.RecoveryConditionName,
            command.RecoveryCondition, command.RecoverySegments, command.RecoveryTerminalState,
            command.RecoveryObservation, command.RetryPolicy, command.IdempotencyAnalysis,
            command.ExitCondition, command.Reconciliation, command.Reason);
        var prior = await elements.FindCommitByOperationAsync(validation.OperationId!, cancellationToken);
        if (prior is not null)
            return await ExistingAsync(prior, validation.OperationId!, fingerprint, project.Id, cancellationToken);

        var ids = new PathIds(identities.NextElementId(), identities.NextElementId(), identities.NextElementId(),
            identities.NextElementId(), identities.NextElementId());
        var transitioned = PathTransition.Define(project, validation.ExpectedRevision!, ids, validation.Draft,
            terminalResult, recoveryResult, await elements.NextElementOrderAsync(project.Id, cancellationToken),
            validation.OperationId!, validation.Reason!, clock.GetCurrentTimestamp(), actor.Subject);
        if (transitioned is DefinePathTransitionResult.Conflict conflict)
            return new DefinePathResult.Conflict(
                conflict.Expected.Value, conflict.Actual.Value,
                ModelApplicationMapping.Conflicts(conflict.Conflicts));
        if (transitioned is DefinePathTransitionResult.Invalid invalid)
            return new DefinePathResult.Invalid(invalid.Errors);

        var accepted = (DefinePathTransitionResult.Accepted)transitioned;
        var stored = await elements.CommitPathAsync(accepted, fingerprint, cancellationToken);
        return stored switch
        {
            ElementStoreCommitResult.Committed => await ReloadAsync(project.Id, accepted.Definitions.Branch.Id,
                accepted.Project.Revision, cancellationToken),
            ElementStoreCommitResult.RevisionConflict storeConflict =>
                new DefinePathResult.Conflict(
                    validation.ExpectedRevision!.Value, storeConflict.Actual.Value,
                    ModelApplicationMapping.RevisionConflict(validation.ExpectedRevision!, storeConflict.Actual)),
            ElementStoreCommitResult.OperationConflict =>
                await ReloadOperationAsync(validation.OperationId!, fingerprint, project.Id, cancellationToken),
            _ => throw new InvalidOperationException("Unknown element store result."),
        };
    }

    private async ValueTask<DefinePathResult> ExistingAsync(StoredElementCommit existing, ChangeSetId operationId,
        string fingerprint, ProjectId projectId, CancellationToken cancellationToken)
    {
        if (existing.ChangeKind != "path.defined" ||
            !string.Equals(existing.RequestFingerprint, fingerprint, StringComparison.Ordinal))
            return new DefinePathResult.IdempotencyConflict(operationId.ToString());
        return await ReloadAsync(projectId, existing.ElementId, existing.ResultRevision, cancellationToken);
    }

    private async ValueTask<DefinePathResult> ReloadOperationAsync(ChangeSetId operationId, string fingerprint,
        ProjectId projectId, CancellationToken cancellationToken)
    {
        var existing = await elements.FindCommitByOperationAsync(operationId, cancellationToken);
        return existing is null
            ? throw new InvalidOperationException("An operation conflict could not be reloaded.")
            : await ExistingAsync(existing, operationId, fingerprint, projectId, cancellationToken);
    }

    private async ValueTask<DefinePathResult> ReloadAsync(ProjectId projectId, ElementId branchPathId,
        Revision revision, CancellationToken cancellationToken)
    {
        var overview = await elements.FindPathAsync(projectId, branchPathId, cancellationToken) ??
            throw new InvalidOperationException("Committed path definitions could not be reloaded.");
        return new DefinePathResult.Defined(overview, revision.Value, "Review the branch, recovery, and terminal state.");
    }

    private static Validated Validate(DefinePathCommand command)
    {
        var errors = new List<SemanticError>();
        var projectId = ModelInputValidation.Accept(ProjectId.Parse(command.ProjectId), errors);
        var expected = ModelInputValidation.Accept(Revision.Parse(command.ExpectedRevision), errors);
        var operation = ModelInputValidation.Accept(ChangeSetId.Parse(command.OperationId), errors);
        var scenario = ModelInputValidation.Accept(ElementId.Parse(command.ScenarioId), errors);
        var transition = ModelInputValidation.Accept(ElementId.Parse(command.SourceTransitionId), errors);
        var terminalResult = ModelInputValidation.Accept(ElementId.Parse(command.TerminalResultId), errors);
        var recoveryResult = ModelInputValidation.Accept(ElementId.Parse(command.RecoveryResultId), errors);
        var owner = ModelInputValidation.Accept(ElementId.Parse(command.OwnerId), errors);
        var classification = ParseEnum<PathClassification>(command.BranchClassification, errors, "path.classification.invalid");
        var conditionKind = ParseEnum<ConditionKind>(command.BranchConditionKind, errors, "path.condition_kind.invalid");
        var effectKind = ParseEnum<EffectKind>(command.EffectKind, errors, "path.effect_kind.invalid");
        var strategy = ParseEnum<RecoveryStrategy>(command.RecoveryStrategy, errors, "path.recovery_strategy.invalid");
        var branchName = ModelInputValidation.Accept(ElementName.Create(command.BranchName), errors);
        var branchConditionName = ModelInputValidation.Accept(ElementName.Create(command.BranchConditionName), errors);
        var branchCondition = ModelInputValidation.Accept(LogicStatement.Create(command.BranchCondition), errors);
        var facts = Ids(command.BranchFactIds, errors);
        var rules = Ids(command.BranchRuleIds, errors);
        var branchSegments = Terms(command.BranchSegments, errors, "path.branch_segments");
        var branchState = ModelInputValidation.Accept(LogicStatement.Create(command.BranchTerminalState), errors);
        var branchObservation = ModelInputValidation.Accept(LogicStatement.Create(command.BranchObservation), errors);
        var effectName = ModelInputValidation.Accept(ElementName.Create(command.EffectName), errors);
        var effectStatement = ModelInputValidation.Accept(LogicStatement.Create(command.EffectStatement), errors);
        var recoveryName = ModelInputValidation.Accept(ElementName.Create(command.RecoveryName), errors);
        var recoveryConditionName = ModelInputValidation.Accept(ElementName.Create(command.RecoveryConditionName), errors);
        var recoveryCondition = ModelInputValidation.Accept(LogicStatement.Create(command.RecoveryCondition), errors);
        var recoverySegments = Terms(command.RecoverySegments, errors, "path.recovery_segments");
        var recoveryState = ModelInputValidation.Accept(LogicStatement.Create(command.RecoveryTerminalState), errors);
        var recoveryObservation = ModelInputValidation.Accept(LogicStatement.Create(command.RecoveryObservation), errors);
        var retryPolicy = Optional(command.RetryPolicy, errors);
        var idempotency = Optional(command.IdempotencyAnalysis, errors);
        var exit = Optional(command.ExitCondition, errors);
        var reconciliation = Optional(command.Reconciliation, errors);
        var reason = ModelInputValidation.Accept(ChangeReason.Create(command.Reason), errors);

        PathDraft? draft = errors.Count == 0 ? new(
            scenario!, transition!, terminalResult!, recoveryResult!, owner!, branchName!, classification!.Value,
            branchConditionName!, conditionKind!.Value, branchCondition!, facts, rules, branchSegments,
            branchState!, branchObservation!, effectName!, effectKind!.Value, effectStatement!,
            recoveryName!, strategy!.Value, recoveryConditionName!, recoveryCondition!, recoverySegments,
            recoveryState!, recoveryObservation!, retryPolicy, idempotency, exit, reconciliation) : null;
        return new(projectId, expected, operation, draft, reason, errors);
    }

    private static ImmutableArray<ElementId> Ids(string input, List<SemanticError> errors)
    {
        var builder = ImmutableArray.CreateBuilder<ElementId>();
        foreach (var value in input.Split(
            ['\r', '\n', ','], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var id = ModelInputValidation.Accept(ElementId.Parse(value), errors);
            if (id is not null) builder.Add(id);
        }
        return builder.ToImmutable();
    }

    private static ImmutableArray<LogicTerm> Terms(string input, List<SemanticError> errors, string field)
    {
        var values = Lines(input);
        if (values.Length == 0) errors.Add(new($"{field}.required", $"At least one {field} entry is required."));
        var builder = ImmutableArray.CreateBuilder<LogicTerm>();
        foreach (var value in values)
        {
            var term = ModelInputValidation.Accept(LogicTerm.Create(value), errors);
            if (term is not null) builder.Add(term);
        }
        return builder.ToImmutable();
    }

    private static LogicStatement? Optional(string value, List<SemanticError> errors) =>
        string.IsNullOrWhiteSpace(value) ? null : ModelInputValidation.Accept(LogicStatement.Create(value), errors);

    private static string[] Lines(string value) => value.Split(
        ['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static TEnum? ParseEnum<TEnum>(string value, List<SemanticError> errors, string code)
        where TEnum : struct, Enum
    {
        if (Enum.TryParse<TEnum>(value, true, out var parsed) && Enum.IsDefined(parsed)) return parsed;
        errors.Add(new(code, $"'{value}' is not a supported {typeof(TEnum).Name}."));
        return null;
    }

    private sealed record Validated(ProjectId? ProjectId, Revision? ExpectedRevision, ChangeSetId? OperationId,
        PathDraft? Draft, ChangeReason? Reason, IReadOnlyList<SemanticError> Errors);
}
