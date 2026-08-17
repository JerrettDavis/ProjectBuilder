using ProjectBuilder.Contracts.Projects;
using ProjectBuilder.Projections.Lenses;

namespace ProjectBuilder.Architecture.Tests;

public sealed class SystemContextProjectionTests
{
    [Test]
    public void Projection_is_deterministic_and_preserves_explicit_provenance()
    {
        var first = SystemContextLensProjector.Project(Model(), "owned", "trust");
        var second = SystemContextLensProjector.Project(Model(), "owned", "trust");
        Assert.Multiple(() =>
        {
            Assert.That(first.ContentHash, Is.EqualTo(second.ContentHash));
            Assert.That(first.ContractVersion, Is.EqualTo("system-context/1"));
            Assert.That(first.DataFlows.All(flow => flow.Origin == "contract-explicit-field"), Is.True);
            Assert.That(first.Nodes.Any(node => node.Kind == "effect" && node.Origin == "semantic-reference"), Is.True);
        });
    }

    [Test]
    public void Validator_rejects_a_missing_endpoint()
    {
        var projection = SystemContextLensProjector.Project(Model(), "owned");
        var invalid = projection with { Connections = projection.Connections.Append(new("bad", "uses", "missing", projection.Nodes[0].Id, "bad", "solid", "test")).ToArray() };
        Assert.That(() => SystemContextProjectionValidator.EnsureValid(invalid), Throws.InvalidOperationException.With.Message.Contains("missing endpoint"));
    }

    private static ProjectModelResponse Model() => new(
        new("project", "workspace", "Project Builder", "Model meaning", "Contributor verifies", 8, "Fixture", "2026-08-16T00:00:00Z", "Review"),
        [new("modeler", "Modeler", "humanRole", "Models", [], [], [], [], "known"), new("dba", "Data owner", "humanRole", "Owns storage", [], [], [], [], "known")],
        [], [], [], [], [], [], [],
        [new("owned", "Project Builder", "Definition-first studio", "modeler", "Modeler", ["Preserve truth"],
            "external", "PostgreSQL", "Durable store", "dba", "Data owner", ["Store records"], "known",
            "interface", "Project API", "Stable server boundary", "http", ["modeler", "external"], ["Modeler", "PostgreSQL"], ["Commit change"], ["Committed revision"], ["Keyboard equivalent"],
            "boundary", "Persistence boundary", "Owned to external", ["ownership", "trust"], ["modeler", "dba"], ["Modeler", "Data owner"], "known", "effect", "Persist change",
            "contract", "Persistence contract", "Governs movement", "api", "1", "modeler", "Modeler", "Unknown", "Breaking changes require version", "Typed change set", "Committed revision", "Internal metadata", "known")]);
}
