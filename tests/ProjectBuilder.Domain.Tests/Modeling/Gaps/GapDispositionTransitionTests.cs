using ProjectBuilder.Domain.Modeling.Gaps;
using ProjectBuilder.Domain.Modeling.Primitives;
using ProjectBuilder.Domain.Modeling.Transitions;
using ProjectBuilder.Domain.Projects;

namespace ProjectBuilder.Domain.Tests.Modeling.Gaps;

public sealed class GapDispositionTransitionTests
{
    [Test]
    public void Accepted_risk_requires_authority_rationale_consequence_and_expiration_then_advances_once()
    {
        var project = Project();
        var result = GapDispositionTransition.Record(project, Revision(1), GapId(), "implementation-ready", "PB-STATE-011",
            Element("0198ad00-0000-7000-8000-000000000001"), GapDispositionKind.AcceptedRisk,
            "The state model is intentionally deferred for the exploratory prototype.",
            "Implementation cannot begin until explicit state and invariant definitions exist.",
            Element("0198ad00-0000-7000-8000-000000000012"), "2026-09-01", "C11",
            Change("0198ad00-0000-7000-8000-000000000701"), Reason("Record an accountable temporary risk."), Time(), "local-reviewer");

        var accepted = result as RecordGapDispositionTransitionResult.Accepted;
        Assert.Multiple(() =>
        {
            Assert.That(accepted, Is.Not.Null);
            Assert.That(accepted!.Project.Revision.Value, Is.EqualTo(2));
            Assert.That(accepted.Disposition.Disposition, Is.EqualTo(GapDispositionKind.AcceptedRisk));
            Assert.That(accepted.ChangeSet.ChangeKind, Is.EqualTo("gap.disposition.recorded"));
            Assert.That(accepted.ChangeSet.Operations.Single(), Is.TypeOf<ProjectChangeOperation.GapDispositionRecorded>());
        });
    }

    [Test]
    public void Deferred_without_review_or_milestone_is_rejected_without_revision_change()
    {
        var project = Project();
        var result = GapDispositionTransition.Record(project, Revision(1), GapId(), "implementation-ready", "PB-STATE-011",
            Element("0198ad00-0000-7000-8000-000000000001"), GapDispositionKind.Deferred,
            "Wait for the next bounded slice.", "Implementation remains blocked.",
            Element("0198ad00-0000-7000-8000-000000000012"), null, null,
            Change("0198ad00-0000-7000-8000-000000000701"), Reason("Attempt incomplete disposition."), Time(), "local-reviewer");

        var invalid = result as RecordGapDispositionTransitionResult.Invalid;
        Assert.Multiple(() =>
        {
            Assert.That(invalid, Is.Not.Null);
            Assert.That(invalid!.Errors.Select(error => error.Code), Does.Contain("gap.review_on.required"));
            Assert.That(invalid.Errors.Select(error => error.Code), Does.Contain("gap.target_milestone.required"));
            Assert.That(project.Revision.Value, Is.EqualTo(1));
        });
    }

    private static ProjectDefinition Project() => ProjectDefinition.Create(
        ProjectId(), WorkspaceId(), Name(),
        Accepted(ProjectPurpose.Create("Purpose-led modeling.")),
        Accepted(IntendedOutcome.Create("A contributor can run and verify the repository.")),
        Change("0198ad00-0000-7000-9000-000000000001"),
        Reason("Create the project."), Time(), "local-modeler");
    private static ProjectId ProjectId() => ((SemanticResult<ProjectId>.Accepted)ProjectBuilder.Domain.Modeling.Primitives.ProjectId.Parse("0198ad00-0000-7000-8000-000000000001")).Value;
    private static WorkspaceId WorkspaceId() => ((SemanticResult<WorkspaceId>.Accepted)ProjectBuilder.Domain.Modeling.Primitives.WorkspaceId.Parse("0198ad00-0000-7000-8000-000000000002")).Value;
    private static ElementId Element(string value) => ((SemanticResult<ElementId>.Accepted)ElementId.Parse(value)).Value;
    private static GapDispositionId GapId() => GapDispositionId.From(Element("0198ad00-0000-7000-8000-000000000700"));
    private static ChangeSetId Change(string value) => ((SemanticResult<ChangeSetId>.Accepted)ChangeSetId.Parse(value)).Value;
    private static Revision Revision(long value) => Accepted(ProjectBuilder.Domain.Modeling.Primitives.Revision.Create(value));
    private static ElementName Name() => ((SemanticResult<ElementName>.Accepted)ElementName.Create("Project Builder")).Value;
    private static ChangeReason Reason(string value) => ((SemanticResult<ChangeReason>.Accepted)ChangeReason.Create(value)).Value;
    private static UtcTimestamp Time() => UtcTimestamp.Create(new DateTimeOffset(2026, 8, 16, 17, 30, 0, TimeSpan.Zero));
    private static T Accepted<T>(SemanticResult<T> result) where T : notnull => ((SemanticResult<T>.Accepted)result).Value;
}
