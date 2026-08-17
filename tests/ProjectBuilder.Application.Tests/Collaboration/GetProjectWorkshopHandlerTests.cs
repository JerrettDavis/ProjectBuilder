using System.Text.Json;

using ProjectBuilder.Application.Collaboration.GetProjectWorkshop;
using ProjectBuilder.Application.Modeling;
using ProjectBuilder.Application.Projects.CreateProject;

namespace ProjectBuilder.Application.Tests.Collaboration;

public sealed class GetProjectWorkshopHandlerTests
{
    [Test]
    public void Same_revision_and_profile_produce_the_same_ordered_workshop_brief()
    {
        var model = Model();

        var first = GetProjectWorkshopHandler.Evaluate(model);
        var second = GetProjectWorkshopHandler.Evaluate(model);

        Assert.Multiple(() =>
        {
            Assert.That(JsonSerializer.Serialize(first), Is.EqualTo(JsonSerializer.Serialize(second)));
            Assert.That(first.BriefVersion, Is.EqualTo("workshop/1"));
            Assert.That(first.Agenda.Select(item => item.Id), Is.EqualTo([
                "workshop.align", "workshop.voices", "workshop.behavior", "workshop.tensions", "workshop.decide", "workshop.close"]));
            Assert.That(first.Agenda.Sum(item => item.Minutes), Is.EqualTo(65));
            Assert.That(first.Agenda.Single(item => item.Id == "workshop.behavior").Status, Is.EqualTo("Needs definition"));
            Assert.That(first.FocusItems[0].Kind, Is.EqualTo("Recommendation"));
        });
    }

    [Test]
    public void Purpose_profile_changes_workshop_pressure_without_changing_model_revision()
    {
        var discovery = GetProjectWorkshopHandler.Evaluate(Model(), "discovery");
        var implementation = GetProjectWorkshopHandler.Evaluate(Model(), "implementation-ready");

        Assert.Multiple(() =>
        {
            Assert.That(discovery.ProfileName, Is.EqualTo("Discovery"));
            Assert.That(implementation.ProfileName, Is.EqualTo("Implementation Ready"));
            Assert.That(discovery.Revision, Is.EqualTo(implementation.Revision));
            Assert.That(discovery.BriefVersion, Is.EqualTo(implementation.BriefVersion));
        });
    }

    private static ProjectModelOverview Model() => new(
        new ProjectOverview("project-1", "workspace-1", "Workshop proof", "Reach shared understanding.",
            "A facilitator can close with explicit next work.", 2, "Create workshop proof.", "2026-08-16T00:00:00Z"),
        [new("actor-1", "Facilitator", "humanRole", "Guides the conversation", ["Protect shared understanding"], [], [], [], "known")],
        [new("outcome-1", "Shared direction", "The next work is explicit.", ["Owner named"], "actor-1", "Facilitator", "known")],
        [], [], [], [],
        [new("change-2", 1, 2, "outcome.added", "Make direction observable.", "local-modeler", "2026-08-16T02:00:00Z", 1, "Outcome added", [])]);
}
