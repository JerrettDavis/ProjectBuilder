using ProjectBuilder.Application.Projects.CreateProject;
using ProjectBuilder.Domain.Modeling.Elements;
using ProjectBuilder.Domain.Modeling.Primitives;
using ProjectBuilder.Domain.Modeling.Transitions;

namespace ProjectBuilder.Application.Modeling.AddOutcome;

public sealed class AddOutcomeHandler(
    IProjectCreationStore projects,
    IProjectElementStore elements,
    IProjectEditAuthorizer authorizer,
    IModelIdentitySource identities,
    IApplicationClock clock)
{
    public async ValueTask<AddOutcomeResult> HandleAsync(
        AddOutcomeCommand command,
        ProjectActor actor,
        CancellationToken cancellationToken = default)
    {
        var validation = Validate(command);
        if (validation.Errors.Count > 0)
        {
            return new AddOutcomeResult.Invalid(validation.Errors);
        }

        var project = await projects.FindByIdAsync(validation.ProjectId!, cancellationToken);
        if (project is null)
        {
            return new AddOutcomeResult.ProjectNotFound(command.ProjectId);
        }

        var access = await authorizer.AuthorizeEditAsync(actor, project.WorkspaceId, cancellationToken);
        if (!access.IsAllowed)
        {
            return new AddOutcomeResult.Denied(access.Reason);
        }

        var fingerprint = ModelRequestFingerprint.Create(
            command.ProjectId, command.ExpectedRevision, command.Name, command.Statement,
            command.SuccessSignals, command.BeneficiaryActorId, command.KnowledgeStatus, command.Reason);
        var existing = await elements.FindCommitByOperationAsync(validation.OperationId!, cancellationToken);
        if (existing is not null)
        {
            return await ExistingAsync(existing, validation.OperationId!, fingerprint, project.Id, cancellationToken);
        }

        var beneficiary = await elements.FindActorAsync(project.Id, validation.BeneficiaryId!, cancellationToken);
        if (beneficiary is null)
        {
            return new AddOutcomeResult.BeneficiaryNotFound(command.BeneficiaryActorId);
        }

        var transitioned = ProjectElementTransition.AddOutcome(
            project,
            validation.ExpectedRevision!,
            identities.NextElementId(),
            validation.Name!,
            validation.Statement!,
            validation.SuccessSignals,
            beneficiary,
            identities.NextRelationId(),
            await elements.NextElementOrderAsync(project.Id, cancellationToken),
            validation.OperationId!,
            validation.Reason!,
            clock.GetCurrentTimestamp(),
            actor.Subject,
            validation.KnowledgeStatus!.Value);

        if (transitioned is AddOutcomeTransitionResult.Conflict revisionConflict)
        {
            return new AddOutcomeResult.Conflict(
                revisionConflict.Expected.Value, revisionConflict.Actual.Value,
                ModelApplicationMapping.Conflicts(revisionConflict.Conflicts));
        }

        if (transitioned is AddOutcomeTransitionResult.InvalidBeneficiary)
        {
            return new AddOutcomeResult.BeneficiaryNotFound(command.BeneficiaryActorId);
        }

        var accepted = (AddOutcomeTransitionResult.Accepted)transitioned;
        var stored = await elements.CommitOutcomeAsync(accepted, fingerprint, cancellationToken);
        return stored switch
        {
            ElementStoreCommitResult.Committed => Added(accepted.Outcome, accepted.Beneficiary.Id, beneficiary, accepted.Project.Revision),
            ElementStoreCommitResult.RevisionConflict storeConflict =>
                new AddOutcomeResult.Conflict(
                    validation.ExpectedRevision!.Value, storeConflict.Actual.Value,
                    ModelApplicationMapping.RevisionConflict(validation.ExpectedRevision!, storeConflict.Actual)),
            ElementStoreCommitResult.OperationConflict =>
                await ReloadOperationAsync(validation.OperationId!, fingerprint, project.Id, cancellationToken),
            _ => throw new InvalidOperationException("Unknown element store result."),
        };
    }

    private async ValueTask<AddOutcomeResult> ExistingAsync(
        StoredElementCommit existing,
        ChangeSetId operationId,
        string fingerprint,
        ProjectId projectId,
        CancellationToken cancellationToken)
    {
        if (existing.ChangeKind != "outcome.added" ||
            !string.Equals(existing.RequestFingerprint, fingerprint, StringComparison.Ordinal))
        {
            return new AddOutcomeResult.IdempotencyConflict(operationId.ToString());
        }

        var stored = await elements.FindOutcomeAsync(projectId, existing.ElementId, cancellationToken) ??
            throw new InvalidOperationException("A committed outcome change set referenced no outcome.");
        return new AddOutcomeResult.Added(
            ModelApplicationMapping.Outcome(stored),
            existing.ResultRevision.Value,
            "Review the project model and its evidence.");
    }

    private async ValueTask<AddOutcomeResult> ReloadOperationAsync(
        ChangeSetId operationId,
        string fingerprint,
        ProjectId projectId,
        CancellationToken cancellationToken)
    {
        var existing = await elements.FindCommitByOperationAsync(operationId, cancellationToken);
        return existing is null
            ? throw new InvalidOperationException("An operation conflict could not be reloaded.")
            : await ExistingAsync(existing, operationId, fingerprint, projectId, cancellationToken);
    }

    private static AddOutcomeResult.Added Added(
        OutcomeDefinition outcome,
        RelationId relationId,
        ActorDefinition beneficiary,
        Revision revision) =>
        new(
            ModelApplicationMapping.Outcome(new StoredOutcome(outcome, relationId, beneficiary.Id, beneficiary.Name.Value)),
            revision.Value,
            "Review the project model and its evidence.");

    private static ValidatedOutcome Validate(AddOutcomeCommand command)
    {
        var errors = new List<SemanticError>();
        var projectId = ModelInputValidation.Accept(ProjectId.Parse(command.ProjectId), errors);
        var expected = ModelInputValidation.Accept(Revision.Parse(command.ExpectedRevision), errors);
        var operation = ModelInputValidation.Accept(ChangeSetId.Parse(command.OperationId), errors);
        var name = ModelInputValidation.Accept(ElementName.Create(command.Name), errors);
        var statement = ModelInputValidation.Accept(OutcomeStatement.Create(command.Statement), errors);
        var signals = ModelInputValidation.SuccessSignals(command.SuccessSignals, errors);
        var beneficiary = ModelInputValidation.Accept(ElementId.Parse(command.BeneficiaryActorId), errors);
        var knowledgeResult = ModelApplicationMapping.ParseKnowledgeStatus(command.KnowledgeStatus);
        KnowledgeStatus? knowledge = knowledgeResult is SemanticResult<KnowledgeStatus>.Accepted acceptedKnowledge
            ? acceptedKnowledge.Value
            : null;
        if (knowledgeResult is SemanticResult<KnowledgeStatus>.Rejected rejectedKnowledge)
        {
            errors.Add(rejectedKnowledge.Error);
        }
        var reason = ModelInputValidation.Accept(ChangeReason.Create(command.Reason), errors);
        return new(projectId, expected, operation, name, statement, signals, beneficiary, knowledge, reason, errors);
    }

    private sealed record ValidatedOutcome(
        ProjectId? ProjectId,
        Revision? ExpectedRevision,
        ChangeSetId? OperationId,
        ElementName? Name,
        OutcomeStatement? Statement,
        System.Collections.Immutable.ImmutableArray<SuccessSignal> SuccessSignals,
        ElementId? BeneficiaryId,
        KnowledgeStatus? KnowledgeStatus,
        ChangeReason? Reason,
        IReadOnlyList<SemanticError> Errors);
}
