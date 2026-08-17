using ProjectBuilder.Domain.Modeling.Elements;
using ProjectBuilder.Domain.Modeling.Primitives;
using ProjectBuilder.Domain.Modeling.Relations;
using ProjectBuilder.Domain.Projects;

namespace ProjectBuilder.Domain.Modeling.Transitions;

public abstract record AddActorTransitionResult
{
    private AddActorTransitionResult()
    {
    }

    public sealed record Accepted(ProjectDefinition Project, ActorDefinition Actor, ProjectModelChangeSet ChangeSet)
        : AddActorTransitionResult;

    public sealed record Conflict(Revision Expected, Revision Actual, System.Collections.Immutable.ImmutableArray<SemanticConflict> Conflicts) : AddActorTransitionResult;
}

public abstract record AddOutcomeTransitionResult
{
    private AddOutcomeTransitionResult()
    {
    }

    public sealed record Accepted(
        ProjectDefinition Project,
        OutcomeDefinition Outcome,
        ModelRelationDefinition Beneficiary,
        ProjectModelChangeSet ChangeSet)
        : AddOutcomeTransitionResult;

    public sealed record Conflict(Revision Expected, Revision Actual, System.Collections.Immutable.ImmutableArray<SemanticConflict> Conflicts) : AddOutcomeTransitionResult;

    public sealed record InvalidBeneficiary(ElementId BeneficiaryId) : AddOutcomeTransitionResult;
}

public abstract record AddCapabilityTransitionResult
{
    private AddCapabilityTransitionResult() { }
    public sealed record Accepted(ProjectDefinition Project, CapabilityDefinition Capability, ProjectModelChangeSet ChangeSet) : AddCapabilityTransitionResult;
    public sealed record Conflict(Revision Expected, Revision Actual, System.Collections.Immutable.ImmutableArray<SemanticConflict> Conflicts) : AddCapabilityTransitionResult;
}

public abstract record UpdateActorTransitionResult
{
    private UpdateActorTransitionResult() { }
    public sealed record Accepted(ProjectDefinition Project, ActorDefinition Actor, ProjectModelChangeSet ChangeSet) : UpdateActorTransitionResult;
    public sealed record Conflict(Revision Expected, Revision Actual, System.Collections.Immutable.ImmutableArray<SemanticConflict> Conflicts) : UpdateActorTransitionResult;
}

public abstract record UpdateOutcomeTransitionResult
{
    private UpdateOutcomeTransitionResult() { }
    public sealed record Accepted(ProjectDefinition Project, OutcomeDefinition Outcome, ModelRelationDefinition Beneficiary, ProjectModelChangeSet ChangeSet) : UpdateOutcomeTransitionResult;
    public sealed record Conflict(Revision Expected, Revision Actual, System.Collections.Immutable.ImmutableArray<SemanticConflict> Conflicts) : UpdateOutcomeTransitionResult;
    public sealed record InvalidBeneficiary(ElementId BeneficiaryId) : UpdateOutcomeTransitionResult;
}

public static class ProjectElementTransition
{
    public static AddCapabilityTransitionResult AddCapability(
        ProjectDefinition project, Revision expectedRevision, ElementId capabilityId, ElementName name,
        Description ability, System.Collections.Immutable.ImmutableArray<ElementId> outcomeIds,
        CapabilityPriority priority, int order, ChangeSetId changeSetId, ChangeReason reason,
        UtcTimestamp occurredAt, string createdBy, KnowledgeStatus knowledgeStatus = KnowledgeStatus.Known)
    {
        if (project.Revision != expectedRevision)
            return new AddCapabilityTransitionResult.Conflict(expectedRevision, project.Revision,
                ProjectChangeSetTransition.RevisionConflicts(expectedRevision, project.Revision));
        var capability = new CapabilityDefinition(capabilityId, project.Id, name, ability, outcomeIds,
            priority, order, occurredAt, createdBy, knowledgeStatus);
        var committed = (ProjectChangeSetTransitionResult.Accepted)ProjectChangeSetTransition.Commit(
            project, expectedRevision,
            new(changeSetId, capabilityId, "capability.added", reason, ProjectChangeSetTransition.AddedElements([capability])),
            occurredAt, createdBy);
        return new AddCapabilityTransitionResult.Accepted(committed.Project, capability, committed.ChangeSet);
    }

