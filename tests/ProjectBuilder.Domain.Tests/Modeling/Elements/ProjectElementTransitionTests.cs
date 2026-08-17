using System.Collections.Immutable;
using ProjectBuilder.Domain.Modeling.Elements;
using ProjectBuilder.Domain.Modeling.Primitives;
using ProjectBuilder.Domain.Modeling.Relations;
using ProjectBuilder.Domain.Modeling.Transitions;
using ProjectBuilder.Domain.Projects;

namespace ProjectBuilder.Domain.Tests.Modeling.Elements;

public sealed class ProjectElementTransitionTests
{
    [Test]
    public void Capability_addition_preserves_explicit_value_trace_and_priority_in_one_change_set()
    {
        var project = Project();
        var outcomeId = Element(41);

        var result = ProjectElementTransition.AddCapability(project, Revision.Initial, Element(42),
            Accepted(ElementName.Create("Verify repository foundation")),
            Accepted(Description.Create("Build, run, and verify the repository through one path.")),
            [outcomeId], CapabilityPriority.Critical, 12, ChangeSet(42),
            Accepted(ChangeReason.Create("Connect value to an explicit ability.")), Now, "modeler-1", KnowledgeStatus.Assumed);

        var accepted = (AddCapabilityTransitionResult.Accepted)result;
        Assert.Multiple(() =>
        {
            Assert.That(accepted.Project.Revision.Value, Is.EqualTo(2));
            Assert.That(accepted.Capability.OutcomeIds, Is.EqualTo(new[] { outcomeId }));
            Assert.That(accepted.Capability.Priority, Is.EqualTo(CapabilityPriority.Critical));
            Assert.That(accepted.Capability.KnowledgeStatus, Is.EqualTo(KnowledgeStatus.Assumed));
            Assert.That(accepted.ChangeSet.ChangeKind, Is.EqualTo("capability.added"));
            Assert.That(((ProjectChangeOperation.ElementAdded)accepted.ChangeSet.Operations.Single()).ElementKind,
                Is.EqualTo(ModelElementKind.Capability));
        });
    }

    [Test]
    public void Actor_addition_advances_exactly_one_revision_and_records_the_changed_element()
    {
        var project = Project();

        var result = ProjectElementTransition.AddActor(
            project,
            Revision.Initial,
            Element(10),
            Accepted(ElementName.Create("Modeler")),
            Accepted(ContextualRole.Create("Defines and reviews semantic model truth.")),
            ActorKind.HumanRole,
            Statements("Express system meaning"),
            Statements("Maintain the canonical definition"),
            Statements("Commit reviewed semantic changes"),
            Statements("Must preserve human-complete workflows"),
            10,
            ChangeSet(10),
            Accepted(ChangeReason.Create("Add the modeler role.")),
            Now,
            "modeler-1",
            KnowledgeStatus.Assumed);

        Assert.That(result, Is.TypeOf<AddActorTransitionResult.Accepted>());
        var accepted = (AddActorTransitionResult.Accepted)result;
        Assert.Multiple(() =>
        {
            Assert.That(accepted.Project.Revision.Value, Is.EqualTo(2));
            Assert.That(accepted.Actor.ProjectId, Is.EqualTo(project.Id));
            Assert.That(accepted.Actor.ContextualRole.Value, Does.Contain("semantic model"));
            Assert.That(accepted.Actor.KnowledgeStatus, Is.EqualTo(KnowledgeStatus.Assumed));
            Assert.That(accepted.ChangeSet.BaseRevision, Is.EqualTo(Revision.Initial));
            Assert.That(accepted.ChangeSet.ResultRevision.Value, Is.EqualTo(2));
            Assert.That(accepted.ChangeSet.ChangedElementId, Is.EqualTo(accepted.Actor.Id));
            Assert.That(accepted.ChangeSet.ChangeKind, Is.EqualTo("actor.added"));
            Assert.That(accepted.ChangeSet.Operations.Length, Is.EqualTo(1));
            Assert.That(((ProjectChangeOperation.ElementAdded)accepted.ChangeSet.Operations[0]).ElementKind,
                Is.EqualTo(ProjectBuilder.Domain.Modeling.Relations.ModelElementKind.Actor));
        });
    }

    [Test]
    public void Stale_actor_addition_returns_conflict_without_constructing_state()
    {
        var project = ProjectAtRevision(2);

        var result = ProjectElementTransition.AddActor(
            project,
            Revision.Initial,
            Element(10),
            Accepted(ElementName.Create("Reviewer")),
            Accepted(ContextualRole.Create("Reviews definitions.")),
            ActorKind.HumanRole,
            [],
            [],
            [],
            [],
            10,
            ChangeSet(10),
            Accepted(ChangeReason.Create("Add reviewer.")),
            Now,
            "modeler-1");

        var conflict = (AddActorTransitionResult.Conflict)result;
        Assert.Multiple(() =>
        {
            Assert.That(conflict.Expected, Is.EqualTo(Revision.Initial));
            Assert.That(conflict.Actual, Is.EqualTo(Accepted(Revision.Create(2))));
            Assert.That(conflict.Conflicts.Single().Code, Is.EqualTo("project.revision.conflict"));
        });
    }

