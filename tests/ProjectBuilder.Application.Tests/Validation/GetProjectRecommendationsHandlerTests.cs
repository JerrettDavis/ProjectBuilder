using System.Text.Json;

using ProjectBuilder.Application.Modeling;
using ProjectBuilder.Application.Projects.CreateProject;
using ProjectBuilder.Application.Validation.GetProjectRecommendations;

namespace ProjectBuilder.Application.Tests.Validation;

public sealed class GetProjectRecommendationsHandlerTests
{
    [Test]
    public void Identical_revision_produces_identical_ranked_recommendations_and_rationale()
    {
        var model = EmptyModel();

        var first = GetProjectRecommendationsHandler.Evaluate(model);
        var second = GetProjectRecommendationsHandler.Evaluate(model);

        Assert.Multiple(() =>
        {
            Assert.That(JsonSerializer.Serialize(first), Is.EqualTo(JsonSerializer.Serialize(second)));
            Assert.That(first.RuleVersion, Is.EqualTo("builtin/1"));
            Assert.That(first.PrimaryRecommendationId, Is.EqualTo("recommend.participant"));
            Assert.That(first.Candidates[0].FindingCodes, Does.Contain("PB-CONTEXT-001"));
            Assert.That(first.Candidates[0].Signals.Select(item => item.Kind),
                Is.EqualTo(["purpose", "finding", "dependency", "recent"]));
            Assert.That(first.Candidates.Single(item => item.Id == "recommend.outcome").Status, Is.EqualTo("Blocked"));
        });
    }

    [Test]
    public void Narrative_change_makes_state_the_dependency_ready_continuation_and_profile_pressure_remains_explicit()
    {
        var model = FramedBehaviorModel();

        var discovery = GetProjectRecommendationsHandler.Evaluate(model, "discovery");
        var implementation = GetProjectRecommendationsHandler.Evaluate(model, "implementation-ready");
        var discoveryState = discovery.Candidates.Single(item => item.Id == "recommend.state");
        var implementationState = implementation.Candidates.Single(item => item.Id == "recommend.state");

        Assert.Multiple(() =>
        {
            Assert.That(discovery.PrimaryRecommendationId, Is.EqualTo("recommend.state"));
            Assert.That(discoveryState.Status, Is.EqualTo("Recommended"));
            Assert.That(discoveryState.Priority, Is.EqualTo("Advisory for profile"));
            Assert.That(implementationState.Priority, Is.EqualTo("Required for profile"));
            Assert.That(discoveryState.Signals.Single(item => item.Kind == "recent").Value, Is.EqualTo("Aligned"));
            Assert.That(discoveryState.Signals.Single(item => item.Kind == "dependency").Value, Is.EqualTo("Ready"));
            Assert.That(discovery.Revision, Is.EqualTo(implementation.Revision));
        });
    }

    [Test]
    public void Recent_work_never_promotes_an_action_across_an_unsatisfied_dependency()
    {
        var model = EmptyModel() with
        {
            ChangeSets = [Change("state-logic.defined", 1)]
        };

        var result = GetProjectRecommendationsHandler.Evaluate(model, "implementation-ready");

        Assert.Multiple(() =>
        {
            Assert.That(result.PrimaryRecommendationId, Is.EqualTo("recommend.participant"));
            Assert.That(result.Candidates.Single(item => item.Id == "recommend.paths").Status, Is.EqualTo("Blocked"));
            Assert.That(result.Candidates.Single(item => item.Id == "recommend.paths").Signals.Single(item => item.Kind == "recent").Value, Is.EqualTo("Aligned"));
        });
    }

    private static ProjectModelOverview EmptyModel() => new(
        new ProjectOverview("project-1", "workspace-1", "Recommendation proof", "Explain the next useful work.",
            "A contributor can choose work without guessing.", 1, "Create recommendation proof.", "2026-08-16T00:00:00Z"),
        [], [], [], [], [], [], [Change("project.created", 1)]);

    private static ProjectModelOverview FramedBehaviorModel() => EmptyModel() with
    {
        Project = EmptyModel().Project with { Revision = 4 },
        Actors =
        [
            new("actor-1", "Clerk", "humanRole", "Initiates item entry", [], [], [], [], "known"),
            new("actor-2", "Catalog", "systemRole", "Resolves product truth", [], [], [], [], "known")
        ],
        Outcomes = [new("outcome-1", "Item joins sale", "The item is visible in the sale.", ["Description visible"], "actor-1", "Clerk", "known")],
        Narratives = [new("episode-1", "Sell merchandise", "Sale open", "Item visible", "Item joins sale",
            "scenario-1", "Scan recognized item", "Happy", ["Sale is open"], "Clerk scans", "Item joins sale",
            "scene-1", "Staffed POS", "Resolve item", "interaction-1", "Clerk", "Catalog", "Add item",
            "Resolve and append", "Item is visible", ["Added", "NotFound"])],
        ChangeSets = [Change("narrative.defined", 4), Change("outcome.added", 3), Change("actor.added", 2), Change("project.created", 1)]
    };

    private static ChangeSetOverview Change(string kind, long revision) => new(
        $"change-{revision}", revision - 1, revision, kind, $"Create {kind} proof.", "local-modeler",
        $"2026-08-16T0{revision}:00:00Z", 1, kind, []);
}