    public static UpdateActorTransitionResult UpdateActor(
        ProjectDefinition project, Revision expectedRevision, ActorDefinition current,
        ElementName name, ContextualRole contextualRole, ActorKind actorKind,
        System.Collections.Immutable.ImmutableArray<ActorStatement> goals,
        System.Collections.Immutable.ImmutableArray<ActorStatement> responsibilities,
        System.Collections.Immutable.ImmutableArray<ActorStatement> authority,
        System.Collections.Immutable.ImmutableArray<ActorStatement> constraints,
        KnowledgeStatus knowledgeStatus, ChangeSetId changeSetId, ChangeReason reason,
        UtcTimestamp occurredAt, string changedBy)
    {
        if (project.Revision != expectedRevision)
            return new UpdateActorTransitionResult.Conflict(expectedRevision, project.Revision, ProjectChangeSetTransition.RevisionConflicts(expectedRevision, project.Revision));
        var actor = new ActorDefinition(current.Id, current.ProjectId, name, contextualRole, actorKind,
            goals, responsibilities, authority, constraints, current.Order, current.CreatedAt, current.CreatedBy, knowledgeStatus);
        var committed = (ProjectChangeSetTransitionResult.Accepted)ProjectChangeSetTransition.Commit(
            project, expectedRevision,
            new(changeSetId, actor.Id, "actor.updated", reason,
                [new ProjectChangeOperation.ElementUpdated(0, actor.Id, ModelElementKind.Actor, current.Name, actor.Name)]),
            occurredAt, changedBy);
        return new UpdateActorTransitionResult.Accepted(committed.Project, actor, committed.ChangeSet);
    }

    public static UpdateOutcomeTransitionResult UpdateOutcome(
        ProjectDefinition project, Revision expectedRevision, OutcomeDefinition current,
        ModelRelationDefinition currentBeneficiary, ElementName name, OutcomeStatement statement,
        System.Collections.Immutable.ImmutableArray<SuccessSignal> successSignals, ActorDefinition beneficiary,
        KnowledgeStatus knowledgeStatus, ChangeSetId changeSetId, ChangeReason reason,
        UtcTimestamp occurredAt, string changedBy)
    {
        if (project.Revision != expectedRevision)
            return new UpdateOutcomeTransitionResult.Conflict(expectedRevision, project.Revision, ProjectChangeSetTransition.RevisionConflicts(expectedRevision, project.Revision));
        if (beneficiary.ProjectId != project.Id)
            return new UpdateOutcomeTransitionResult.InvalidBeneficiary(beneficiary.Id);
        var outcome = new OutcomeDefinition(current.Id, current.ProjectId, name, statement, successSignals,
            current.Order, current.CreatedAt, current.CreatedBy, knowledgeStatus);
        var relation = (SemanticResult<ModelRelationDefinition>.Accepted)ModelRelationRegistry.Create(
            currentBeneficiary.Id, project.Id, ModelRelationKind.BenefitsFrom, beneficiary.Id,
            ModelElementKind.Actor, outcome.Id, ModelElementKind.Outcome, currentBeneficiary.CreatedAt, currentBeneficiary.CreatedBy);
        var operations = System.Collections.Immutable.ImmutableArray.Create<ProjectChangeOperation>(
            new ProjectChangeOperation.ElementUpdated(0, outcome.Id, ModelElementKind.Outcome, current.Name, outcome.Name),
            new ProjectChangeOperation.RelationUpdated(1, relation.Value.Id, relation.Value.Kind,
                currentBeneficiary.SourceId, relation.Value.SourceId, relation.Value.TargetId));
        var committed = (ProjectChangeSetTransitionResult.Accepted)ProjectChangeSetTransition.Commit(
            project, expectedRevision, new(changeSetId, outcome.Id, "outcome.updated", reason, operations), occurredAt, changedBy);
        return new UpdateOutcomeTransitionResult.Accepted(committed.Project, outcome, relation.Value, committed.ChangeSet);
    }

