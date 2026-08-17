using System.Text.Json;
using ProjectBuilder.Contracts.Projects;
using ProjectBuilder.Projections.Lenses;

namespace ProjectBuilder.Architecture.Tests;

public sealed class StoryMapProjectionTests
{
    private static readonly string[] ExpectedKinds = ["outcome", "capability", "episode", "scenario", "scene", "actor", "actor"];
    private static readonly string[] ExpectedEdges = ["benefitsFrom", "contributesTo", "exercises", "contains", "contains", "participatesIn", "participatesIn"];

    [Test]
    public void Same_story_inputs_produce_byte_identical_value_to_scene_topology()
    {
        var first = StoryMapLensProjector.Project(Model(), new([], [], null, ["priority", "status"]));
        var second = StoryMapLensProjector.Project(Model(), new([], [], null, ["status", "priority"]));

        Assert.Multiple(() =>
        {
            Assert.That(JsonSerializer.Serialize(second), Is.EqualTo(JsonSerializer.Serialize(first)));
            Assert.That(first.Nodes.Select(node => node.Kind), Is.EqualTo(ExpectedKinds));
            Assert.That(first.Edges.Select(edge => edge.Kind), Is.EquivalentTo(ExpectedEdges));
            Assert.That(LensProjectionValidator.Validate(first), Is.Empty);
        });
    }

    [Test]
    public void Overlay_changes_annotations_without_changing_semantic_nodes_or_edges()
    {
        var withOverlays = StoryMapLensProjector.Project(Model(), new([], [], null, ["priority", "status"]));
        var without = StoryMapLensProjector.Project(Model(), new([], [], null, []));

        Assert.Multiple(() =>
        {
            Assert.That(without.Nodes.Select(node => node.SemanticId), Is.EqualTo(withOverlays.Nodes.Select(node => node.SemanticId)));
            Assert.That(without.Edges.Select(edge => edge.Id), Is.EqualTo(withOverlays.Edges.Select(edge => edge.Id)));
            Assert.That(withOverlays.Nodes.Single(node => node.Kind == "capability").Badges, Does.Contain("Priority · critical"));
            Assert.That(without.Nodes.Single(node => node.Kind == "capability").Badges, Does.Not.Contain("Priority · critical"));
        });
    }

    [Test]
    public void Kind_filter_suppresses_connectors_instead_of_leaving_dangling_edges()
    {
        var filtered = StoryMapLensProjector.Project(Model(), new(["episode", "scenario", "scene"], [], null));

        Assert.Multiple(() =>
        {
            Assert.That(filtered.Edges.All(edge => filtered.Nodes.Any(node => node.Id == edge.SourceNodeId) && filtered.Nodes.Any(node => node.Id == edge.TargetNodeId)), Is.True);
            Assert.That(filtered.Diagnostics.Select(item => item.Code), Does.Contain("story-map.filter.edge-suppressed"));
            Assert.That(LensProjectionValidator.Validate(filtered), Is.Empty);
        });
    }

    [Test]
    public void Missing_capability_is_reported_and_never_inferred_from_episode_names()
    {
        var model = Model() with { Capabilities = [] };
        var projection = StoryMapLensProjector.Project(model, LensProjectionRequest.All);

        Assert.Multiple(() =>
        {
            Assert.That(projection.Nodes.Any(node => node.Kind == "capability"), Is.False);
            Assert.That(projection.Diagnostics.Select(item => item.Code), Does.Contain("story-map.capability.missing"));
        });
    }

    private static ProjectModelResponse Model() => new(
        new("project-story", "workspace", "Story project", "Trace value into behavior.", "A modeled story is inspectable.", 5, "Create fixture.", "2026-08-16T00:00:00Z", "Open map."),
        [
            new("actor-modeler", "Modeler", "humanRole", "Frames the story.", [], [], ["May model"], [], "known"),
            new("actor-studio", "Project Builder", "systemRole", "Receives modeling intent.", [], [], [], [], "known"),
        ],
        [new("outcome-map", "Traceable story", "Value remains connected to concrete behavior.", ["Trace is complete"], "actor-modeler", "Modeler", "known")],
        [new("episode-bootstrap", "Bootstrap Repository", "A clean clone exists.", "The repository is healthy.", "Traceable story",
            "scenario-clean", "Clean clone is built", "happy", ["Repository cloned"], "Contributor runs verification", "Shell and evidence are available",
            "Build and run", "Local workstation", "Contributor verifies the foundation", "Run verification", "Modeler", "Project Builder",
            "Verify the repository", "Run the documented command", "Health and evidence are visible", ["Verified"],
            "outcome-map", "scene-build", "actor-modeler", "actor-studio")],
        [], [],
        [new("relation-benefit", "benefitsFrom", "Modeler benefits from traceable story", "actor-modeler", "actor", "Modeler", "outcome-map", "outcome", "Traceable story", "directed", "oneToMany", true, "target", "restrict")],
        [],
        [new("capability-verify", "Verify repository foundation", "Build, run, and verify one repository path.", ["outcome-map"], "critical", "known")]);
}
