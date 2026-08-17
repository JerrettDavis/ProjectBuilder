using ProjectBuilder.Application.Projects.CreateProject;
using ProjectBuilder.Domain.Modeling.Elements;
using ProjectBuilder.Domain.Modeling.Primitives;
using ProjectBuilder.Domain.Modeling.Relations;
using ProjectBuilder.Domain.Modeling.Transitions;

namespace ProjectBuilder.Application.Modeling.UpdateOutcome;

public sealed class UpdateOutcomeHandler(IProjectCreationStore projects, IProjectElementStore elements,
    IProjectEditAuthorizer authorizer, IApplicationClock clock)
{
    public async ValueTask<UpdateOutcomeResult> HandleAsync(UpdateOutcomeCommand command, ProjectActor actor, CancellationToken cancellationToken = default)
    {
        var errors = new List<SemanticError>();
        var projectId = ModelInputValidation.Accept(ProjectId.Parse(command.ProjectId), errors);
        var outcomeId = ModelInputValidation.Accept(ElementId.Parse(command.OutcomeId), errors);
        var expected = ModelInputValidation.Accept(Revision.Parse(command.ExpectedRevision), errors);
        var operation = ModelInputValidation.Accept(ChangeSetId.Parse(command.OperationId), errors);
        var name = ModelInputValidation.Accept(ElementName.Create(command.Name), errors);
        var statement = ModelInputValidation.Accept(OutcomeStatement.Create(command.Statement), errors);
        var signals = ModelInputValidation.SuccessSignals(command.SuccessSignals, errors);
        var beneficiaryId = ModelInputValidation.Accept(ElementId.Parse(command.BeneficiaryActorId), errors);
        var knowledgeResult = ModelApplicationMapping.ParseKnowledgeStatus(command.KnowledgeStatus);
        var knowledge = knowledgeResult is SemanticResult<KnowledgeStatus>.Accepted acceptedKnowledge ? acceptedKnowledge.Value : (KnowledgeStatus?)null;
        if (knowledgeResult is SemanticResult<KnowledgeStatus>.Rejected rejectedKnowledge) errors.Add(rejectedKnowledge.Error);
        var reason = ModelInputValidation.Accept(ChangeReason.Create(command.Reason), errors);
        if (errors.Count > 0) return new UpdateOutcomeResult.Invalid(errors);
        var project = await projects.FindByIdAsync(projectId!, cancellationToken);
        if (project is null) return new UpdateOutcomeResult.ProjectNotFound(command.ProjectId);
        var access = await authorizer.AuthorizeEditAsync(actor, project.WorkspaceId, cancellationToken);
        if (!access.IsAllowed) return new UpdateOutcomeResult.Denied(access.Reason);
        var fingerprint = ModelRequestFingerprint.Create(command.ProjectId, command.OutcomeId, command.ExpectedRevision,
            command.Name, command.Statement, command.SuccessSignals, command.BeneficiaryActorId, command.KnowledgeStatus, command.Reason);
        var existing = await elements.FindCommitByOperationAsync(operation!, cancellationToken);
        if (existing is not null) return await ExistingAsync(existing, operation!, fingerprint, project.Id, cancellationToken);
        var current = await elements.FindOutcomeAsync(project.Id, outcomeId!, cancellationToken);
        if (current is null) return new UpdateOutcomeResult.OutcomeNotFound(command.OutcomeId);
        var beneficiary = await elements.FindActorAsync(project.Id, beneficiaryId!, cancellationToken);
        if (beneficiary is null) return new UpdateOutcomeResult.BeneficiaryNotFound(command.BeneficiaryActorId);
        var relation = ((SemanticResult<ModelRelationDefinition>.Accepted)ModelRelationRegistry.Create(
            current.BeneficiaryRelationId, project.Id, ModelRelationKind.BenefitsFrom, current.BeneficiaryActorId,
            ModelElementKind.Actor, current.Outcome.Id, ModelElementKind.Outcome, current.Outcome.CreatedAt, current.Outcome.CreatedBy)).Value;
        var transition = ProjectElementTransition.UpdateOutcome(project, expected!, current.Outcome, relation, name!, statement!, signals,
            beneficiary, knowledge!.Value, operation!, reason!, clock.GetCurrentTimestamp(), actor.Subject);
        if (transition is UpdateOutcomeTransitionResult.Conflict conflict)
            return new UpdateOutcomeResult.Conflict(conflict.Expected.Value, conflict.Actual.Value, ModelApplicationMapping.Conflicts(conflict.Conflicts));
        var accepted = (UpdateOutcomeTransitionResult.Accepted)transition;
        var stored = await elements.UpdateOutcomeAsync(accepted, fingerprint, cancellationToken);
        return stored switch
        {
            ElementStoreCommitResult.Committed => Updated(accepted.Outcome, accepted.Beneficiary.Id, beneficiary, accepted.Project.Revision),
            ElementStoreCommitResult.RevisionConflict storeConflict => new UpdateOutcomeResult.Conflict(expected!.Value, storeConflict.Actual.Value, ModelApplicationMapping.RevisionConflict(expected!, storeConflict.Actual)),
            ElementStoreCommitResult.OperationConflict => await ReloadAsync(operation!, fingerprint, project.Id, cancellationToken),
            _ => throw new InvalidOperationException("Unknown element store result."),
        };
    }
    private async ValueTask<UpdateOutcomeResult> ExistingAsync(StoredElementCommit existing, ChangeSetId operation, string fingerprint, ProjectId projectId, CancellationToken cancellationToken)
    {
        if (existing.ChangeKind != "outcome.updated" || !string.Equals(existing.RequestFingerprint, fingerprint, StringComparison.Ordinal)) return new UpdateOutcomeResult.IdempotencyConflict(operation.ToString());
        var current = await elements.FindOutcomeAsync(projectId, existing.ElementId, cancellationToken) ?? throw new InvalidOperationException("Updated outcome was not found.");
        return new UpdateOutcomeResult.Updated(ModelApplicationMapping.Outcome(current), existing.ResultRevision.Value, "Review the updated outcome and relation.");
    }
    private async ValueTask<UpdateOutcomeResult> ReloadAsync(ChangeSetId operation, string fingerprint, ProjectId projectId, CancellationToken cancellationToken) =>
        await elements.FindCommitByOperationAsync(operation, cancellationToken) is { } existing ? await ExistingAsync(existing, operation, fingerprint, projectId, cancellationToken) : throw new InvalidOperationException("Operation conflict could not be reloaded.");
    private static UpdateOutcomeResult.Updated Updated(OutcomeDefinition value, RelationId relationId, ActorDefinition beneficiary, Revision revision) =>
        new(ModelApplicationMapping.Outcome(new(value, relationId, beneficiary.Id, beneficiary.Name.Value)), revision.Value, "Review the updated outcome and relation.");
}
