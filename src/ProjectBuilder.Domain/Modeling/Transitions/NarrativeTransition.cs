using System.Collections.Immutable;
using ProjectBuilder.Domain.Modeling.Elements;
using ProjectBuilder.Domain.Modeling.Primitives;
using ProjectBuilder.Domain.Projects;

namespace ProjectBuilder.Domain.Modeling.Transitions;

public enum NarrativeKind { Episode, Scenario, Scene, Interaction, Intent, Step, Observation }

public sealed record NarrativeNode(ElementId Id, ElementId? ParentId, NarrativeKind Kind, int Order);

public static class NarrativeStructure
{
    public static IReadOnlyList<SemanticError> Validate(IEnumerable<NarrativeNode> source)
    {
        var nodes = source.ToArray();
        var errors = new List<SemanticError>();
        var byId = nodes.GroupBy(x => x.Id).ToDictionary(x => x.Key, x => x.ToArray());
        if (byId.Any(x => x.Value.Length > 1))
            errors.Add(new("PB-STRUCT-001", "Narrative element identifiers must be unique."));
        if (nodes.GroupBy(x => x.Order).Any(x => x.Count() > 1) || nodes.Any(x => x.Order < 0))
            errors.Add(new("PB-STRUCT-005", "Narrative element order values must be non-negative and unique."));

        var unique = byId.ToDictionary(x => x.Key, x => x.Value[0]);
        foreach (var node in nodes)
        {
            if (node.ParentId is null)
            {
                if (node.Kind != NarrativeKind.Episode)
                    errors.Add(new("PB-STRUCT-004", $"A {node.Kind} requires its permitted narrative parent."));
                continue;
            }
            if (!unique.TryGetValue(node.ParentId, out var parent))
            {
                errors.Add(new("PB-STRUCT-002", $"The parent of {node.Kind} does not exist."));
                continue;
            }
            if (!Permitted(parent.Kind, node.Kind))
                errors.Add(new("PB-STRUCT-004", $"A {node.Kind} cannot be contained by {parent.Kind}."));
        }

        foreach (var node in nodes)
        {
            var visited = new HashSet<ElementId>();
            var current = node;
            while (current.ParentId is not null && unique.TryGetValue(current.ParentId, out var parent))
            {
                if (!visited.Add(parent.Id))
                {
                    errors.Add(new("PB-STRUCT-003", "Narrative containment must be acyclic."));
                    break;
                }
                current = parent;
            }
        }
        return errors.Distinct().ToArray();
    }

    private static bool Permitted(NarrativeKind parent, NarrativeKind child) => (parent, child) switch
    {
        (NarrativeKind.Episode, NarrativeKind.Scenario) => true,
        (NarrativeKind.Scenario, NarrativeKind.Scene) => true,
        (NarrativeKind.Scene, NarrativeKind.Interaction) => true,
        (NarrativeKind.Interaction, NarrativeKind.Intent or NarrativeKind.Step or NarrativeKind.Observation) => true,
        _ => false,
    };
}

public sealed record NarrativeIds(
    ElementId EpisodeId, ElementId ScenarioId, ElementId SceneId, ElementId InteractionId,
    ElementId IntentId, ElementId StepId, ElementId ObservationId);

public sealed record NarrativeDraft(
    ElementName EpisodeName, NarrativeText EpisodeStart, NarrativeText EpisodeEnd,
    ElementName ScenarioName, ScenarioClassification Classification,
    ImmutableArray<NarrativeFact> StartingFacts, NarrativeText Trigger, NarrativeText ExpectedOutcome,
    ElementName SceneName, NarrativeText Setting, NarrativeText Responsibility,
    ElementName InteractionName, NarrativeText Intent, NarrativeText Step, NarrativeText Observation,
    ImmutableArray<NarrativeFact> SemanticResults);

public sealed record NarrativeDefinitionSet(
    EpisodeDefinition Episode, ScenarioDefinition Scenario, SceneDefinition Scene,
    InteractionDefinition Interaction, IntentDefinition Intent, StepDefinition Step,
    ObservationDefinition Observation)
{
    public ImmutableArray<ModelElement> Elements => [Episode, Scenario, Scene, Interaction, Intent, Step, Observation];
}

public abstract record DefineNarrativeTransitionResult
{
    private DefineNarrativeTransitionResult() { }
    public sealed record Accepted(ProjectDefinition Project, NarrativeDefinitionSet Narrative, ProjectModelChangeSet ChangeSet)
        : DefineNarrativeTransitionResult;
    public sealed record Conflict(Revision Expected, Revision Actual, ImmutableArray<SemanticConflict> Conflicts) : DefineNarrativeTransitionResult;
    public sealed record Invalid(IReadOnlyList<SemanticError> Errors) : DefineNarrativeTransitionResult;
}

