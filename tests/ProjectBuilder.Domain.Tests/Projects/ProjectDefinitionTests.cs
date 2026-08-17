using ProjectBuilder.Domain.Modeling.Primitives;
using ProjectBuilder.Domain.Projects;

namespace ProjectBuilder.Domain.Tests.Projects;

public sealed class ProjectDefinitionTests
{
    [Test]
    public void Creation_establishes_revision_one_and_an_auditable_change_set()
    {
        var projectId = Accepted(ProjectId.Parse("0198ad00-0000-7000-8000-000000000401"));
        var workspaceId = Accepted(WorkspaceId.Parse("0198ad00-0000-7000-8000-000000000402"));
        var changeSetId = Accepted(ChangeSetId.Parse("0198ad00-0000-7000-8000-000000000403"));
        var reason = Accepted(ChangeReason.Create("Define the initial project outcome."));
        var occurredAt = UtcTimestamp.Create(new DateTimeOffset(2026, 8, 15, 20, 0, 0, TimeSpan.Zero));

        var project = ProjectDefinition.Create(
            projectId,
            workspaceId,
            Accepted(ElementName.Create("Order discovery")),
            Accepted(ProjectPurpose.Create("Understand order fulfilment.")),
            Accepted(IntendedOutcome.Create("A modeler can explain the end-to-end episode.")),
            changeSetId,
            reason,
            occurredAt,
            "modeler-1");

        Assert.Multiple(() =>
        {
            Assert.That(project.Revision, Is.EqualTo(Revision.Initial));
            Assert.That(project.Creation.Id, Is.EqualTo(changeSetId));
            Assert.That(project.Creation.ProjectId, Is.EqualTo(projectId));
            Assert.That(project.Creation.ResultRevision, Is.EqualTo(Revision.Initial));
            Assert.That(project.Creation.Reason, Is.EqualTo(reason));
            Assert.That(project.Creation.OccurredAt, Is.EqualTo(occurredAt));
            Assert.That(project.Creation.CreatedBy, Is.EqualTo("modeler-1"));
            Assert.That(project.Creation.Operations.Single(), Is.TypeOf<ProjectBuilder.Domain.Modeling.Transitions.ProjectChangeOperation.ProjectCreated>());
        });
    }

    [TestCase(null, "project.purpose.required")]
    [TestCase("  ", "project.purpose.required")]
    public void Purpose_rejects_missing_semantic_content(string? value, string code)
    {
        AssertRejected(ProjectPurpose.Create(value), code);
    }

    [TestCase(null, "project.intended_outcome.required")]
    [TestCase("  ", "project.intended_outcome.required")]
    public void Intended_outcome_rejects_missing_semantic_content(string? value, string code)
    {
        AssertRejected(IntendedOutcome.Create(value), code);
    }

    private static T Accepted<T>(SemanticResult<T> result)
        where T : notnull
    {
        Assert.That(result, Is.TypeOf<SemanticResult<T>.Accepted>());
        return ((SemanticResult<T>.Accepted)result).Value;
    }

    private static void AssertRejected<T>(SemanticResult<T> result, string code)
        where T : notnull
    {
        Assert.That(result, Is.TypeOf<SemanticResult<T>.Rejected>());
        Assert.That(((SemanticResult<T>.Rejected)result).Error.Code, Is.EqualTo(code));
    }
}