    [Test]
    public void Outcome_addition_creates_one_beneficiary_relation_and_advances_revision()
    {
        var project = ProjectAtRevision(2);
        var beneficiary = Actor(project.Id, Element(20));
        var expected = Accepted(Revision.Create(2));

        var result = ProjectElementTransition.AddOutcome(
            project,
            expected,
            Element(21),
            Accepted(ElementName.Create("Repository can be verified")),
            Accepted(OutcomeStatement.Create("A contributor can verify a clean clone.")),
            ImmutableArray.Create(Accepted(SuccessSignal.Create("The verification command passes."))),
            beneficiary,
            Relation(21),
            20,
            ChangeSet(21),
            Accepted(ChangeReason.Create("Add the contributor outcome.")),
            Now,
            "modeler-1",
            KnowledgeStatus.Disputed);

        Assert.That(result, Is.TypeOf<AddOutcomeTransitionResult.Accepted>());
        var accepted = (AddOutcomeTransitionResult.Accepted)result;
        Assert.Multiple(() =>
        {
            Assert.That(accepted.Project.Revision.Value, Is.EqualTo(3));
            Assert.That(accepted.Beneficiary.SourceId, Is.EqualTo(beneficiary.Id));
            Assert.That(accepted.Beneficiary.TargetId, Is.EqualTo(accepted.Outcome.Id));
            Assert.That(accepted.Outcome.SuccessSignals.Length, Is.EqualTo(1));
            Assert.That(accepted.Outcome.KnowledgeStatus, Is.EqualTo(KnowledgeStatus.Disputed));
            Assert.That(accepted.ChangeSet.ChangeKind, Is.EqualTo("outcome.added"));
            Assert.That(accepted.ChangeSet.Operations.Select(operation => operation.GetType()), Is.EqualTo(new[]
            {
                typeof(ProjectChangeOperation.ElementAdded),
                typeof(ProjectChangeOperation.RelationAdded),
            }));
        });
    }

    [Test]
    public void Outcome_rejects_a_beneficiary_from_another_project()
    {
        var project = ProjectAtRevision(2);
        var otherProjectActor = Actor(ProjectIdValue(99), Element(30));

        var result = ProjectElementTransition.AddOutcome(
            project,
            Accepted(Revision.Create(2)),
            Element(31),
            Accepted(ElementName.Create("Invalid outcome")),
            Accepted(OutcomeStatement.Create("This outcome crosses project truth.")),
            ImmutableArray.Create(Accepted(SuccessSignal.Create("It must be rejected."))),
            otherProjectActor,
            Relation(31),
            20,
            ChangeSet(31),
            Accepted(ChangeReason.Create("Prove beneficiary scope.")),
            Now,
            "modeler-1");

        Assert.That(result, Is.EqualTo(new AddOutcomeTransitionResult.InvalidBeneficiary(otherProjectActor.Id)));
    }

    [Test]
    public void Actor_update_preserves_identity_and_records_a_typed_update()
    {
        var project = ProjectAtRevision(3);
        var current = Actor(project.Id, Element(40));
        var result = ProjectElementTransition.UpdateActor(project, Accepted(Revision.Create(3)), current,
            Accepted(ElementName.Create("Domain reviewer")), Accepted(ContextualRole.Create("Reviews disputed semantic meaning.")),
            ActorKind.HumanRole, Statements("Reach a review decision"), Statements("Challenge unsupported claims"),
            Statements("Approve a definition"), Statements("Cannot invent authority"), KnowledgeStatus.Disputed,
            ChangeSet(40), Accepted(ChangeReason.Create("Clarify the reviewer role.")), Now, "modeler-1");

        var accepted = (UpdateActorTransitionResult.Accepted)result;
        Assert.Multiple(() =>
        {
            Assert.That(accepted.Actor.Id, Is.EqualTo(current.Id));
            Assert.That(accepted.Actor.Order, Is.EqualTo(current.Order));
            Assert.That(accepted.Actor.KnowledgeStatus, Is.EqualTo(KnowledgeStatus.Disputed));
            Assert.That(accepted.Project.Revision.Value, Is.EqualTo(4));
            Assert.That(accepted.ChangeSet.Operations.Single(), Is.TypeOf<ProjectChangeOperation.ElementUpdated>());
        });
    }

