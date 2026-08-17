using System.Text.Json;
using ProjectBuilder.Contracts.Projects;
using ProjectBuilder.Projections.Lenses;

namespace ProjectBuilder.Architecture.Tests;

public sealed class ScenarioFlowProjectionTests
{
    private static readonly string[] ExpectedPathClassifications = ["happy", "exceptional", "recovery"];
    private static readonly string[] ExpectedChangedFacts = ["Definition validity"];

    [Test]
    public void Same_scenario_and_path_truth_produce_byte_identical_flow_and_playback()
    {
        var first = ScenarioFlowLensProjector.Project(Model(), "scenario-clean");
        var second = ScenarioFlowLensProjector.Project(Model(), "scenario-clean");

        Assert.Multiple(() =>
        {
            Assert.That(JsonSerializer.Serialize(second), Is.EqualTo(JsonSerializer.Serialize(first)));
            Assert.That(first.ContractVersion, Is.EqualTo("scenario-flow/1"));
            Assert.That(first.Paths.Select(path => path.Classification), Is.EqualTo(ExpectedPathClassifications));
            Assert.That(first.Playback.GroupBy(step => step.PathId).All(group => group.Select(step => step.Position).SequenceEqual(Enumerable.Range(1, group.Count()))), Is.True);
            Assert.That(first.Overlays.Select(item => item.PathId), Is.EqualTo(first.Paths.Select(item => item.Id)));
        });
    }

    [Test]
    public void Explicit_transition_state_observation_and_invariant_become_a_review_stop_without_rule_execution()
    {
        var projection = ScenarioFlowLensProjector.Project(Model(), "scenario-clean");
        var overlay = projection.Overlays.Single(item => item.PathId == "path:branch-invalid");

        Assert.Multiple(() =>
        {
            Assert.That(overlay.BeforeState, Is.EqualTo("Definition awaits verification"));
            Assert.That(overlay.AfterState, Is.EqualTo("No semantic change is committed"));
            Assert.That(overlay.ChangedFacts, Is.EqualTo(ExpectedChangedFacts));
            Assert.That(overlay.Observation, Is.EqualTo("Findings are visible"));
            Assert.That(overlay.InvariantName, Is.EqualTo("Invalid definitions do not advance"));
            Assert.That(overlay.StopReason, Does.Contain("Review"));
        });
    }

    [Test]
    public void Semantic_elements_and_derived_field_fragments_declare_distinct_origin()
    {
        var projection = ScenarioFlowLensProjector.Project(Model(), "scenario-clean");

        Assert.Multiple(() =>
        {
            Assert.That(projection.Nodes.Single(node => node.SemanticReference == "interaction-run").Origin, Is.EqualTo("semantic-element"));
            Assert.That(projection.Nodes.Single(node => node.SemanticReference == "scenario-clean:trigger").Origin, Is.EqualTo("derived-explicit-field"));
            Assert.That(projection.Edges.Any(edge => edge.Kind == "crossesBoundary" && edge.Origin == "semantic-effect-classification"), Is.True);
            Assert.That(projection.Diagnostics.Select(item => item.Code), Does.Not.Contain("scenario-flow.boundary.unmodeled"));
        });
    }

    [Test]
    public void Missing_paths_and_boundaries_are_exposed_without_invented_branches()
    {
        var projection = ScenarioFlowLensProjector.Project(Model() with { Paths = [] }, "scenario-clean");

        Assert.Multiple(() =>
        {
            Assert.That(projection.Paths, Has.Count.EqualTo(1));
            Assert.That(projection.Paths[0].Classification, Is.EqualTo("happy"));
            Assert.That(projection.Diagnostics.Select(item => item.Code), Does.Contain("scenario-flow.paths.missing"));
            Assert.That(projection.Diagnostics.Select(item => item.Code), Does.Contain("scenario-flow.boundary.unmodeled"));
        });
    }

    [Test]
    public void Validator_rejects_a_flow_edge_with_a_missing_endpoint()
    {
        var projection = ScenarioFlowLensProjector.Project(Model(), "scenario-clean");
        var invalid = projection with { Edges = projection.Edges.Append(new("corrupt", "next", "missing", projection.Nodes[0].Id, "Then", "solid", "test", "path:primary")).ToArray() };

        Assert.That(() => ScenarioFlowProjectionValidator.EnsureValid(invalid),
            Throws.InvalidOperationException.With.Message.Contains("missing source"));
    }

    private static ProjectModelResponse Model() => new(
        new("project-flow", "workspace", "Flow project", "Trace behavior.", "Scenario playback is inspectable.", 8, "Create fixture.", "2026-08-16T00:00:00Z", "Open flow."),
        [
            new("actor-modeler", "Modeler", "humanRole", "Initiates verification.", [], [], [], [], "known"),
            new("actor-studio", "Project Builder", "systemRole", "Runs verification.", [], [], [], [], "known"),
        ],
        [new("outcome-verified", "Verified repository", "Evidence is visible.", ["Health ready"], "actor-modeler", "Modeler", "known")],
        [new("episode-bootstrap", "Bootstrap Repository", "A clean clone exists.", "The repository is healthy.", "Verified repository",
            "scenario-clean", "Clean clone is built and run", "happy", ["Repository cloned", "Docker available"], "Modeler requests verification", "Evidence is visible",
            "Verify foundation", "Local workstation", "Run and inspect verification", "Run verification", "Modeler", "Project Builder",
            "Verify foundation", "Execute the documented command", "Health and evidence are visible", ["Verified"],
            "outcome-verified", "scene-verify", "actor-modeler", "actor-studio", "interaction-run", "intent-verify", "step-run", "observation-ready")],
        [new("state-verification", "Definition verification", "Domain", ["DefinitionStatus"], ["Valid", "Invalid"], "Project Builder",
            "fact-validity", "Definition validity", "boolean", "Project model", "Transitioned",
            "rule-validity", "Definition acceptance", "Validation", "Only valid definitions produce evidence.",
            "Invalid definitions do not advance", "Rejected verification preserves current semantic state.",
            "An invalid definition advances revision.", ["Property test"],
            [new("result-invalid", "Invalid", "invalid", "Findings are returned."), new("result-verified", "Verified", "success", "Evidence is visible.")],
            "transition-accept", "Accept definition", "Definition awaits verification", "Verification requested", "Evidence is visible",
            InvariantId: "invariant-no-advance", ChangedFactIds: ["fact-validity"])],
        [new("branch-invalid", "Invalid definition", "exceptional", "Clean clone is built and run", "Accept definition",
            "Definition is invalid", "branch", "Validation finds an error", ["Reject the definition", "Present actionable findings"],
            "Invalid", "invalid", "No semantic change is committed", "Findings are visible", "Modeler",
            "Publish validation findings", "externalInteraction", "Return findings across the application boundary",
            "recovery-correct", "Correct and retry", "correctAndRetry", "The modeler corrects the definition",
            ["Preserve the draft", "Correct invalid fields", "Retry with the same intent"], "Verified", "A valid definition is committed", "Evidence is visible",
            "Retry only after correction", "One operation identity prevents duplicate commit", "A valid result or explicit stop", "No partial semantic state remains",
            "scenario-clean", "transition-accept", "condition-invalid", "result-invalid", "actor-modeler", "effect-findings", "condition-correct", "result-verified")],
        [], [], []);
}
