using System.Collections.Immutable;
using ProjectBuilder.Domain.Modeling.Gaps;
using ProjectBuilder.Domain.Modeling.Primitives;
using ProjectBuilder.Domain.Projects;

namespace ProjectBuilder.Domain.Modeling.Transitions;

public abstract record RecordGapDispositionTransitionResult
{
    private RecordGapDispositionTransitionResult() { }
    public sealed record Accepted(ProjectDefinition Project, GapDispositionDefinition Disposition, ProjectModelChangeSet ChangeSet) : RecordGapDispositionTransitionResult;
    public sealed record Invalid(ImmutableArray<SemanticError> Errors) : RecordGapDispositionTransitionResult;
    public sealed record Conflict(Revision Expected, Revision Actual, ImmutableArray<SemanticConflict> Conflicts) : RecordGapDispositionTransitionResult;
}

public static class GapDispositionTransition
{
    public static RecordGapDispositionTransitionResult Record(
        ProjectDefinition project, Revision expectedRevision, GapDispositionId id,
        string profileId, string ruleCode, ElementId scopeId, GapDispositionKind kind,
        string rationale, string consequence, ElementId authorityActorId,
        string? reviewOn, string? targetMilestone, ChangeSetId operationId,
        ChangeReason reason, UtcTimestamp occurredAt, string createdBy)
    {
        if (project.Revision != expectedRevision)
            return new RecordGapDispositionTransitionResult.Conflict(expectedRevision, project.Revision, ProjectChangeSetTransition.RevisionConflicts(expectedRevision, project.Revision));

        var errors = ImmutableArray.CreateBuilder<SemanticError>();
        if (!GapDispositionDefinition.IsValidProfile(profileId)) errors.Add(new("gap.profile.invalid", "Gap disposition profile must be discovery or implementation-ready."));
        if (!GapDispositionDefinition.IsValidRuleCode(ruleCode)) errors.Add(new("gap.rule.invalid", "A stable PB rule code is required."));
        var acceptedRationale = Description.Create(rationale);
        if (acceptedRationale is SemanticResult<Description>.Rejected rejectedRationale) errors.Add(new("gap.rationale.required", rejectedRationale.Error.Message));
        var acceptedConsequence = Description.Create(consequence);
        if (acceptedConsequence is SemanticResult<Description>.Rejected rejectedConsequence) errors.Add(new("gap.consequence.required", rejectedConsequence.Error.Message));
        if (!GapDispositionDefinition.IsValidDate(reviewOn)) errors.Add(new("gap.review_on.invalid", "Review date must use yyyy-MM-dd."));
        if (GapDispositionDefinition.IsValidDate(reviewOn) && reviewOn is not null &&
            DateOnly.ParseExact(reviewOn, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture) <= DateOnly.FromDateTime(occurredAt.Value.UtcDateTime))
            errors.Add(new("gap.review_on.expired", "Review or expiration date must be after the disposition is recorded."));
        if (kind is GapDispositionKind.Assumed or GapDispositionKind.Deferred or GapDispositionKind.AcceptedRisk && reviewOn is null)
            errors.Add(new("gap.review_on.required", $"{Label(kind)} requires a review or expiration date."));
        if (kind == GapDispositionKind.Deferred && string.IsNullOrWhiteSpace(targetMilestone))
            errors.Add(new("gap.target_milestone.required", "Deferred disposition requires a target milestone."));
        if (errors.Count > 0) return new RecordGapDispositionTransitionResult.Invalid(errors.ToImmutable());

        var disposition = new GapDispositionDefinition(id, project.Id, profileId, ruleCode, scopeId, kind,
            ((SemanticResult<Description>.Accepted)acceptedRationale).Value,
            ((SemanticResult<Description>.Accepted)acceptedConsequence).Value,
            authorityActorId, reviewOn, string.IsNullOrWhiteSpace(targetMilestone) ? null : targetMilestone.Trim(), occurredAt, createdBy);
        var draft = new DraftProjectChangeSet(operationId, scopeId, "gap.disposition.recorded", reason,
            [new ProjectChangeOperation.GapDispositionRecorded(0, id, scopeId, ruleCode, kind)]);
        var committed = ProjectChangeSetTransition.Commit(project, expectedRevision, draft, occurredAt, createdBy);
        return committed switch
        {
            ProjectChangeSetTransitionResult.Accepted accepted => new RecordGapDispositionTransitionResult.Accepted(accepted.Project, disposition, accepted.ChangeSet),
            ProjectChangeSetTransitionResult.Conflict conflict => new RecordGapDispositionTransitionResult.Conflict(conflict.Expected, conflict.Actual, conflict.Conflicts),
            ProjectChangeSetTransitionResult.Invalid invalid => new RecordGapDispositionTransitionResult.Invalid(invalid.Errors),
            _ => throw new InvalidOperationException("Unknown change-set result."),
        };
    }

    private static string Label(GapDispositionKind kind) => kind switch { GapDispositionKind.AcceptedRisk => "Accepted risk", _ => kind.ToString() };
}
