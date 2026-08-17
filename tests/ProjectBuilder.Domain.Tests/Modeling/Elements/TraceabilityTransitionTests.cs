using ProjectBuilder.Domain.Modeling.Elements;
using ProjectBuilder.Domain.Modeling.Primitives;
using ProjectBuilder.Domain.Modeling.Traceability;
using ProjectBuilder.Domain.Modeling.Transitions;
using ProjectBuilder.Domain.Projects;

namespace ProjectBuilder.Domain.Tests.Modeling.Elements;

public sealed class TraceabilityTransitionTests
{
    private static readonly string[] OperationNames = ["ClaimAdded", "EvidenceAdded"];
    [Test]
    public void Claim_and_evidence_are_committed_at_one_revision_with_explicit_scope()
    {
        var result = TraceabilityTransition.Define(Project(), RevisionValue(3), ClaimId.From(Element(50)),
            EvidenceId.From(Element(51)), Draft(), ChangeSet(4), Reason(), Now, "reviewer");
        var accepted = (DefineEvidencePacketTransitionResult.Accepted)result;
        Assert.Multiple(() =>
        {
            Assert.That(accepted.Project.Revision.Value, Is.EqualTo(4));
            Assert.That(accepted.Packet.Claim.ElementIds, Is.EqualTo(new[] { Element(20) }));
            Assert.That(accepted.Packet.Evidence.ModelRevision.Value, Is.EqualTo(4));
            Assert.That(accepted.Packet.Claim.EvidenceId, Is.EqualTo(accepted.Packet.Evidence.Id));
            Assert.That(accepted.ChangeSet.Operations.Select(item => item.GetType().Name), Is.EqualTo(OperationNames));
        });
    }

    [Test]
    public void Passed_evidence_cannot_hide_an_unknown_result()
    {
        var result = TraceabilityTransition.Define(Project(), RevisionValue(3), ClaimId.From(Element(50)),
            EvidenceId.From(Element(51)), Draft() with { Summary = Statement("Unknown") }, ChangeSet(4), Reason(), Now, "reviewer");
        Assert.That(((DefineEvidencePacketTransitionResult.Invalid)result).Errors.Select(item => item.Code), Does.Contain("PB-EVID-004"));
    }

    private static EvidencePacketDraft Draft() => new(ClaimKind.Behavior, Statement("Contributor verifies the repository."), ClaimStatus.Required,
        [Element(20)], Element(10), [Term("BDD")], EvidenceKind.EndToEndTest, EvidenceStatus.Passed,
        Term("ProjectBuilder.EndToEnd.Tests"), Term("Kestrel PostgreSQL Chromium"), Statement("The complete journey passed."), [Term("One example path")]);
    private static ProjectDefinition Project() { var project = ProjectDefinition.Create(ProjectIdValue(), Accepted(WorkspaceId.Parse("0198ad00-0000-7000-8000-000000000700")), Name("Evidence"), Accepted(ProjectPurpose.Create("Preserve proof.")), Accepted(IntendedOutcome.Create("Reviewer traces proof.")), ChangeSet(1), Reason(), Now, "reviewer"); return ProjectDefinition.Restore(project.Id, project.WorkspaceId, project.Name, project.Purpose, project.IntendedOutcome, RevisionValue(3), project.Creation); }
    private static readonly UtcTimestamp Now = UtcTimestamp.Create(new DateTimeOffset(2026, 8, 16, 0, 0, 0, TimeSpan.Zero));
    private static ProjectId ProjectIdValue() => Accepted(ProjectId.Parse("0198ad00-0000-7000-8100-000000000001")); private static ElementId Element(int n) => Accepted(ElementId.Parse($"0198ad00-0000-7000-8500-{n:X12}")); private static ChangeSetId ChangeSet(int n) => Accepted(ChangeSetId.Parse($"0198ad00-0000-7000-9500-{n:X12}")); private static Revision RevisionValue(long n) => Accepted(Revision.Create(n)); private static ChangeReason Reason() => Accepted(ChangeReason.Create("Attach attributable proof.")); private static ElementName Name(string v) => Accepted(ElementName.Create(v)); private static LogicTerm Term(string v) => Accepted(LogicTerm.Create(v)); private static LogicStatement Statement(string v) => Accepted(LogicStatement.Create(v)); private static T Accepted<T>(SemanticResult<T> result) where T : notnull => ((SemanticResult<T>.Accepted)result).Value;
}
