using System.Text.Json;
using ProjectBuilder.Contracts.Projects;
using ProjectBuilder.Projections.Lenses;

namespace ProjectBuilder.Architecture.Tests;

public sealed class LensProjectionContractTests
{
    private static readonly string[] FilteredKinds = ["project", "outcome", "outcome"];
    private static readonly string[] BenefitRelationKinds = ["benefitsFrom"];

    [Test]
    public void Same_revision_and_request_produce_byte_identical_contracts()
    {
        var first = ProjectDefinitionLensProjector.Project(Model(), LensProjectionRequest.All);
        var second = ProjectDefinitionLensProjector.Project(Model(), LensProjectionRequest.All);

        Assert.That(JsonSerializer.Serialize(first), Is.EqualTo(JsonSerializer.Serialize(second)));
        Assert.That(first.ContentHash, Has.Length.EqualTo(64));
    }

    [Test]
    public void Input_collection_order_does_not_change_projection_identity()
    {
        var original = Model();
        var reordered = original with { Actors = original.Actors.Reverse().ToArray(), Outcomes = original.Outcomes.Reverse().ToArray() };

        var first = ProjectDefinitionLensProjector.Project(original, LensProjectionRequest.All);
        var second = ProjectDefinitionLensProjector.Project(reordered, LensProjectionRequest.All);

        Assert.Multiple(() =>
        {
            Assert.That(second.ContentHash, Is.EqualTo(first.ContentHash));
            Assert.That(second.ProjectionId, Is.EqualTo(first.ProjectionId));
        });
    }

    [Test]
    public void Filter_keeps_context_and_never_emits_a_dangling_edge()
    {
        var projection = ProjectDefinitionLensProjector.Project(Model(), new(["outcome"], [], null));

        Assert.Multiple(() =>
        {
            Assert.That(projection.Nodes.Select(node => node.Kind), Is.EqualTo(FilteredKinds));
            Assert.That(projection.Edges, Is.Empty);
            Assert.That(projection.Diagnostics.Select(item => item.Code), Does.Contain("lens.filter.edge-suppressed"));
            Assert.That(LensProjectionValidator.Validate(projection), Is.Empty);
        });
    }

    [Test]
    public void Typed_benefit_relation_uses_owned_directional_ports()
    {
        var projection = ProjectDefinitionLensProjector.Project(Model(), LensProjectionRequest.All);
        var edge = projection.Edges.Single();

        Assert.Multiple(() =>
        {
            Assert.That(projection.Nodes.Single(node => node.Id == edge.SourceNodeId).Ports.Single().Direction, Is.EqualTo("output"));
            Assert.That(projection.Nodes.Single(node => node.Id == edge.TargetNodeId).Ports.Single().Direction, Is.EqualTo("input"));
            Assert.That(projection.Nodes.SelectMany(node => node.Ports).SelectMany(port => port.RelationKinds).Distinct(), Is.EqualTo(BenefitRelationKinds));
        });
    }

    [Test]
    public void Validator_catches_an_intentional_topology_violation()
    {
        var valid = ProjectDefinitionLensProjector.Project(Model(), LensProjectionRequest.All);
        var invalidEdge = valid.Edges.Single() with { TargetNodeId = "node:missing" };
        var invalid = valid with { Edges = [invalidEdge] };

        var findings = LensProjectionValidator.Validate(invalid);

        Assert.That(findings, Does.Contain($"Edge '{invalidEdge.Id}' references missing node 'node:missing'."));
        Assert.That(() => LensProjectionValidator.EnsureValid(invalid), Throws.InvalidOperationException);
    }

    private static ProjectModelResponse Model() => new(
        new("project-01", "workspace-01", "Project Builder", "Model systems from explicit definitions.",
            "A contributor can run and verify the repository.", 31, "Bootstrap.", "2026-08-16T00:00:00Z", "Inspect the lens."),
        [
            new("actor-reviewer", "Reviewer", "Person", "Reviews evidence.", [], [], [], [], "assumed"),
            new("actor-contributor", "Contributor", "Person", "Builds and verifies the repository.", [], [], ["Commit changes"], [], "known"),
        ],
        [
            new("outcome-secondary", "Review confidence", "Evidence is inspectable.", ["Review passes"], "actor-reviewer", "Reviewer", "assumed"),
            new("outcome-run", "Executable foundation", "A clean clone builds and runs.", ["Build passes", "Health is ready"], "actor-contributor", "Contributor", "known"),
        ], [], [], [],
        [new("relation-benefit", "benefitsFrom", "Contributor benefits from Executable foundation", "actor-contributor", "actor", "Contributor", "outcome-run", "outcome", "Executable foundation", "directed", "many-to-one", true, "source", "restrict")],
        []);
}
