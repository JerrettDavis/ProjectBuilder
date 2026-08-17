using ProjectBuilder.Application.Modeling;
using ProjectBuilder.Application.Projects.CreateProject;
using ProjectBuilder.Application.Validation.GetProjectFindings;
using ProjectBuilder.Domain.Modeling.Gaps;
using ProjectBuilder.Domain.Modeling.Primitives;
using ProjectBuilder.Domain.Modeling.Transitions;

namespace ProjectBuilder.Application.Validation.RecordGapDisposition;

public sealed class RecordGapDispositionHandler(
    IProjectCreationStore projects, IProjectElementStore elements, IProjectEditAuthorizer authorizer,
    IModelIdentitySource identities, IApplicationClock clock, GetProjectFindingsHandler findings)
{
    public async ValueTask<RecordGapDispositionResult> HandleAsync(
        RecordGapDispositionCommand command, ProjectActor actor, CancellationToken cancellationToken = default)
    {
        var errors = new List<SemanticError>();
        var projectId = ModelInputValidation.Accept(ProjectId.Parse(command.ProjectId), errors);
        var expected = ModelInputValidation.Accept(Revision.Parse(command.ExpectedRevision), errors);
        var operation = ModelInputValidation.Accept(ChangeSetId.Parse(command.OperationId), errors);
        var scope = ModelInputValidation.Accept(ElementId.Parse(command.ScopeId), errors);
        var authority = ModelInputValidation.Accept(ElementId.Parse(command.AuthorityActorId), errors);
        var reason = ModelInputValidation.Accept(ChangeReason.Create(command.Reason), errors);
        if (!Enum.TryParse<GapDispositionKind>(command.Disposition, true, out var kind))
            errors.Add(new("gap.disposition.invalid", "Disposition must be Assumed, Deferred, AcceptedRisk, or NotApplicable."));
        if (errors.Count > 0) return new RecordGapDispositionResult.Invalid(errors);

        var project = await projects.FindByIdAsync(projectId!, cancellationToken);
        if (project is null) return new RecordGapDispositionResult.ProjectNotFound(command.ProjectId);
        var access = await authorizer.AuthorizeEditAsync(actor, project.WorkspaceId, cancellationToken);
        if (!access.IsAllowed) return new RecordGapDispositionResult.Denied(access.Reason);

        var model = await elements.LoadModelAsync(project.Id, cancellationToken);
        if (model.Actors.All(item => item.Id != authority))
            return new RecordGapDispositionResult.ReferenceNotFound("authority actor");
        var evaluated = await findings.HandleAsync(command.ProjectId, command.ProfileId, cancellationToken);
        if (evaluated is not GetProjectFindingsResult.Found found ||
            found.Overview.Findings.All(item => item.Code != command.RuleCode || item.ScopeId != command.ScopeId))
            return new RecordGapDispositionResult.FindingNotFound(command.RuleCode, command.ScopeId, command.ProfileId);

        var fingerprint = ModelRequestFingerprint.Create(
            command.ProjectId, command.ExpectedRevision, command.ProfileId, command.RuleCode, command.ScopeId,
            command.Disposition, command.Rationale, command.Consequence, command.AuthorityActorId,
            command.ReviewOn ?? string.Empty, command.TargetMilestone ?? string.Empty, command.Reason);
        var prior = await elements.FindCommitByOperationAsync(operation!, cancellationToken);
        if (prior is not null)
        {
            if (prior.ChangeKind != "gap.disposition.recorded" ||
                !string.Equals(prior.RequestFingerprint, fingerprint, StringComparison.Ordinal))
                return new RecordGapDispositionResult.IdempotencyConflict(operation!.ToString());
            var existing = model.GapDispositions.SingleOrDefault(item => item.ProfileId == command.ProfileId &&
                item.RuleCode == command.RuleCode && item.ScopeId == command.ScopeId);
            return existing is null
                ? throw new InvalidOperationException("The committed disposition could not be reloaded.")
                : new RecordGapDispositionResult.Recorded(existing, prior.ResultRevision.Value);
        }

        var transitioned = GapDispositionTransition.Record(project, expected!, GapDispositionId.From(identities.NextElementId()),
            command.ProfileId, command.RuleCode, scope!, kind, command.Rationale, command.Consequence, authority!,
            command.ReviewOn, command.TargetMilestone, operation!, reason!, clock.GetCurrentTimestamp(), actor.Subject);
        if (transitioned is RecordGapDispositionTransitionResult.Invalid invalid)
            return new RecordGapDispositionResult.Invalid(invalid.Errors);
        if (transitioned is RecordGapDispositionTransitionResult.Conflict conflict)
            return new RecordGapDispositionResult.Conflict(conflict.Expected.Value, conflict.Actual.Value,
                ModelApplicationMapping.Conflicts(conflict.Conflicts));
        var accepted = (RecordGapDispositionTransitionResult.Accepted)transitioned;
        var stored = await elements.CommitGapDispositionAsync(accepted, fingerprint, cancellationToken);
        if (stored is ElementStoreCommitResult.RevisionConflict storeConflict)
            return new RecordGapDispositionResult.Conflict(expected!.Value, storeConflict.Actual.Value,
                ModelApplicationMapping.RevisionConflict(expected, storeConflict.Actual));
        if (stored is ElementStoreCommitResult.OperationConflict)
            return new RecordGapDispositionResult.IdempotencyConflict(command.OperationId);
        var authorityName = model.Actors.Single(item => item.Id == authority).Name.Value;
        var disposition = accepted.Disposition;
        return new RecordGapDispositionResult.Recorded(new(
            disposition.Id.ToString(), disposition.ProfileId, disposition.RuleCode, disposition.ScopeId.ToString(),
            disposition.Disposition.ToString(), disposition.Rationale.Value, disposition.Consequence.Value,
            disposition.AuthorityActorId.ToString(), authorityName, disposition.ReviewOn, disposition.TargetMilestone,
            disposition.CreatedAt.ToString(), disposition.CreatedBy), accepted.Project.Revision.Value);
    }
}