    [Test]
    public void Outcome_update_can_retarget_the_owned_beneficiary_relation_atomically()
    {
        var project = ProjectAtRevision(3);
        var previousActor = Actor(project.Id, Element(50));
        var nextActor = Actor(project.Id, Element(51));
        var current = new OutcomeDefinition(Element(52), project.Id, Accepted(ElementName.Create("Review is visible")),
            Accepted(OutcomeStatement.Create("A contributor can see the review result.")),
            [Accepted(SuccessSignal.Create("The decision is visible."))], 30, Now, "modeler-1");
        var relation = ((SemanticResult<ModelRelationDefinition>.Accepted)ModelRelationRegistry.Create(
            Relation(52), project.Id, ModelRelationKind.BenefitsFrom, previousActor.Id, ModelElementKind.Actor,
            current.Id, ModelElementKind.Outcome, Now, "modeler-1")).Value;

        var result = ProjectElementTransition.UpdateOutcome(project, Accepted(Revision.Create(3)), current, relation,
            Accepted(ElementName.Create("Review decision is traceable")),
            Accepted(OutcomeStatement.Create("A reviewer can trace the accepted decision.")),
            [Accepted(SuccessSignal.Create("The change reason is visible."))], nextActor, KnowledgeStatus.Assumed,
            ChangeSet(52), Accepted(ChangeReason.Create("Retarget the review outcome.")), Now, "modeler-1");

        var accepted = (UpdateOutcomeTransitionResult.Accepted)result;
        Assert.Multiple(() =>
        {
            Assert.That(accepted.Outcome.Id, Is.EqualTo(current.Id));
            Assert.That(accepted.Beneficiary.Id, Is.EqualTo(relation.Id));
            Assert.That(accepted.Beneficiary.SourceId, Is.EqualTo(nextActor.Id));
            Assert.That(accepted.ChangeSet.Operations.Select(x => x.GetType()), Is.EqualTo(new[] { typeof(ProjectChangeOperation.ElementUpdated), typeof(ProjectChangeOperation.RelationUpdated) }));
            Assert.That(accepted.Project.Revision.Value, Is.EqualTo(4));
        });
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("  ")]
    public void Contextual_role_rejects_blank_content(string? value)
    {
        Assert.That(ContextualRole.Create(value), Is.TypeOf<SemanticResult<ContextualRole>.Rejected>());
    }

    private static readonly UtcTimestamp Now = UtcTimestamp.Create(
        new DateTimeOffset(2026, 8, 15, 22, 0, 0, TimeSpan.Zero));

    private static ProjectDefinition Project() =>
        ProjectDefinition.Create(
            ProjectIdValue(1),
            Accepted(WorkspaceId.Parse("0198ad00-0000-7000-8000-000000000700")),
            Accepted(ElementName.Create("Project Builder")),
            Accepted(ProjectPurpose.Create("Model system meaning.")),
            Accepted(IntendedOutcome.Create("A modeler can build a trustworthy definition.")),
            ChangeSet(1),
            Accepted(ChangeReason.Create("Create project.")),
            Now,
            "modeler-1");

    private static ProjectDefinition ProjectAtRevision(long revision)
    {
        var project = Project();
        return ProjectDefinition.Restore(
            project.Id,
            project.WorkspaceId,
            project.Name,
            project.Purpose,
            project.IntendedOutcome,
            Accepted(Revision.Create(revision)),
            project.Creation);
    }

    private static ActorDefinition Actor(ProjectId projectId, ElementId id) =>
        new(
            id,
            projectId,
            Accepted(ElementName.Create("Contributor")),
            Accepted(ContextualRole.Create("Contributes model definitions.")),
            ActorKind.HumanRole,
            [],
            [],
            [],
            [],
            10,
            Now,
            "modeler-1");

    private static ImmutableArray<ActorStatement> Statements(string value) =>
        ImmutableArray.Create(Accepted(ActorStatement.Create(value)));

    private static ProjectId ProjectIdValue(int seed) =>
        Accepted(ProjectId.Parse($"0198ad00-0000-7000-8000-{seed:X12}"));

    private static ElementId Element(int seed) =>
        Accepted(ElementId.Parse($"0198ad00-0000-7000-8001-{seed:X12}"));

    private static RelationId Relation(int seed) =>
        Accepted(RelationId.Parse($"0198ad00-0000-7000-8002-{seed:X12}"));

    private static ChangeSetId ChangeSet(int seed) =>
        Accepted(ChangeSetId.Parse($"0198ad00-0000-7000-8003-{seed:X12}"));

    private static T Accepted<T>(SemanticResult<T> result)
        where T : notnull =>
        ((SemanticResult<T>.Accepted)result).Value;
}
