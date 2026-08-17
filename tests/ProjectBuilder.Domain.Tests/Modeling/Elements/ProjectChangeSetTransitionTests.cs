using ProjectBuilder.Domain.Modeling.Elements;
using ProjectBuilder.Domain.Modeling.Primitives;
using ProjectBuilder.Domain.Modeling.Relations;
using ProjectBuilder.Domain.Modeling.Transitions;
using ProjectBuilder.Domain.Projects;

namespace ProjectBuilder.Domain.Tests.Modeling.Elements;

public sealed class ProjectChangeSetTransitionTests
{
    [Test]
    public void Typed_operations_commit_once_with_deterministic_sequence_and_audit()
    {
        var project = Project();
        var actor = Actor(project);
        var draft = new DraftProjectChangeSet(
            ChangeSet(2), actor.Id, "actor.added", Reason(),
            ProjectChangeSetTransition.AddedElements([actor]));

        var result = ProjectChangeSetTransition.Commit(project, Revision.Initial, draft, Now, "modeler");

        var accepted = (ProjectChangeSetTransitionResult.Accepted)result;
        Assert.Multiple(() =>
        {
            Assert.That(accepted.Project.Revision.Value, Is.EqualTo(2));
            Assert.That(accepted.ChangeSet.BaseRevision, Is.EqualTo(Revision.Initial));
            Assert.That(accepted.ChangeSet.ResultRevision.Value, Is.EqualTo(2));
            Assert.That(accepted.ChangeSet.Reason, Is.EqualTo(Reason()));
            Assert.That(accepted.ChangeSet.CreatedBy, Is.EqualTo("modeler"));
            Assert.That(accepted.ChangeSet.Operations.Single().Sequence, Is.Zero);
        });
    }

    [Test]
    public void Stale_draft_returns_structured_conflict_without_advancing_state()
    {
        var project = AtRevision(Project(), Accepted(Revision.Create(3)));
        var actor = Actor(project);
        var draft = new DraftProjectChangeSet(
            ChangeSet(3), actor.Id, "actor.added", Reason(),
            ProjectChangeSetTransition.AddedElements([actor]));

        var result = ProjectChangeSetTransition.Commit(project, Revision.Initial, draft, Now, "modeler");

        var conflict = (ProjectChangeSetTransitionResult.Conflict)result;
        Assert.Multiple(() =>
        {
            Assert.That(conflict.Expected, Is.EqualTo(Revision.Initial));
            Assert.That(conflict.Actual.Value, Is.EqualTo(3));
            Assert.That(conflict.Conflicts.Single().Code, Is.EqualTo("project.revision.conflict"));
            Assert.That(project.Revision.Value, Is.EqualTo(3));
        });
    }

    [Test]
    public void Empty_or_non_deterministically_ordered_draft_is_invalid()
    {
        var project = Project();
        var primaryId = Element(9);
        var empty = ProjectChangeSetTransition.Commit(
            project, Revision.Initial,
            new(ChangeSet(4), primaryId, "model.changed", Reason(), []), Now, "modeler");
        var outOfOrder = ProjectChangeSetTransition.Commit(
            project, Revision.Initial,
            new(ChangeSet(5), primaryId, "model.changed", Reason(),
                [new ProjectChangeOperation.ElementAdded(1, primaryId, ModelElementKind.Actor, Name("Actor"))]),
            Now, "modeler");

        Assert.Multiple(() =>
        {
            Assert.That(((ProjectChangeSetTransitionResult.Invalid)empty).Errors.Select(error => error.Code),
                Does.Contain("PB-CHANGE-001"));
            Assert.That(((ProjectChangeSetTransitionResult.Invalid)outOfOrder).Errors.Select(error => error.Code),
                Does.Contain("PB-CHANGE-002"));
        });
    }

    private static readonly UtcTimestamp Now = UtcTimestamp.Create(
        new DateTimeOffset(2026, 8, 16, 2, 0, 0, TimeSpan.Zero));

    private static ProjectDefinition Project() => ProjectDefinition.Create(
        Accepted(ProjectId.Parse("0198ad00-0000-7000-8000-000000000901")),
        Accepted(WorkspaceId.Parse("0198ad00-0000-7000-8000-000000000902")),
        Name("Change-set project"),
        Accepted(ProjectPurpose.Create("Prove typed atomic changes.")),
        Accepted(IntendedOutcome.Create("A modeler reviews committed operations.")),
        ChangeSet(1), Reason(), Now, "modeler");

    private static ActorDefinition Actor(ProjectDefinition project) => new(
        Element(1), project.Id, Name("Contributor"),
        Accepted(ContextualRole.Create("Authors a reviewed change set.")),
        ActorKind.HumanRole, [], [], [], [], 0, Now, "modeler");

    private static ProjectDefinition AtRevision(ProjectDefinition project, Revision revision) =>
        ProjectDefinition.Restore(
            project.Id, project.WorkspaceId, project.Name, project.Purpose,
            project.IntendedOutcome, revision, project.Creation);

    private static ElementName Name(string value) => Accepted(ElementName.Create(value));
    private static ChangeReason Reason() => Accepted(ChangeReason.Create("Commit typed operations."));
    private static ElementId Element(int seed) => Accepted(ElementId.Parse($"0198ad00-0000-7000-8001-{seed:X12}"));
    private static ChangeSetId ChangeSet(int seed) => Accepted(ChangeSetId.Parse($"0198ad00-0000-7000-8003-{seed:X12}"));
    private static T Accepted<T>(SemanticResult<T> result) where T : notnull =>
        ((SemanticResult<T>.Accepted)result).Value;
}
