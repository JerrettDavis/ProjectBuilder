using ProjectBuilder.Application.Foundation;

namespace ProjectBuilder.Application.Tests;

public sealed class FoundationDefinitionTests
{
    [Test]
    public void Description_exposes_supplied_build_identity_and_health_contracts()
    {
        var result = FoundationDefinition.Describe("1.2.3", "abc123");

        Assert.Multiple(() =>
        {
            Assert.That(result.Name, Is.EqualTo("Project Builder"));
            Assert.That(result.Version, Is.EqualTo("1.2.3"));
            Assert.That(result.Commit, Is.EqualTo("abc123"));
            Assert.That(result.ReadinessEndpoint, Is.EqualTo("/health"));
            Assert.That(result.LivenessEndpoint, Is.EqualTo("/alive"));
        });
    }
}
