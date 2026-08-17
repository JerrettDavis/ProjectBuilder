using ProjectBuilder.Application.Projects.CreateProject;
using ProjectBuilder.Domain.Modeling.Elements;
using ProjectBuilder.Domain.Modeling.Primitives;
using ProjectBuilder.Domain.Modeling.Transitions;

namespace ProjectBuilder.Application.Modeling.AddActor;

public sealed class AddActorHandler(
    IProjectCreationStore projects,
    IProjectElementStore elements,
    IProjectEditAuthorizer authorizer,
    IModelIdentitySource identities,
    IApplicationClock clock)
{
    public async ValueTask<AddActorResult> HandleAsync(
        AddActorCommand command,
        ProjectActor actor,
        CancellationToken cancellationToken = default)
    {
        var validation = Validate(command);
        if (validation.Errors.Count > 0)
        {
            return new AddActorResult.Invalid(validation.Errors);
        }

        var project = await projects.FindByIdAsync(validation.ProjectId!, cancellationToken);
        if (project is null)
        {
            return new AddActorResult.ProjectNotFound(command.ProjectId);
        }

        var access = await authorizer.AuthorizeEditAsync(actor, project.WorkspaceId, cancellationToken);
        if (!access.IsAllowed)
        {
            return new AddActorResult.Denied(access.Reason);
        }

        var fingerprint = ModelRequestFingerprint.Create(
            command.ProjectId,
            command.ExpectedRevision,
            command.Name,
            command.ActorKind,
            command.ContextualRole,
            command.Goals,
            command.Responsibilities,
            command.Authority,
            command.Constraints,
            command.KnowledgeStatus,
            command.Reason);
        var existing = await elements.FindCommitByOperationAsync(validation.OperationId!, cancellationToken);
        if (existing is not null)
        {
            return await ExistingAsync(existing, validation.OperationId!, fingerprint, project.Id, cancellationToken);
        }

        var order = await elements.NextElementOrderAsync(project.Id, cancellationToken);
        var transitioned = ProjectElementTransition.AddActor(
            project,
            validation.ExpectedRevision!,
            identities.NextElementId(),
            validation.Name!,
            validation.ContextualRole!,
            validation.ActorKind!.Value,
            validation.Goals,
            validation.Responsibilities,
            validation.Authority,
            validation.Constraints,
            order,
            validation.OperationId!,
            validation.Reason!,
            clock.GetCurrentTimestamp(),
            actor.Subject,
            validation.KnowledgeStatus!.Value);

        if (transitioned is AddActorTransitionResult.Conflict conflict)
        {
            return new AddActorResult.Conflict(
                conflict.Expected.Value, conflict.Actual.Value,
                ModelApplicationMapping.Conflicts(conflict.Conflicts));
        }

        var accepted = (AddActorTransitionResult.Accepted)transitioned;
        var stored = await elements.CommitActorAsync(accepted, fingerprint, cancellationToken);
        return stored switch
        {
            ElementStoreCommitResult.Committed => Added(accepted.Actor, accepted.Project.Revision),
            ElementStoreCommitResult.RevisionConflict storeConflict =>
                new AddActorResult.Conflict(
                    validation.ExpectedRevision!.Value, storeConflict.Actual.Value,
                    ModelApplicationMapping.RevisionConflict(validation.ExpectedRevision!, storeConflict.Actual)),
            ElementStoreCommitResult.OperationConflict =>
                await ReloadOperationAsync(validation.OperationId!, fingerprint, project.Id, cancellationToken),
            _ => throw new InvalidOperationException("Unknown element store result."),
        };
    }

    private async ValueTask<AddActorResult> ExistingAsync(
        StoredElementCommit existing,
        ChangeSetId operationId,
        string fingerprint,
        ProjectId projectId,
        CancellationToken cancellationToken)
    {
        if (existing.ChangeKind != "actor.added" ||
            !string.Equals(existing.RequestFingerprint, fingerprint, StringComparison.Ordinal))
        {
            return new AddActorResult.IdempotencyConflict(operationId.ToString());
        }

        var actor = await elements.FindActorAsync(projectId, existing.ElementId, cancellationToken) ??
            throw new InvalidOperationException("A committed actor change set referenced no actor.");
        return Added(actor, existing.ResultRevision);
    }

    private async ValueTask<AddActorResult> ReloadOperationAsync(
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

    private static AddActorResult.Added Added(ActorDefinition actor, Revision revision) =>
        new(ModelApplicationMapping.Actor(actor), revision.Value, "Define an observable outcome for this actor.");

    private static ValidatedActor Validate(AddActorCommand command)
    {
        var errors = new List<SemanticError>();
        var projectId = ModelInputValidation.Accept(ProjectId.Parse(command.ProjectId), errors);
        var expected = ModelInputValidation.Accept(Revision.Parse(command.ExpectedRevision), errors);
        var operation = ModelInputValidation.Accept(ChangeSetId.Parse(command.OperationId), errors);
        var name = ModelInputValidation.Accept(ElementName.Create(command.Name), errors);
        var kindResult = ModelApplicationMapping.ParseActorKind(command.ActorKind);
        ActorKind? kind = kindResult is SemanticResult<ActorKind>.Accepted acceptedKind
            ? acceptedKind.Value
            : null;
        if (kindResult is SemanticResult<ActorKind>.Rejected rejectedKind)
        {
            errors.Add(rejectedKind.Error);
        }

        var role = ModelInputValidation.Accept(ContextualRole.Create(command.ContextualRole), errors);
        var goals = ModelInputValidation.ActorStatements(command.Goals, errors, "goals");
        var responsibilities = ModelInputValidation.ActorStatements(command.Responsibilities, errors, "responsibilities");
        var authority = ModelInputValidation.ActorStatements(command.Authority, errors, "authority");
        var constraints = ModelInputValidation.ActorStatements(command.Constraints, errors, "constraints");
        var knowledgeResult = ModelApplicationMapping.ParseKnowledgeStatus(command.KnowledgeStatus);
        KnowledgeStatus? knowledge = knowledgeResult is SemanticResult<KnowledgeStatus>.Accepted acceptedKnowledge
            ? acceptedKnowledge.Value
            : null;
        if (knowledgeResult is SemanticResult<KnowledgeStatus>.Rejected rejectedKnowledge)
        {
            errors.Add(rejectedKnowledge.Error);
        }
        var reason = ModelInputValidation.Accept(ChangeReason.Create(command.Reason), errors);
        return new(projectId, expected, operation, name, kind, role, goals, responsibilities, authority, constraints, knowledge, reason, errors);
    }

    private sealed record ValidatedActor(
        ProjectId? ProjectId,
        Revision? ExpectedRevision,
        ChangeSetId? OperationId,
        ElementName? Name,
        ActorKind? ActorKind,
        ContextualRole? ContextualRole,
        System.Collections.Immutable.ImmutableArray<ActorStatement> Goals,
        System.Collections.Immutable.ImmutableArray<ActorStatement> Responsibilities,
        System.Collections.Immutable.ImmutableArray<ActorStatement> Authority,
        System.Collections.Immutable.ImmutableArray<ActorStatement> Constraints,
        KnowledgeStatus? KnowledgeStatus,
        ChangeReason? Reason,
        IReadOnlyList<SemanticError> Errors);
}
