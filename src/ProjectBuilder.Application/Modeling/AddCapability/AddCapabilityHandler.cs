using System.Collections.Immutable;
using ProjectBuilder.Application.Projects.CreateProject;
using ProjectBuilder.Domain.Modeling.Elements;
using ProjectBuilder.Domain.Modeling.Primitives;
using ProjectBuilder.Domain.Modeling.Transitions;

namespace ProjectBuilder.Application.Modeling.AddCapability;

public sealed class AddCapabilityHandler(
    IProjectCreationStore projects, IProjectElementStore elements, IProjectEditAuthorizer authorizer,
    IModelIdentitySource identities, IApplicationClock clock)
{
    public async ValueTask<AddCapabilityResult> HandleAsync(
        AddCapabilityCommand command, ProjectActor actor, CancellationToken cancellationToken = default)
    {
        var validation = Validate(command);
        if (validation.Errors.Count > 0) return new AddCapabilityResult.Invalid(validation.Errors);
        var project = await projects.FindByIdAsync(validation.ProjectId!, cancellationToken);
        if (project is null) return new AddCapabilityResult.ProjectNotFound(command.ProjectId);
        var access = await authorizer.AuthorizeEditAsync(actor, project.WorkspaceId, cancellationToken);
        if (!access.IsAllowed) return new AddCapabilityResult.Denied(access.Reason);
        var fingerprint = ModelRequestFingerprint.Create(command.ProjectId, command.ExpectedRevision, command.Name,
            command.Ability, string.Join(',', command.OutcomeIds.Order(StringComparer.Ordinal)), command.Priority,
            command.KnowledgeStatus, command.Reason);
        var existing = await elements.FindCommitByOperationAsync(validation.OperationId!, cancellationToken);
        if (existing is not null) return await ExistingAsync(existing, validation.OperationId!, fingerprint, project.Id, cancellationToken);
        var model = await elements.LoadModelAsync(project.Id, cancellationToken);
        var knownOutcomes = model.Outcomes.Select(item => item.Outcome.Id).ToHashSet();
        var missing = validation.OutcomeIds.FirstOrDefault(id => !knownOutcomes.Contains(id));
        if (missing is not null) return new AddCapabilityResult.OutcomeNotFound(missing.ToString());
        var transitioned = ProjectElementTransition.AddCapability(project, validation.ExpectedRevision!, identities.NextElementId(),
            validation.Name!, validation.Ability!, validation.OutcomeIds, validation.Priority!.Value,
            await elements.NextElementOrderAsync(project.Id, cancellationToken), validation.OperationId!, validation.Reason!,
            clock.GetCurrentTimestamp(), actor.Subject, validation.KnowledgeStatus!.Value);
        if (transitioned is AddCapabilityTransitionResult.Conflict conflict)
            return new AddCapabilityResult.Conflict(conflict.Expected.Value, conflict.Actual.Value, ModelApplicationMapping.Conflicts(conflict.Conflicts));
        var accepted = (AddCapabilityTransitionResult.Accepted)transitioned;
        return await elements.CommitCapabilityAsync(accepted, fingerprint, cancellationToken) switch
        {
            ElementStoreCommitResult.Committed => Added(accepted.Capability, accepted.Project.Revision),
            ElementStoreCommitResult.RevisionConflict storeConflict => new AddCapabilityResult.Conflict(validation.ExpectedRevision!.Value,
                storeConflict.Actual.Value, ModelApplicationMapping.RevisionConflict(validation.ExpectedRevision!, storeConflict.Actual)),
            ElementStoreCommitResult.OperationConflict => await ReloadAsync(validation.OperationId!, fingerprint, project.Id, cancellationToken),
            _ => throw new InvalidOperationException("Unknown element store result."),
        };
    }

    private async ValueTask<AddCapabilityResult> ExistingAsync(StoredElementCommit existing, ChangeSetId operationId,
        string fingerprint, ProjectId projectId, CancellationToken cancellationToken)
    {
        if (existing.ChangeKind != "capability.added" || existing.RequestFingerprint != fingerprint)
            return new AddCapabilityResult.IdempotencyConflict(operationId.ToString());
        var capability = await elements.FindCapabilityAsync(projectId, existing.ElementId, cancellationToken)
            ?? throw new InvalidOperationException("A committed capability change set referenced no capability.");
        return Added(capability, existing.ResultRevision);
    }

    private async ValueTask<AddCapabilityResult> ReloadAsync(ChangeSetId operationId, string fingerprint,
        ProjectId projectId, CancellationToken cancellationToken)
    {
        var existing = await elements.FindCommitByOperationAsync(operationId, cancellationToken);
        return existing is null ? throw new InvalidOperationException("An operation conflict could not be reloaded.")
            : await ExistingAsync(existing, operationId, fingerprint, projectId, cancellationToken);
    }

    private static AddCapabilityResult.Added Added(CapabilityDefinition value, Revision revision) =>
        new(ModelApplicationMapping.Capability(value), revision.Value, "Map an episode that exercises this capability.");

    private static ValidatedCapability Validate(AddCapabilityCommand command)
    {
        var errors = new List<SemanticError>();
        var projectId = ModelInputValidation.Accept(ProjectId.Parse(command.ProjectId), errors);
        var revision = ModelInputValidation.Accept(Revision.Parse(command.ExpectedRevision), errors);
        var operation = ModelInputValidation.Accept(ChangeSetId.Parse(command.OperationId), errors);
        var name = ModelInputValidation.Accept(ElementName.Create(command.Name), errors);
        var ability = ModelInputValidation.Accept(Description.Create(command.Ability), errors);
        var reason = ModelInputValidation.Accept(ChangeReason.Create(command.Reason), errors);
        var outcomeIds = ImmutableArray.CreateBuilder<ElementId>();
        foreach (var text in command.OutcomeIds.Distinct(StringComparer.Ordinal))
        {
            var id = ModelInputValidation.Accept(ElementId.Parse(text), errors);
            if (id is not null) outcomeIds.Add(id);
        }
        if (outcomeIds.Count == 0) errors.Add(new("capability.outcome.required", "Select at least one outcome this capability contributes to."));
        CapabilityPriority? priority = Enum.TryParse<CapabilityPriority>(command.Priority, true, out var parsedPriority) ? parsedPriority : null;
        if (priority is null) errors.Add(new("capability.priority.invalid", "Select Critical, High, Normal, or Low priority."));
        var knowledgeResult = ModelApplicationMapping.ParseKnowledgeStatus(command.KnowledgeStatus);
        KnowledgeStatus? knowledge = knowledgeResult is SemanticResult<KnowledgeStatus>.Accepted accepted ? accepted.Value : null;
        if (knowledgeResult is SemanticResult<KnowledgeStatus>.Rejected rejected) errors.Add(rejected.Error);
        return new(projectId, revision, operation, name, ability, outcomeIds.ToImmutable(), priority, knowledge, reason, errors);
    }

    private sealed record ValidatedCapability(ProjectId? ProjectId, Revision? ExpectedRevision, ChangeSetId? OperationId,
        ElementName? Name, Description? Ability, ImmutableArray<ElementId> OutcomeIds, CapabilityPriority? Priority,
        KnowledgeStatus? KnowledgeStatus, ChangeReason? Reason, IReadOnlyList<SemanticError> Errors);
}
