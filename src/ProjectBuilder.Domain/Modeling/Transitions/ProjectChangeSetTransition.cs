using System.Collections.Immutable;
using ProjectBuilder.Domain.Modeling.Elements;
using ProjectBuilder.Domain.Modeling.Gaps;
using ProjectBuilder.Domain.Modeling.Primitives;
using ProjectBuilder.Domain.Modeling.Relations;
using ProjectBuilder.Domain.Modeling.Traceability;
using ProjectBuilder.Domain.Projects;

namespace ProjectBuilder.Domain.Modeling.Transitions;

public sealed record SemanticConflict(string Code, string Message, Revision Expected, Revision Actual);

public abstract record ProjectChangeOperation(int Sequence)
{
    public sealed record ProjectCreated(int Sequence, ProjectId ProjectId, ElementName Name)
        : ProjectChangeOperation(Sequence);

    public sealed record ElementAdded(
        int Sequence,
        ElementId ElementId,
        ModelElementKind ElementKind,
        ElementName Name)
        : ProjectChangeOperation(Sequence);

    public sealed record ElementUpdated(
        int Sequence,
        ElementId ElementId,
        ModelElementKind ElementKind,
        ElementName PreviousName,
        ElementName Name)
        : ProjectChangeOperation(Sequence);

    public sealed record RelationAdded(
        int Sequence,
        RelationId RelationId,
        ModelRelationKind RelationKind,
        ElementId SourceElementId,
        ElementId TargetElementId)
        : ProjectChangeOperation(Sequence);

    public sealed record RelationUpdated(
        int Sequence,
        RelationId RelationId,
        ModelRelationKind RelationKind,
        ElementId PreviousSourceElementId,
        ElementId SourceElementId,
        ElementId TargetElementId)
        : ProjectChangeOperation(Sequence);

    public sealed record GapDispositionRecorded(
        int Sequence,
        GapDispositionId DispositionId,
        ElementId ScopeId,
        string RuleCode,
        GapDispositionKind Disposition)
        : ProjectChangeOperation(Sequence);

    public sealed record ClaimAdded(int Sequence, ClaimId ClaimId, ElementId ScopeId, ClaimKind ClaimKind)
        : ProjectChangeOperation(Sequence);

    public sealed record EvidenceAdded(int Sequence, EvidenceId EvidenceId, ClaimId ClaimId, EvidenceKind EvidenceKind)
        : ProjectChangeOperation(Sequence);
}

public sealed record DraftProjectChangeSet(
    ChangeSetId Id,
    ElementId PrimaryElementId,
    string ChangeKind,
    ChangeReason Reason,
    ImmutableArray<ProjectChangeOperation> Operations);

public sealed record ProjectModelChangeSet(
    ChangeSetId Id,
    ProjectId ProjectId,
    Revision BaseRevision,
    Revision ResultRevision,
    ElementId ChangedElementId,
    string ChangeKind,
    ChangeReason Reason,
    UtcTimestamp OccurredAt,
    string CreatedBy,
    ImmutableArray<ProjectChangeOperation> Operations);

public abstract record ProjectChangeSetTransitionResult
{
    private ProjectChangeSetTransitionResult() { }

    public sealed record Accepted(ProjectDefinition Project, ProjectModelChangeSet ChangeSet)
        : ProjectChangeSetTransitionResult;

    public sealed record Conflict(
        Revision Expected,
        Revision Actual,
        ImmutableArray<SemanticConflict> Conflicts)
        : ProjectChangeSetTransitionResult;

    public sealed record Invalid(ImmutableArray<SemanticError> Errors)
        : ProjectChangeSetTransitionResult;
}

public static class ProjectChangeSetTransition
{
    public static ProjectChangeSetTransitionResult Commit(
        ProjectDefinition project,
        Revision expectedRevision,
        DraftProjectChangeSet draft,
        UtcTimestamp occurredAt,
        string createdBy)
    {
        if (project.Revision != expectedRevision)
        {
            return new ProjectChangeSetTransitionResult.Conflict(
                expectedRevision,
                project.Revision,
                RevisionConflicts(expectedRevision, project.Revision));
        }

        var errors = Validate(draft);
        if (errors.Length > 0)
            return new ProjectChangeSetTransitionResult.Invalid(errors);

        var next = ((SemanticResult<Revision>.Accepted)project.Revision.Next()).Value;
        var changeSet = new ProjectModelChangeSet(
            draft.Id,
            project.Id,
            project.Revision,
            next,
            draft.PrimaryElementId,
            draft.ChangeKind,
            draft.Reason,
            occurredAt,
            createdBy,
            draft.Operations);
        return new ProjectChangeSetTransitionResult.Accepted(project.AtRevision(next), changeSet);
    }

    public static ImmutableArray<ProjectChangeOperation> AddedElements(IEnumerable<ModelElement> elements) =>
        elements.Select((element, sequence) => new ProjectChangeOperation.ElementAdded(
            sequence,
            element.Id,
            ElementKind(element),
            element.Name) as ProjectChangeOperation).ToImmutableArray();

    public static ImmutableArray<SemanticConflict> RevisionConflicts(Revision expected, Revision actual) =>
        [new(
            "project.revision.conflict",
            $"Expected revision {expected.Value}; actual revision is {actual.Value}.",
            expected,
            actual)];

