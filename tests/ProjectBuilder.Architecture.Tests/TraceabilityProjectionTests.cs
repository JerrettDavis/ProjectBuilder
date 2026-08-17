using ProjectBuilder.Contracts.Projects;
using ProjectBuilder.Projections.Lenses;

namespace ProjectBuilder.Architecture.Tests;

public sealed class TraceabilityProjectionTests
{
    private static readonly string[] ExplicitOrigins = ["claim-explicit-reference", "evidence-explicit-reference"];
    [Test]
    public void Projection_is_deterministic_and_never_uses_test_count_as_evidence()
    {
        var first = TraceabilityLensProjector.Project(Model(), Trace()); var second = TraceabilityLensProjector.Project(Model(), Trace());
        Assert.Multiple(() =>
        {
            Assert.That(first.ContentHash, Is.EqualTo(second.ContentHash));
            Assert.That(first.ContractVersion, Is.EqualTo("traceability/1"));
            Assert.That(first.OutcomeTraces.Single().Status, Is.EqualTo("supported"));
            Assert.That(first.Edges.Select(item => item.Origin), Is.EquivalentTo(ExplicitOrigins));
        });
    }

    [Test]
    public void Later_semantic_change_marks_evidence_for_review()
    {
        var projection = TraceabilityLensProjector.Project(Model() with { ChangeSets = Model().ChangeSets.Append(Change(8)).ToArray() }, Trace(), "impact");
        Assert.That(projection.Impact.Single().Status, Is.EqualTo("review-required"));
    }

    [Test]
    public void Missing_endpoint_is_rejected()
    {
        var projection = TraceabilityLensProjector.Project(Model(), Trace());
        var invalid = projection with { Edges = projection.Edges.Append(new("bad", "proves", "missing", projection.Nodes[0].Id, "bad", "solid", "test")).ToArray() };
        Assert.That(() => TraceabilityProjectionValidator.EnsureValid(invalid), Throws.InvalidOperationException.With.Message.Contains("missing endpoint"));
    }

    private static TraceabilityResponse Trace() => new([new("claim", "behavior", "Contributor verifies the repository.", "required", ["outcome"], "actor", "Reviewer", ["BDD"], "2026-08-16T00:00:00Z", "reviewer")],
        [new("evidence", "endToEndTest", "passed", "claim", "ProjectBuilder.EndToEnd.Tests", "2026-08-16T00:00:00Z", 5, "Kestrel PostgreSQL Chromium", "Journey passed.", ["One example"], "reviewer")]);
    private static ProjectModelResponse Model() => new(new("project", "workspace", "Evidence", "Trace proof", "Reviewer sees proof", 7, "Fixture", "2026-08-16T00:00:00Z", "Review"),
        [new("actor", "Reviewer", "humanRole", "Reviews", [], [], [], [], "known")],
        [new("outcome", "Repository verified", "Contributor verifies the repository.", ["Checks pass"], "actor", "Reviewer", "known")], [], [], [], [], [Change(4)], []);
    private static ChangeSetResponse Change(long revision) => new($"change-{revision}", revision - 1, revision, "outcome.updated", "Update", "reviewer", "2026-08-16T00:00:00Z", 1, "Updated", [new(0, "element.updated", "outcome", "outcome", null, "Updated")]);
}