    public static AddActorTransitionResult AddActor(
        ProjectDefinition project,
        Revision expectedRevision,
        ElementId actorId,
        ElementName name,
        ContextualRole contextualRole,
        ActorKind actorKind,
        System.Collections.Immutable.ImmutableArray<ActorStatement> goals,
        System.Collections.Immutable.ImmutableArray<ActorStatement> responsibilities,
        System.Collections.Immutable.ImmutableArray<ActorStatement> authority,
        System.Collections.Immutable.ImmutableArray<ActorStatement> constraints,
        int order,
        ChangeSetId changeSetId,
        ChangeReason reason,
        UtcTimestamp occurredAt,
        string createdBy,
        KnowledgeStatus knowledgeStatus = KnowledgeStatus.Known)
    {
        if (project.Revision != expectedRevision)
        {
            return new AddActorTransitionResult.Conflict(
                expectedRevision, project.Revision,
                ProjectChangeSetTransition.RevisionConflicts(expectedRevision, project.Revision));
        }

        var actor = new ActorDefinition(
            actorId,
            project.Id,
            name,
            contextualRole,
            actorKind,
            goals,
            responsibilities,
            authority,
            constraints,
            order,
            occurredAt,
            createdBy,
            knowledgeStatus);
        var committed = ProjectChangeSetTransition.Commit(
            project,
            expectedRevision,
            new(changeSetId, actorId, "actor.added", reason, ProjectChangeSetTransition.AddedElements([actor])),
            occurredAt,
            createdBy);
        var accepted = (ProjectChangeSetTransitionResult.Accepted)committed;
        return new AddActorTransitionResult.Accepted(accepted.Project, actor, accepted.ChangeSet);
    }

    public static AddOutcomeTransitionResult AddOutcome(
        ProjectDefinition project,
        Revision expectedRevision,
        ElementId outcomeId,
        ElementName name,
        OutcomeStatement statement,
        System.Collections.Immutable.ImmutableArray<SuccessSignal> successSignals,
        ActorDefinition beneficiary,
        RelationId relationId,
        int order,
        ChangeSetId changeSetId,
        ChangeReason reason,
        UtcTimestamp occurredAt,
        string createdBy,
        KnowledgeStatus knowledgeStatus = KnowledgeStatus.Known)
    {
        if (project.Revision != expectedRevision)
        {
            return new AddOutcomeTransitionResult.Conflict(
                expectedRevision, project.Revision,
                ProjectChangeSetTransition.RevisionConflicts(expectedRevision, project.Revision));
        }

        if (beneficiary.ProjectId != project.Id)
        {
            return new AddOutcomeTransitionResult.InvalidBeneficiary(beneficiary.Id);
        }

        var outcome = new OutcomeDefinition(
            outcomeId,
            project.Id,
            name,
            statement,
            successSignals,
            order,
            occurredAt,
            createdBy,
            knowledgeStatus);
        var relation = (SemanticResult<ModelRelationDefinition>.Accepted)ModelRelationRegistry.Create(
            relationId,
            project.Id,
            ModelRelationKind.BenefitsFrom,
            beneficiary.Id,
            ModelElementKind.Actor,
            outcome.Id,
            ModelElementKind.Outcome,
            occurredAt,
            createdBy);
        var operations = ProjectChangeSetTransition.AddedElements([outcome]).Add(
            new ProjectChangeOperation.RelationAdded(
                1, relation.Value.Id, relation.Value.Kind, relation.Value.SourceId, relation.Value.TargetId));
        var committed = ProjectChangeSetTransition.Commit(
            project,
            expectedRevision,
            new(changeSetId, outcomeId, "outcome.added", reason, operations),
            occurredAt,
            createdBy);
        var accepted = (ProjectChangeSetTransitionResult.Accepted)committed;
        return new AddOutcomeTransitionResult.Accepted(accepted.Project, outcome, relation.Value, accepted.ChangeSet);
    }
}