    private static ImmutableArray<SemanticError> Validate(DraftProjectChangeSet draft)
    {
        var errors = ImmutableArray.CreateBuilder<SemanticError>();
        if (string.IsNullOrWhiteSpace(draft.ChangeKind))
            errors.Add(new("PB-CHANGE-001", "A change set requires an intention-revealing change kind."));
        if (draft.Operations.IsDefaultOrEmpty)
            errors.Add(new("PB-CHANGE-001", "A change set requires at least one typed operation."));
        else
        {
            if (!draft.Operations.Select(operation => operation.Sequence).SequenceEqual(
                    Enumerable.Range(0, draft.Operations.Length)))
                errors.Add(new("PB-CHANGE-002", "Change-set operation sequence must be contiguous and deterministic."));

            var elementOperations = draft.Operations.OfType<ProjectChangeOperation.ElementAdded>().ToArray();
            if (elementOperations.Select(operation => operation.ElementId).Distinct().Count() != elementOperations.Length)
                errors.Add(new("PB-CHANGE-002", "A change set cannot add the same element more than once."));
            var updatedElementOperations = draft.Operations.OfType<ProjectChangeOperation.ElementUpdated>().ToArray();
            if (updatedElementOperations.Select(operation => operation.ElementId).Distinct().Count() != updatedElementOperations.Length)
                errors.Add(new("PB-CHANGE-002", "A change set cannot update the same element more than once."));
            var dispositionOperations = draft.Operations.OfType<ProjectChangeOperation.GapDispositionRecorded>().ToArray();
            if (dispositionOperations.Select(operation => operation.DispositionId).Distinct().Count() != dispositionOperations.Length)
                errors.Add(new("PB-CHANGE-002", "A change set cannot record the same gap disposition more than once."));
            var claimOperations = draft.Operations.OfType<ProjectChangeOperation.ClaimAdded>().ToArray();
            if (claimOperations.Select(operation => operation.ClaimId).Distinct().Count() != claimOperations.Length)
                errors.Add(new("PB-CHANGE-002", "A change set cannot add the same claim more than once."));
            var evidenceOperations = draft.Operations.OfType<ProjectChangeOperation.EvidenceAdded>().ToArray();
            if (evidenceOperations.Select(operation => operation.EvidenceId).Distinct().Count() != evidenceOperations.Length)
                errors.Add(new("PB-CHANGE-002", "A change set cannot add the same evidence more than once."));
            if (elementOperations.All(operation => operation.ElementId != draft.PrimaryElementId) &&
                updatedElementOperations.All(operation => operation.ElementId != draft.PrimaryElementId) &&
                dispositionOperations.All(operation => operation.ScopeId != draft.PrimaryElementId) &&
                claimOperations.All(operation => operation.ScopeId != draft.PrimaryElementId))
                errors.Add(new("PB-CHANGE-003", "The primary changed element must be one of the typed operations."));

            var relationOperations = draft.Operations.OfType<ProjectChangeOperation.RelationAdded>().ToArray();
            if (relationOperations.Select(operation => operation.RelationId).Distinct().Count() != relationOperations.Length)
                errors.Add(new("PB-CHANGE-002", "A change set cannot add the same relation more than once."));
            var updatedRelationOperations = draft.Operations.OfType<ProjectChangeOperation.RelationUpdated>().ToArray();
            if (updatedRelationOperations.Select(operation => operation.RelationId).Distinct().Count() != updatedRelationOperations.Length)
                errors.Add(new("PB-CHANGE-002", "A change set cannot update the same relation more than once."));
        }

        return errors.ToImmutable();
    }

    private static ModelElementKind ElementKind(ModelElement element) => element switch
    {
        ActorDefinition => ModelElementKind.Actor,
        OutcomeDefinition => ModelElementKind.Outcome,
        CapabilityDefinition => ModelElementKind.Capability,
        EpisodeDefinition => ModelElementKind.Episode,
        ScenarioDefinition => ModelElementKind.Scenario,
        SceneDefinition => ModelElementKind.Scene,
        InteractionDefinition => ModelElementKind.Interaction,
        IntentDefinition => ModelElementKind.Intent,
        StepDefinition => ModelElementKind.Step,
        ObservationDefinition => ModelElementKind.Observation,
        StateDefinition => ModelElementKind.StateDefinition,
        FactDefinition => ModelElementKind.FactDefinition,
        RuleDefinition => ModelElementKind.RuleDefinition,
        InvariantDefinition => ModelElementKind.InvariantDefinition,
        SemanticResultDefinition => ModelElementKind.ResultDefinition,
        TransitionDefinition => ModelElementKind.TransitionDefinition,
        PathDefinition => ModelElementKind.Path,
        ConditionDefinition => ModelElementKind.Condition,
        EffectDefinition => ModelElementKind.EffectDefinition,
        SystemDefinition => ModelElementKind.System,
        InterfaceDefinition => ModelElementKind.Interface,
        BoundaryDefinition => ModelElementKind.Boundary,
        ContractDefinition => ModelElementKind.Contract,
        _ => throw new InvalidOperationException($"Element type '{element.GetType().Name}' has no change-operation kind."),
    };
}
