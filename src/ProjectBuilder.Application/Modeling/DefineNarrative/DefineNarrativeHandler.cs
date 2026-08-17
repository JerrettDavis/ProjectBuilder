using System.Collections.Immutable;
using ProjectBuilder.Application.Projects.CreateProject;
using ProjectBuilder.Domain.Modeling.Elements;
using ProjectBuilder.Domain.Modeling.Primitives;
using ProjectBuilder.Domain.Modeling.Transitions;

namespace ProjectBuilder.Application.Modeling.DefineNarrative;

public sealed class DefineNarrativeHandler(
    IProjectCreationStore projects, IProjectElementStore elements, IProjectEditAuthorizer authorizer,
    IModelIdentitySource identities, IApplicationClock clock)
{
    public async ValueTask<DefineNarrativeResult> HandleAsync(
        DefineNarrativeCommand command, ProjectActor actor, CancellationToken cancellationToken = default)
    {
        var validation = Validate(command);
        if (validation.Errors.Count > 0) return new DefineNarrativeResult.Invalid(validation.Errors);

        var project = await projects.FindByIdAsync(validation.ProjectId!, cancellationToken);
        if (project is null) return new DefineNarrativeResult.ProjectNotFound(command.ProjectId);
        var access = await authorizer.AuthorizeEditAsync(actor, project.WorkspaceId, cancellationToken);
        if (!access.IsAllowed) return new DefineNarrativeResult.Denied(access.Reason);

        var fingerprint = ModelRequestFingerprint.Create(
            command.ProjectId, command.ExpectedRevision, command.OutcomeId, command.ParticipantIds,
            command.InitiatorId, command.ReceiverId, command.EpisodeName, command.EpisodeStart,
            command.EpisodeEnd, command.ScenarioName, command.Classification, command.StartingFacts,
            command.Trigger, command.ExpectedOutcome, command.SceneName, command.Setting,
            command.Responsibility, command.InteractionName, command.Intent, command.Step,
            command.Observation, command.SemanticResults, command.Reason);
        var prior = await elements.FindCommitByOperationAsync(validation.OperationId!, cancellationToken);
        if (prior is not null) return await ExistingAsync(prior, validation.OperationId!, fingerprint, project.Id, cancellationToken);

        var outcome = await elements.FindOutcomeAsync(project.Id, validation.OutcomeId!, cancellationToken);
        if (outcome is null) return new DefineNarrativeResult.ReferenceNotFound("outcome");
        var participants = ImmutableArray.CreateBuilder<ActorDefinition>();
        foreach (var id in validation.ParticipantIds)
        {
            var participant = await elements.FindActorAsync(project.Id, id, cancellationToken);
            if (participant is null) return new DefineNarrativeResult.ReferenceNotFound($"participant {id}");
            participants.Add(participant);
        }
        var initiator = participants.FirstOrDefault(x => x.Id == validation.InitiatorId);
        var receiver = participants.FirstOrDefault(x => x.Id == validation.ReceiverId);
        if (initiator is null) return new DefineNarrativeResult.ReferenceNotFound("initiator participant");
        if (receiver is null) return new DefineNarrativeResult.ReferenceNotFound("receiver participant");

        var ids = new NarrativeIds(identities.NextElementId(), identities.NextElementId(), identities.NextElementId(),
            identities.NextElementId(), identities.NextElementId(), identities.NextElementId(), identities.NextElementId());
        var transitioned = NarrativeTransition.Define(project, validation.ExpectedRevision!, outcome.Outcome,
            participants.ToImmutable(), initiator, receiver, ids, validation.Draft!,
            await elements.NextElementOrderAsync(project.Id, cancellationToken), validation.OperationId!,
            validation.Reason!, clock.GetCurrentTimestamp(), actor.Subject);
        if (transitioned is DefineNarrativeTransitionResult.Conflict conflict)
            return new DefineNarrativeResult.Conflict(
                conflict.Expected.Value, conflict.Actual.Value,
                ModelApplicationMapping.Conflicts(conflict.Conflicts));
        if (transitioned is DefineNarrativeTransitionResult.Invalid invalid)
            return new DefineNarrativeResult.Invalid(invalid.Errors);

        var accepted = (DefineNarrativeTransitionResult.Accepted)transitioned;
        var stored = await elements.CommitNarrativeAsync(accepted, fingerprint, cancellationToken);
        return stored switch
        {
            ElementStoreCommitResult.Committed => await ReloadAsync(project.Id, accepted.Narrative.Episode.Id, accepted.Project.Revision, cancellationToken),
            ElementStoreCommitResult.RevisionConflict storeConflict => new DefineNarrativeResult.Conflict(
                validation.ExpectedRevision!.Value, storeConflict.Actual.Value,
                ModelApplicationMapping.RevisionConflict(validation.ExpectedRevision!, storeConflict.Actual)),
            ElementStoreCommitResult.OperationConflict => await ReloadOperationAsync(validation.OperationId!, fingerprint, project.Id, cancellationToken),
            _ => throw new InvalidOperationException("Unknown element store result."),
        };
    }

    private async ValueTask<DefineNarrativeResult> ExistingAsync(StoredElementCommit existing, ChangeSetId operationId,
        string fingerprint, ProjectId projectId, CancellationToken cancellationToken)
    {
        if (existing.ChangeKind != "narrative.defined" || !string.Equals(existing.RequestFingerprint, fingerprint, StringComparison.Ordinal))
            return new DefineNarrativeResult.IdempotencyConflict(operationId.ToString());
        return await ReloadAsync(projectId, existing.ElementId, existing.ResultRevision, cancellationToken);
    }
    private async ValueTask<DefineNarrativeResult> ReloadOperationAsync(ChangeSetId id, string fingerprint,
        ProjectId projectId, CancellationToken cancellationToken)
    {
        var existing = await elements.FindCommitByOperationAsync(id, cancellationToken);
        return existing is null ? throw new InvalidOperationException("An operation conflict could not be reloaded.")
            : await ExistingAsync(existing, id, fingerprint, projectId, cancellationToken);
    }
    private async ValueTask<DefineNarrativeResult> ReloadAsync(ProjectId projectId, ElementId episodeId,
        Revision revision, CancellationToken cancellationToken)
    {
        var narrative = await elements.FindNarrativeAsync(projectId, episodeId, cancellationToken) ??
            throw new InvalidOperationException("A committed narrative could not be reloaded.");
        return new DefineNarrativeResult.Defined(narrative, revision.Value, "Review the narrative and model state and rules next.");
    }

    private static Validated Validate(DefineNarrativeCommand command)
    {
        var errors = new List<SemanticError>();
        var projectId = ModelInputValidation.Accept(ProjectId.Parse(command.ProjectId), errors);
        var expected = ModelInputValidation.Accept(Revision.Parse(command.ExpectedRevision), errors);
        var operation = ModelInputValidation.Accept(ChangeSetId.Parse(command.OperationId), errors);
        var outcome = ModelInputValidation.Accept(ElementId.Parse(command.OutcomeId), errors);
        var participants = ParseIds(command.ParticipantIds, errors);
        var initiator = ModelInputValidation.Accept(ElementId.Parse(command.InitiatorId), errors);
        var receiver = ModelInputValidation.Accept(ElementId.Parse(command.ReceiverId), errors);
        var classification = ParseClassification(command.Classification, errors);
        var draftValues = new object?[] {
            ModelInputValidation.Accept(ElementName.Create(command.EpisodeName), errors),
            ModelInputValidation.Accept(NarrativeText.Create(command.EpisodeStart), errors),
            ModelInputValidation.Accept(NarrativeText.Create(command.EpisodeEnd), errors),
            ModelInputValidation.Accept(ElementName.Create(command.ScenarioName), errors),
            ModelInputValidation.Accept(NarrativeText.Create(command.Trigger), errors),
            ModelInputValidation.Accept(NarrativeText.Create(command.ExpectedOutcome), errors),
            ModelInputValidation.Accept(ElementName.Create(command.SceneName), errors),
            ModelInputValidation.Accept(NarrativeText.Create(command.Setting), errors),
            ModelInputValidation.Accept(NarrativeText.Create(command.Responsibility), errors),
            ModelInputValidation.Accept(ElementName.Create(command.InteractionName), errors),
            ModelInputValidation.Accept(NarrativeText.Create(command.Intent), errors),
            ModelInputValidation.Accept(NarrativeText.Create(command.Step), errors),
            ModelInputValidation.Accept(NarrativeText.Create(command.Observation), errors),
        };
        var facts = ModelInputValidation.NarrativeFacts(command.StartingFacts, errors, "starting_facts");
        var results = ModelInputValidation.NarrativeFacts(command.SemanticResults, errors, "semantic_results");
        var reason = ModelInputValidation.Accept(ChangeReason.Create(command.Reason), errors);
        NarrativeDraft? draft = errors.Count == 0 ? new(
            (ElementName)draftValues[0]!, (NarrativeText)draftValues[1]!, (NarrativeText)draftValues[2]!,
            (ElementName)draftValues[3]!, classification!.Value, facts, (NarrativeText)draftValues[4]!,
            (NarrativeText)draftValues[5]!, (ElementName)draftValues[6]!, (NarrativeText)draftValues[7]!,
            (NarrativeText)draftValues[8]!, (ElementName)draftValues[9]!, (NarrativeText)draftValues[10]!,
            (NarrativeText)draftValues[11]!, (NarrativeText)draftValues[12]!, results) : null;
        return new(projectId, expected, operation, outcome, participants, initiator, receiver, draft, reason, errors);
    }

    private static ImmutableArray<ElementId> ParseIds(string input, List<SemanticError> errors)
    {
        var builder = ImmutableArray.CreateBuilder<ElementId>();
        foreach (var value in input.Split([',', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var parsed = ModelInputValidation.Accept(ElementId.Parse(value), errors);
            if (parsed is not null && !builder.Contains(parsed)) builder.Add(parsed);
        }
        if (builder.Count == 0) errors.Add(new("narrative.participants.required", "At least one participant is required."));
        return builder.ToImmutable();
    }
    private static ScenarioClassification? ParseClassification(string value, List<SemanticError> errors)
    {
        if (Enum.TryParse<ScenarioClassification>(value, true, out var parsed)) return parsed;
        errors.Add(new("scenario.classification.invalid", "Select a supported scenario classification."));
        return null;
    }
    private sealed record Validated(ProjectId? ProjectId, Revision? ExpectedRevision, ChangeSetId? OperationId,
        ElementId? OutcomeId, ImmutableArray<ElementId> ParticipantIds, ElementId? InitiatorId,
        ElementId? ReceiverId, NarrativeDraft? Draft, ChangeReason? Reason, IReadOnlyList<SemanticError> Errors);
}