public static class NarrativeTransition
{
    public static DefineNarrativeTransitionResult Define(
        ProjectDefinition project, Revision expectedRevision, OutcomeDefinition outcome,
        ImmutableArray<ActorDefinition> participants, ActorDefinition initiator, ActorDefinition receiver,
        NarrativeIds ids, NarrativeDraft draft, int firstOrder, ChangeSetId changeSetId,
        ChangeReason reason, UtcTimestamp occurredAt, string createdBy)
    {
        if (project.Revision != expectedRevision)
            return new DefineNarrativeTransitionResult.Conflict(
                expectedRevision, project.Revision,
                ProjectChangeSetTransition.RevisionConflicts(expectedRevision, project.Revision));

        var referenceErrors = ValidateReferences(project, outcome, participants, initiator, receiver);
        if (referenceErrors.Count > 0)
            return new DefineNarrativeTransitionResult.Invalid(referenceErrors);

        var participantIds = participants.Select(x => x.Id).Distinct().ToImmutableArray();
        var narrative = new NarrativeDefinitionSet(
            new(ids.EpisodeId, project.Id, draft.EpisodeName, draft.EpisodeStart, draft.EpisodeEnd,
                outcome.Id, participantIds, firstOrder, occurredAt, createdBy),
            new(ids.ScenarioId, project.Id, ids.EpisodeId, draft.ScenarioName, draft.Classification,
                draft.StartingFacts, draft.Trigger, draft.ExpectedOutcome, participantIds,
                firstOrder + 1, occurredAt, createdBy),
            new(ids.SceneId, project.Id, ids.ScenarioId, draft.SceneName, draft.Setting,
                draft.Responsibility, participantIds, firstOrder + 2, occurredAt, createdBy),
            new(ids.InteractionId, project.Id, ids.SceneId, draft.InteractionName, initiator.Id,
                receiver.Id, draft.SemanticResults, firstOrder + 3, occurredAt, createdBy),
            new(ids.IntentId, project.Id, ids.InteractionId, AcceptedName("Express intent"), draft.Intent,
                initiator.Id, firstOrder + 4, occurredAt, createdBy),
            new(ids.StepId, project.Id, ids.InteractionId, AcceptedName("Perform interaction step"), draft.Step,
                firstOrder + 5, occurredAt, createdBy),
            new(ids.ObservationId, project.Id, ids.InteractionId, AcceptedName("Observe result"), draft.Observation,
                initiator.Id, firstOrder + 6, occurredAt, createdBy));

        var structureErrors = NarrativeStructure.Validate(narrative.Elements.Select(ToNode));
        if (structureErrors.Count > 0)
            return new DefineNarrativeTransitionResult.Invalid(structureErrors);

        var committed = ProjectChangeSetTransition.Commit(
            project,
            expectedRevision,
            new(changeSetId, ids.EpisodeId, "narrative.defined", reason,
                ProjectChangeSetTransition.AddedElements(narrative.Elements)),
            occurredAt,
            createdBy);
        var accepted = (ProjectChangeSetTransitionResult.Accepted)committed;
        return new DefineNarrativeTransitionResult.Accepted(accepted.Project, narrative, accepted.ChangeSet);
    }

    private static List<SemanticError> ValidateReferences(
        ProjectDefinition project, OutcomeDefinition outcome, ImmutableArray<ActorDefinition> participants,
        ActorDefinition initiator, ActorDefinition receiver)
    {
        var errors = new List<SemanticError>();
        if (outcome.ProjectId != project.Id)
            errors.Add(new("PB-REF-001", "The episode outcome must exist in this project."));
        if (participants.IsDefaultOrEmpty)
            errors.Add(new("PB-NARR-001", "An episode requires at least one participant and beneficiary context."));
        if (participants.Any(x => x.ProjectId != project.Id))
            errors.Add(new("PB-REF-001", "Every narrative participant must exist in this project."));
        if (!participants.Any(x => x.Id == initiator.Id))
            errors.Add(new("PB-NARR-006", "The interaction initiator must be a scenario participant."));
        if (!participants.Any(x => x.Id == receiver.Id))
            errors.Add(new("PB-NARR-007", "The interaction receiver must be a scenario participant."));
        return errors;
    }

    private static NarrativeNode ToNode(ModelElement element) =>
        new(element.Id, element.ParentId, element switch
        {
            EpisodeDefinition => NarrativeKind.Episode,
            ScenarioDefinition => NarrativeKind.Scenario,
            SceneDefinition => NarrativeKind.Scene,
            InteractionDefinition => NarrativeKind.Interaction,
            IntentDefinition => NarrativeKind.Intent,
            StepDefinition => NarrativeKind.Step,
            ObservationDefinition => NarrativeKind.Observation,
            _ => throw new InvalidOperationException("Unknown narrative element."),
        }, element.Order);

    private static ElementName AcceptedName(string value) =>
        ((SemanticResult<ElementName>.Accepted)ElementName.Create(value)).Value;
}
