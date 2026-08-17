using System.Text.Json;
using ProjectBuilder.Contracts.Projects;
using ProjectBuilder.Projections.Lenses;

namespace ProjectBuilder.Architecture.Tests;

public sealed class StateRuleProjectionTests
{
    private static readonly string[] ExpectedRepresentations = ["graph", "transition-matrix", "rule-matrix", "invariant-panel"];

    [Test]
    public void Same_state_truth_produces_byte_identical_graph_and_matrices()
    {
        var first = StateRuleLensProjector.Project(Model(), "state-project");
        var second = StateRuleLensProjector.Project(Model(), "state-project");

        Assert.Multiple(() =>
        {
            Assert.That(JsonSerializer.Serialize(second), Is.EqualTo(JsonSerializer.Serialize(first)));
            Assert.That(first.ContractVersion, Is.EqualTo("state-rule/1"));
            Assert.That(first.Representations, Is.EqualTo(ExpectedRepresentations));
            Assert.That(first.Transitions, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void Transition_references_and_derived_predicates_declare_distinct_provenance()
    {
        var projection = StateRuleLensProjector.Project(Model(), "state-project");

        Assert.Multiple(() =>
        {
            Assert.That(projection.Nodes.Single(node => node.SemanticReference == "transition-create").Origin, Is.EqualTo("semantic-element"));
            Assert.That(projection.Nodes.Single(node => node.SemanticReference == "transition-create:source").Origin, Is.EqualTo("derived-explicit-field"));
            Assert.That(projection.Edges.Any(edge => edge.Kind == "changesFact" && edge.Origin == "semantic-transition-reference"), Is.True);
            Assert.That(projection.Edges.Any(edge => edge.Kind == "requestsEffect" && edge.Origin == "semantic-path-reference"), Is.True);
            Assert.That(projection.Nodes.Single(node => node.Kind == "fact").KnowledgeStatus, Is.EqualTo("unknown-capable"));
        });
    }

    [Test]
    public void Missing_event_model_is_reported_and_no_event_is_invented()
    {
        var projection = StateRuleLensProjector.Project(Model() with { Paths = [] }, "state-project");

        Assert.Multiple(() =>
        {
            Assert.That(projection.Nodes.Select(node => node.Kind), Does.Not.Contain("event"));
            Assert.That(projection.Diagnostics.Select(item => item.Code), Does.Contain("state-rule.events.unmodeled"));
            Assert.That(projection.Diagnostics.Select(item => item.Code), Does.Contain("state-rule.effects.unmodeled"));
        });
    }

    [Test]
    public void Validator_rejects_an_edge_with_a_missing_endpoint()
    {
        var projection = StateRuleLensProjector.Project(Model(), "state-project");
        var invalid = projection with { Edges = projection.Edges.Append(new("corrupt", "changesFact", "missing", projection.Nodes[0].Id, "Changes", "solid", "test")).ToArray() };

        Assert.That(() => StateRuleProjectionValidator.EnsureValid(invalid),
            Throws.InvalidOperationException.With.Message.Contains("missing source"));
    }

    private static ProjectModelResponse Model() => new(
        new("project-state", "workspace", "State project", "Trace state truth.", "Rules are inspectable.", 7, "Fixture.", "2026-08-16T00:00:00Z", "Open state."),
        [new("actor-modeler", "Modeler", "humanRole", "Owns model truth.", [], [], [], [], "known")],
        [], [],
        [new("state-project", "Project definition", "domain", ["DefinitionStatus", "Revision"], ["Unmodeled", "Defined"], "Modeler",
            "fact-purpose", "Purpose recorded", "boolean", "The project aggregate owns accepted purpose truth.", "transitioned",
            "rule-valid", "Definition validity", "validation", "Name and purpose must be valid.",
            "invariant-once", "Revision advances once", "One accepted operation cannot advance twice.",
            ["Transition example", "Idempotency property"],
            [new("result-created", "Created", "success", "The definition is created."), new("result-invalid", "Invalid", "invalid", "State is unchanged.")],
            "transition-create", "Create definition", "No accepted definition exists.", "The modeler submits a valid definition.", "A definition exists at revision one.",
            "actor-modeler", ["known", "unknown", "assumed"], "actor-modeler", "invariant-once", ["state-project"],
            ["fact-purpose"], ["rule-valid"], ["invariant-once"], ["result-created", "result-invalid"])],
        [new("path-invalid", "Invalid definition", "exceptional", "Scenario", "Create definition", "Definition invalid", "branch",
            "Validation fails", ["Reject"], "Invalid", "invalid", "Unchanged", "Findings visible", "Modeler",
            "Present findings", "observation", "Show actionable findings", "recovery", "Correct", "correctAndRetry",
            "Correct input", ["Edit fields"], "Created", "Defined", "Definition visible", "Retry after correction",
            "No duplicate", "Created or stop", "No partial state", "scenario", "transition-create", "condition", "result-invalid",
            "actor-modeler", "effect-findings", "recovery-condition", "result-created")],
        [], [], []);
}
