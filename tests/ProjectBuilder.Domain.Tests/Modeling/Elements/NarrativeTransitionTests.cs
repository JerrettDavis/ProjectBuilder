using System.Collections.Immutable;
using ProjectBuilder.Domain.Modeling.Elements;
using ProjectBuilder.Domain.Modeling.Primitives;
using ProjectBuilder.Domain.Modeling.Transitions;
using ProjectBuilder.Domain.Projects;

namespace ProjectBuilder.Domain.Tests.Modeling.Elements;

public sealed class NarrativeTransitionTests
{
    private static readonly int[] ExpectedOrders = [20, 21, 22, 23, 24, 25, 26];
    [Test]
    public void Complete_narrative_is_nested_in_deterministic_order_and_advances_once()
    {
        var project = Project(3);
        var modeler = Actor(project.Id, 1, "Modeler");
        var contributor = Actor(project.Id, 2, "Contributor");

        var result = NarrativeTransition.Define(
            project, RevisionValue(3), Outcome(project.Id), [modeler, contributor], modeler, contributor,
            Ids(), Draft(), 20, ChangeSet(20), Reason(), Now, "modeler");

        Assert.That(result, Is.TypeOf<DefineNarrativeTransitionResult.Accepted>());
        var accepted = (DefineNarrativeTransitionResult.Accepted)result;
        var elements = accepted.Narrative.Elements;
        Assert.Multiple(() =>
        {
            Assert.That(accepted.Project.Revision.Value, Is.EqualTo(4));
            Assert.That(elements.Select(x => x.Order), Is.EqualTo(ExpectedOrders));
            Assert.That(accepted.Narrative.Scenario.ParentId, Is.EqualTo(accepted.Narrative.Episode.Id));
            Assert.That(accepted.Narrative.Scene.ParentId, Is.EqualTo(accepted.Narrative.Scenario.Id));
            Assert.That(accepted.Narrative.Interaction.ParentId, Is.EqualTo(accepted.Narrative.Scene.Id));
            Assert.That(accepted.Narrative.Intent.ParentId, Is.EqualTo(accepted.Narrative.Interaction.Id));
            Assert.That(accepted.ChangeSet.ChangeKind, Is.EqualTo("narrative.defined"));
            Assert.That(accepted.ChangeSet.Operations.Length, Is.EqualTo(7));
            Assert.That(accepted.ChangeSet.Operations.Select(operation => operation.Sequence), Is.EqualTo(Enumerable.Range(0, 7)));
        });
    }

    [Test]
    public void Interaction_rejects_an_initiator_missing_from_scenario_participants()
    {
        var project = Project(3);
        var modeler = Actor(project.Id, 1, "Modeler");
        var contributor = Actor(project.Id, 2, "Contributor");

        var result = NarrativeTransition.Define(
            project, RevisionValue(3), Outcome(project.Id), [contributor], modeler, contributor,
            Ids(), Draft(), 20, ChangeSet(20), Reason(), Now, "modeler");

        var invalid = (DefineNarrativeTransitionResult.Invalid)result;
        Assert.That(invalid.Errors.Select(x => x.Code), Does.Contain("PB-NARR-006"));
    }

    [Test]
    public void Structure_validation_reports_missing_parent_and_cycle_without_repairing_them()
    {
        var episode = Element(30);
        var scenario = Element(31);
        var missing = Element(99);
        var missingParent = NarrativeStructure.Validate([
            new(episode, null, NarrativeKind.Episode, 0),
            new(scenario, missing, NarrativeKind.Scenario, 1)]);
        var cycle = NarrativeStructure.Validate([
            new(episode, scenario, NarrativeKind.Episode, 0),
            new(scenario, episode, NarrativeKind.Scenario, 1)]);

        Assert.Multiple(() =>
        {
            Assert.That(missingParent.Select(x => x.Code), Does.Contain("PB-STRUCT-002"));
            Assert.That(cycle.Select(x => x.Code), Does.Contain("PB-STRUCT-003"));
        });
    }

    private static NarrativeDraft Draft() => new(
        Name("Create Project"), Text("A modeler has an unmodeled project intention."),
        Text("The project purpose is persisted and reviewable."),
        Name("Authorized modeler creates project"), ScenarioClassification.Happy,
        [Fact("The local development workspace is available.")],
        Text("The modeler submits the project definition."),
        Text("Revision 1 is visible with its purpose and outcome."),
        Name("Capture project definition"), Text("The accessible project form."),
        Text("Capture purpose-led project meaning."), Name("Submit project definition"),
        Text("Create a purpose-led project."), Text("Validate and commit the project change set."),
        Text("The modeler sees revision 1 and the allowed next action."),
        [Fact("ProjectCreated"), Fact("Invalid"), Fact("Denied"), Fact("Conflict")]);

    private static NarrativeIds Ids() => new(Element(10), Element(11), Element(12), Element(13), Element(14), Element(15), Element(16));
    private static ProjectDefinition Project(long revision)
    {
        var created = ProjectDefinition.Create(ProjectIdValue(1), Accepted(WorkspaceId.Parse("0198ad00-0000-7000-8000-000000000700")),
            Name("Project Builder"), Accepted(ProjectPurpose.Create("Model system meaning.")),
            Accepted(IntendedOutcome.Create("A modeler can build a trustworthy definition.")), ChangeSet(1),
            Reason(), Now, "modeler");
        return ProjectDefinition.Restore(created.Id, created.WorkspaceId, created.Name, created.Purpose,
            created.IntendedOutcome, RevisionValue(revision), created.Creation);
    }
    private static ActorDefinition Actor(ProjectId projectId, int seed, string name) => new(
        Element(seed), projectId, Name(name), Accepted(ContextualRole.Create($"The {name} role.")),
        ActorKind.HumanRole, [], [], [], [], seed, Now, "modeler");
    private static OutcomeDefinition Outcome(ProjectId projectId) => new(
        Element(3), projectId, Name("Project purpose is persisted"),
        Accepted(OutcomeStatement.Create("A modeler can reopen the project purpose.")),
        [Accepted(SuccessSignal.Create("Revision 1 is visible."))], 3, Now, "modeler");
    private static readonly UtcTimestamp Now = UtcTimestamp.Create(new DateTimeOffset(2026, 8, 15, 23, 0, 0, TimeSpan.Zero));
    private static ElementName Name(string value) => Accepted(ElementName.Create(value));
    private static NarrativeText Text(string value) => Accepted(NarrativeText.Create(value));
    private static NarrativeFact Fact(string value) => Accepted(NarrativeFact.Create(value));
    private static ChangeReason Reason() => Accepted(ChangeReason.Create("Define the complete project creation narrative."));
    private static Revision RevisionValue(long value) => Accepted(Revision.Create(value));
    private static ProjectId ProjectIdValue(int seed) => Accepted(ProjectId.Parse($"0198ad00-0000-7000-8000-{seed:X12}"));
    private static ElementId Element(int seed) => Accepted(ElementId.Parse($"0198ad00-0000-7000-8400-{seed:X12}"));
    private static ChangeSetId ChangeSet(int seed) => Accepted(ChangeSetId.Parse($"0198ad00-0000-7000-9400-{seed:X12}"));
    private static T Accepted<T>(SemanticResult<T> result) where T : notnull => ((SemanticResult<T>.Accepted)result).Value;
}
