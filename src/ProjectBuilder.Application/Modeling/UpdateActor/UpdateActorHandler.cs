using ProjectBuilder.Application.Projects.CreateProject;
using ProjectBuilder.Domain.Modeling.Elements;
using ProjectBuilder.Domain.Modeling.Primitives;
using ProjectBuilder.Domain.Modeling.Transitions;

namespace ProjectBuilder.Application.Modeling.UpdateActor;

public sealed class UpdateActorHandler(IProjectCreationStore projects, IProjectElementStore elements,
    IProjectEditAuthorizer authorizer, IApplicationClock clock)
{
    public async ValueTask<UpdateActorResult> HandleAsync(UpdateActorCommand command, ProjectActor actor, CancellationToken cancellationToken = default)
    {
        var errors = new List<SemanticError>();
        var projectId = ModelInputValidation.Accept(ProjectId.Parse(command.ProjectId), errors);
        var actorId = ModelInputValidation.Accept(ElementId.Parse(command.ActorId), errors);
        var expected = ModelInputValidation.Accept(Revision.Parse(command.ExpectedRevision), errors);
        var operation = ModelInputValidation.Accept(ChangeSetId.Parse(command.OperationId), errors);
        var name = ModelInputValidation.Accept(ElementName.Create(command.Name), errors);
        var role = ModelInputValidation.Accept(ContextualRole.Create(command.ContextualRole), errors);
        var kindResult = ModelApplicationMapping.ParseActorKind(command.ActorKind);
        var kind = kindResult is SemanticResult<ActorKind>.Accepted acceptedKind ? acceptedKind.Value : (ActorKind?)null;
        if (kindResult is SemanticResult<ActorKind>.Rejected rejectedKind) errors.Add(rejectedKind.Error);
        var knowledgeResult = ModelApplicationMapping.ParseKnowledgeStatus(command.KnowledgeStatus);
        var knowledge = knowledgeResult is SemanticResult<KnowledgeStatus>.Accepted acceptedKnowledge ? acceptedKnowledge.Value : (KnowledgeStatus?)null;
        if (knowledgeResult is SemanticResult<KnowledgeStatus>.Rejected rejectedKnowledge) errors.Add(rejectedKnowledge.Error);
        var goals = ModelInputValidation.ActorStatements(command.Goals, errors, "goals");
        var responsibilities = ModelInputValidation.ActorStatements(command.Responsibilities, errors, "responsibilities");
        var authority = ModelInputValidation.ActorStatements(command.Authority, errors, "authority");
        var constraints = ModelInputValidation.ActorStatements(command.Constraints, errors, "constraints");
        var reason = ModelInputValidation.Accept(ChangeReason.Create(command.Reason), errors);
        if (errors.Count > 0) return new UpdateActorResult.Invalid(errors);
        var project = await projects.FindByIdAsync(projectId!, cancellationToken);
        if (project is null) return new UpdateActorResult.ProjectNotFound(command.ProjectId);
        var access = await authorizer.AuthorizeEditAsync(actor, project.WorkspaceId, cancellationToken);
        if (!access.IsAllowed) return new UpdateActorResult.Denied(access.Reason);
        var fingerprint = ModelRequestFingerprint.Create(command.ProjectId, command.ActorId, command.ExpectedRevision,
            command.Name, command.ActorKind, command.ContextualRole, command.Goals, command.Responsibilities,
            command.Authority, command.Constraints, command.KnowledgeStatus, command.Reason);
        var existing = await elements.FindCommitByOperationAsync(operation!, cancellationToken);
        if (existing is not null) return await ExistingAsync(existing, operation!, fingerprint, project.Id, cancellationToken);
        var current = await elements.FindActorAsync(project.Id, actorId!, cancellationToken);
        if (current is null) return new UpdateActorResult.ActorNotFound(command.ActorId);
        var transition = ProjectElementTransition.UpdateActor(project, expected!, current, name!, role!, kind!.Value,
            goals, responsibilities, authority, constraints, knowledge!.Value, operation!, reason!, clock.GetCurrentTimestamp(), actor.Subject);
        if (transition is UpdateActorTransitionResult.Conflict conflict)
            return new UpdateActorResult.Conflict(conflict.Expected.Value, conflict.Actual.Value, ModelApplicationMapping.Conflicts(conflict.Conflicts));
        var accepted = (UpdateActorTransitionResult.Accepted)transition;
        var stored = await elements.UpdateActorAsync(accepted, fingerprint, cancellationToken);
        return stored switch
        {
            ElementStoreCommitResult.Committed => Updated(accepted.Actor, accepted.Project.Revision),
            ElementStoreCommitResult.RevisionConflict storeConflict => new UpdateActorResult.Conflict(expected!.Value, storeConflict.Actual.Value, ModelApplicationMapping.RevisionConflict(expected!, storeConflict.Actual)),
            ElementStoreCommitResult.OperationConflict => await ReloadAsync(operation!, fingerprint, project.Id, cancellationToken),
            _ => throw new InvalidOperationException("Unknown element store result."),
        };
    }

    private async ValueTask<UpdateActorResult> ExistingAsync(StoredElementCommit existing, ChangeSetId operation, string fingerprint, ProjectId projectId, CancellationToken cancellationToken)
    {
        if (existing.ChangeKind != "actor.updated" || !string.Equals(existing.RequestFingerprint, fingerprint, StringComparison.Ordinal)) return new UpdateActorResult.IdempotencyConflict(operation.ToString());
        var current = await elements.FindActorAsync(projectId, existing.ElementId, cancellationToken) ?? throw new InvalidOperationException("Updated actor was not found.");
        return Updated(current, existing.ResultRevision);
    }
    private async ValueTask<UpdateActorResult> ReloadAsync(ChangeSetId operation, string fingerprint, ProjectId projectId, CancellationToken cancellationToken) =>
        await elements.FindCommitByOperationAsync(operation, cancellationToken) is { } existing ? await ExistingAsync(existing, operation, fingerprint, projectId, cancellationToken) : throw new InvalidOperationException("Operation conflict could not be reloaded.");
    private static UpdateActorResult.Updated Updated(ActorDefinition value, Revision revision) => new(ModelApplicationMapping.Actor(value), revision.Value, "Review the updated actor and affected outcomes.");
}
