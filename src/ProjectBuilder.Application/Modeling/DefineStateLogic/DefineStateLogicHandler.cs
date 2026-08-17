using System.Collections.Immutable;
using ProjectBuilder.Application.Projects.CreateProject;
using ProjectBuilder.Domain.Modeling.Elements;
using ProjectBuilder.Domain.Modeling.Primitives;
using ProjectBuilder.Domain.Modeling.Transitions;

namespace ProjectBuilder.Application.Modeling.DefineStateLogic;

public sealed class DefineStateLogicHandler(
    IProjectCreationStore projects, IProjectElementStore elements, IProjectEditAuthorizer authorizer,
    IModelIdentitySource identities, IApplicationClock clock)
{
    public async ValueTask<DefineStateLogicResult> HandleAsync(
        DefineStateLogicCommand command, ProjectActor actor, CancellationToken cancellationToken = default)
    {
        var validation = Validate(command);
        if (validation.Errors.Count > 0) return new DefineStateLogicResult.Invalid(validation.Errors);
        var project = await projects.FindByIdAsync(validation.ProjectId!, cancellationToken);
        if (project is null) return new DefineStateLogicResult.ProjectNotFound(command.ProjectId);
        var access = await authorizer.AuthorizeEditAsync(actor, project.WorkspaceId, cancellationToken);
        if (!access.IsAllowed) return new DefineStateLogicResult.Denied(access.Reason);

        var model = await elements.LoadModelAsync(project.Id, cancellationToken);
        if (model.Actors.All(x => x.Id != validation.Draft!.OwnerId)) return new DefineStateLogicResult.ReferenceNotFound("state owner");
        if (model.Actors.All(x => x.Id != validation.Draft!.RuleAuthorityOwnerId)) return new DefineStateLogicResult.ReferenceNotFound("rule authority owner");

        var fingerprint = ModelRequestFingerprint.Create(command.ProjectId, command.ExpectedRevision,
            command.StateName, command.StateCategory, command.StateStructure, command.StateValues, command.OwnerId,
            command.FactName, command.FactValueType, command.FactAuthority, command.FactMutability,
            command.RuleName, command.RuleKind, command.RuleStatement, command.RuleAuthorityOwnerId,
            command.InvariantName, command.InvariantStatement, command.FalsifyingExample, command.ProofExpectation,
            command.SemanticResults, command.TransitionName, command.SourcePredicate, command.Trigger,
            command.TargetPredicate, command.Reason);
        var prior = await elements.FindCommitByOperationAsync(validation.OperationId!, cancellationToken);
        if (prior is not null) return await ExistingAsync(prior, validation.OperationId!, fingerprint, project.Id, cancellationToken);

        var stateId = identities.NextElementId();
        var factId = identities.NextElementId();
        var ruleId = identities.NextElementId();
        var invariantId = identities.NextElementId();
        var resultIds = validation.Draft!.Results.Select(_ => identities.NextElementId()).ToImmutableArray();
        var transitionId = identities.NextElementId();
        var ids = new StateLogicIds(
            stateId,
            factId,
            ruleId,
            invariantId,
            transitionId,
            resultIds);
        var transitioned = StateLogicTransition.Define(project, validation.ExpectedRevision!, ids, validation.Draft,
            await elements.NextElementOrderAsync(project.Id, cancellationToken), validation.OperationId!, validation.Reason!,
            clock.GetCurrentTimestamp(), actor.Subject);
        if (transitioned is DefineStateLogicTransitionResult.Conflict conflict)
            return new DefineStateLogicResult.Conflict(
                conflict.Expected.Value, conflict.Actual.Value,
                ModelApplicationMapping.Conflicts(conflict.Conflicts));
        if (transitioned is DefineStateLogicTransitionResult.Invalid invalid)
            return new DefineStateLogicResult.Invalid(invalid.Errors);

        var accepted = (DefineStateLogicTransitionResult.Accepted)transitioned;
        var stored = await elements.CommitStateLogicAsync(accepted, fingerprint, cancellationToken);
        return stored switch
        {
            ElementStoreCommitResult.Committed => await ReloadAsync(project.Id, accepted.Definitions.State.Id, accepted.Project.Revision, cancellationToken),
            ElementStoreCommitResult.RevisionConflict storeConflict => new DefineStateLogicResult.Conflict(
                validation.ExpectedRevision!.Value, storeConflict.Actual.Value,
                ModelApplicationMapping.RevisionConflict(validation.ExpectedRevision!, storeConflict.Actual)),
            ElementStoreCommitResult.OperationConflict => await ReloadOperationAsync(validation.OperationId!, fingerprint, project.Id, cancellationToken),
            _ => throw new InvalidOperationException("Unknown element store result."),
        };
    }

    private async ValueTask<DefineStateLogicResult> ExistingAsync(StoredElementCommit existing, ChangeSetId operationId,
        string fingerprint, ProjectId projectId, CancellationToken cancellationToken)
    {
        if (existing.ChangeKind != "state-logic.defined" || !string.Equals(existing.RequestFingerprint, fingerprint, StringComparison.Ordinal))
            return new DefineStateLogicResult.IdempotencyConflict(operationId.ToString());
        return await ReloadAsync(projectId, existing.ElementId, existing.ResultRevision, cancellationToken);
    }
    private async ValueTask<DefineStateLogicResult> ReloadOperationAsync(ChangeSetId id, string fingerprint,
        ProjectId projectId, CancellationToken cancellationToken)
    {
        var existing = await elements.FindCommitByOperationAsync(id, cancellationToken);
        return existing is null ? throw new InvalidOperationException("An operation conflict could not be reloaded.")
            : await ExistingAsync(existing, id, fingerprint, projectId, cancellationToken);
    }
    private async ValueTask<DefineStateLogicResult> ReloadAsync(ProjectId projectId, ElementId stateId,
        Revision revision, CancellationToken cancellationToken)
    {
        var overview = await elements.FindStateLogicAsync(projectId, stateId, cancellationToken) ??
            throw new InvalidOperationException("Committed state and logic could not be reloaded.");
        return new DefineStateLogicResult.Defined(overview, revision.Value, "Model alternate and failure paths next.");
    }

    private static Validated Validate(DefineStateLogicCommand command)
    {
        var errors = new List<SemanticError>();
        var projectId = ModelInputValidation.Accept(ProjectId.Parse(command.ProjectId), errors);
        var expected = ModelInputValidation.Accept(Revision.Parse(command.ExpectedRevision), errors);
        var operation = ModelInputValidation.Accept(ChangeSetId.Parse(command.OperationId), errors);
        var owner = ModelInputValidation.Accept(ElementId.Parse(command.OwnerId), errors);
        var ruleOwner = ModelInputValidation.Accept(ElementId.Parse(command.RuleAuthorityOwnerId), errors);
        var stateCategory = ParseEnum<StateCategory>(command.StateCategory, errors, "state.category.invalid");
        var factMutability = ParseEnum<FactMutability>(command.FactMutability, errors, "fact.mutability.invalid");
        var ruleKind = ParseEnum<RuleKind>(command.RuleKind, errors, "rule.kind.invalid");
        var structure = Terms(command.StateStructure, errors, "state.structure");
        var values = Terms(command.StateValues, errors, "state.values", false);
        var proof = Terms(command.ProofExpectation, errors, "invariant.proof");
        var results = Results(command.SemanticResults, errors);
        var stateName = ModelInputValidation.Accept(ElementName.Create(command.StateName), errors);
        var factName = ModelInputValidation.Accept(ElementName.Create(command.FactName), errors);
        var factType = ModelInputValidation.Accept(LogicTerm.Create(command.FactValueType), errors);
        var factAuthority = ModelInputValidation.Accept(LogicStatement.Create(command.FactAuthority), errors);
        var ruleName = ModelInputValidation.Accept(ElementName.Create(command.RuleName), errors);
        var ruleStatement = ModelInputValidation.Accept(LogicStatement.Create(command.RuleStatement), errors);
        var invariantName = ModelInputValidation.Accept(ElementName.Create(command.InvariantName), errors);
        var invariantStatement = ModelInputValidation.Accept(LogicStatement.Create(command.InvariantStatement), errors);
        var falsifying = ModelInputValidation.Accept(LogicStatement.Create(command.FalsifyingExample), errors);
        var transitionName = ModelInputValidation.Accept(ElementName.Create(command.TransitionName), errors);
        var source = ModelInputValidation.Accept(LogicStatement.Create(command.SourcePredicate), errors);
        var trigger = ModelInputValidation.Accept(LogicStatement.Create(command.Trigger), errors);
        var target = ModelInputValidation.Accept(LogicStatement.Create(command.TargetPredicate), errors);
        var reason = ModelInputValidation.Accept(ChangeReason.Create(command.Reason), errors);
        StateLogicDraft? draft = errors.Count == 0 ? new(stateName!, stateCategory!.Value, structure, values, owner!,
            factName!, factType!, factAuthority!, factMutability!.Value, ruleName!, ruleKind!.Value, ruleStatement!,
            ruleOwner!, invariantName!, invariantStatement!, falsifying!, proof, results, transitionName!, source!, trigger!, target!) : null;
        return new(projectId, expected, operation, draft, reason, errors);
    }

    private static ImmutableArray<LogicTerm> Terms(string input, List<SemanticError> errors, string field, bool required = true)
    {
        var values = input.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (required && values.Length == 0) errors.Add(new($"{field}.required", $"At least one {field} entry is required."));
        var builder = ImmutableArray.CreateBuilder<LogicTerm>();
        foreach (var value in values) { var term = ModelInputValidation.Accept(LogicTerm.Create(value), errors); if (term is not null) builder.Add(term); }
        return builder.ToImmutable();
    }
    private static ImmutableArray<SemanticResultDraft> Results(string input, List<SemanticError> errors)
    {
        var builder = ImmutableArray.CreateBuilder<SemanticResultDraft>();
        foreach (var line in input.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var parts = line.Split('|', 3, StringSplitOptions.TrimEntries);
            if (parts.Length != 3) { errors.Add(new("semantic_result.format", "Each semantic result must use Name | Kind | Meaning.")); continue; }
            var name = ModelInputValidation.Accept(ElementName.Create(parts[0]), errors);
            var kind = ParseEnum<SemanticResultKind>(parts[1], errors, "semantic_result.kind.invalid");
            var meaning = ModelInputValidation.Accept(LogicStatement.Create(parts[2]), errors);
            if (name is not null && kind is not null && meaning is not null) builder.Add(new(name, kind.Value, meaning));
        }
        if (builder.Count == 0) errors.Add(new("PB-STATE-010", "At least one typed semantic result is required."));
        return builder.ToImmutable();
    }
    private static TEnum? ParseEnum<TEnum>(string value, List<SemanticError> errors, string code) where TEnum : struct, Enum
    {
        if (Enum.TryParse<TEnum>(value, true, out var parsed) && Enum.IsDefined(parsed)) return parsed;
        errors.Add(new(code, $"'{value}' is not a supported {typeof(TEnum).Name}.")); return null;
    }
    private sealed record Validated(ProjectId? ProjectId, Revision? ExpectedRevision, ChangeSetId? OperationId,
        StateLogicDraft? Draft, ChangeReason? Reason, IReadOnlyList<SemanticError> Errors);
}
