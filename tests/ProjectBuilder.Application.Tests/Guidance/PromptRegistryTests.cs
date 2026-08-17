using System.Collections.Immutable;
using ProjectBuilder.Application.Guidance;

namespace ProjectBuilder.Application.Tests.Guidance;

public sealed class PromptRegistryTests
{
    [Test]
    public void Built_in_registry_is_valid_versioned_and_deterministically_ordered()
    {
        var registry = new PromptRegistry();

        Assert.Multiple(() =>
        {
            Assert.That(registry.Findings, Is.Empty);
            Assert.That(registry.All.Length, Is.EqualTo(6));
            Assert.That(registry.All.Select(item => item.Stage), Is.Ordered);
            Assert.That(registry.All, Is.All.Matches<PromptDescriptor>(prompt => prompt.Version == 1));
            Assert.That(registry.All, Is.All.Matches<PromptDescriptor>(prompt => prompt.AnswerMappings.Length == 5));
        });
    }

    [Test]
    public void Contradictory_applicability_is_reported_as_unreachable()
    {
        var prompt = Descriptor() with
        {
            AppliesWhen = [new(GuidanceFact.HasActors, true), new(GuidanceFact.HasActors, false)]
        };

        var findings = PromptRegistry.Validate([prompt]);

        Assert.That(findings.Select(item => item.Code), Does.Contain("GUIDE-REG-006"));
    }

    [Test]
    public void Applicability_never_treats_a_missing_fact_as_an_answer()
    {
        var registry = new PromptRegistry();
        var facts = Enum.GetValues<GuidanceFact>().ToDictionary(item => item, _ => false);

        var prompts = registry.Applicable(facts);

        Assert.Multiple(() =>
        {
            Assert.That(prompts.Select(item => item.Id), Is.EqualTo([
                "guide.frame.observable-outcome", "guide.participants.accountable-actor"]));
            Assert.That(prompts.Any(item => item.Id == "guide.behavior.coherent-scenario"), Is.False);
        });
    }

    private static PromptDescriptor Descriptor() => new(
        "guide.frame.test", 1, GuidanceStage.Frame, 1, "Question?", "Why.", "Learn.", "Trigger.",
        [new(GuidanceFact.HasActors, false)], ["Actor"], ["Example"],
        [new("author", "Author", GuidanceAnswerKind.Author, "Create Actor.", false, "/projects/{projectId}/actors/new")],
        "/projects/{projectId}/actors/new");
}
