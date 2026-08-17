using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Nodes;
using Json.Schema;

namespace ProjectBuilder.Contract.Tests;

public sealed class PortableContractTests
{
    private static readonly ConcurrentDictionary<string, Lazy<JsonSchema>> Schemas = new(StringComparer.Ordinal);

    [TestCase("project-builder-model.schema.json", "Examples", "pos-example.project-builder.json")]
    [TestCase("project-builder-model.schema.json", "Dogfood", "project-builder-foundation.project-builder.json")]
    [TestCase("project-builder-model.schema.json", "Examples", "example-importable-project.project-builder.json")]
    [TestCase("project-builder-changeset.schema.json", "Examples", "example-change-set.json")]
    [TestCase("project-builder-projection.schema.json", "Examples", "example-projection.json")]
    public void Fixture_is_valid_against_its_declared_schema(
        string schemaFile,
        string fixtureGroup,
        string fixtureFile)
    {
        var contracts = Path.Combine(TestContext.CurrentContext.TestDirectory, "Contracts");
        var schemaPath = Path.Combine(contracts, "Schemas", schemaFile);
        var schema = Schemas.GetOrAdd(
            schemaPath,
            path => new Lazy<JsonSchema>(() => JsonSchema.FromText(File.ReadAllText(path)), true)).Value;
        using var fixture = JsonDocument.Parse(File.ReadAllText(Path.Combine(contracts, fixtureGroup, fixtureFile)));

        var result = schema.Evaluate(fixture.RootElement, new EvaluationOptions
        {
            OutputFormat = OutputFormat.List,
            RequireFormatValidation = true
        });

        Assert.That(result.IsValid, Is.True, result.ToString());
    }

    [Test]
    public void Dogfood_identifiers_resolve_and_semantic_elements_have_deterministic_order()
    {
        var path = Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "Contracts",
            "Dogfood",
            "project-builder-foundation.project-builder.json");
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var root = document.RootElement;
        var elements = root.GetProperty("elements").EnumerateArray().ToArray();
        var elementIds = elements.Select(element => element.GetProperty("id").GetString()).ToHashSet(StringComparer.Ordinal);
        var elementKinds = elements.ToDictionary(
            element => element.GetProperty("id").GetString()!,
            element => element.GetProperty("kind").GetString()!,
            StringComparer.Ordinal);
        var orders = elements.Select(element => element.GetProperty("order").GetInt32()).ToArray();
        var intendedOutcomeIds = root.GetProperty("project").GetProperty("intendedOutcomeIds").EnumerateArray()
            .Select(identifier => identifier.GetString()!)
            .ToArray();
        var capabilityOutcomeIds = elements
            .Where(element => element.GetProperty("kind").GetString() == "capability")
            .SelectMany(element => element.GetProperty("payload").GetProperty("outcomeIds").EnumerateArray())
            .Select(identifier => identifier.GetString()!)
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(orders, Is.Ordered.Ascending);
            Assert.That(elementIds.Count, Is.EqualTo(elements.Length));
            Assert.That(root.GetProperty("relations").EnumerateArray().All(relation =>
                elementIds.Contains(relation.GetProperty("sourceId").GetString())
                && elementIds.Contains(relation.GetProperty("targetId").GetString())), Is.True);
            Assert.That(intendedOutcomeIds, Is.All.Matches<string>(identifier =>
                elementKinds.TryGetValue(identifier, out var kind) && kind == "outcome"));
            Assert.That(capabilityOutcomeIds, Is.All.Matches<string>(identifier =>
                elementKinds.TryGetValue(identifier, out var kind) && kind == "outcome"));
        });
    }

    [Test]
    public void Canonical_fixture_serialization_is_repeatable()
    {
        var path = Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "Contracts",
            "Dogfood",
            "project-builder-foundation.project-builder.json");
        var node = JsonNode.Parse(File.ReadAllText(path));
        var options = new JsonSerializerOptions { WriteIndented = true };

        var first = node!.ToJsonString(options);
        var second = node.ToJsonString(options);

        Assert.That(second, Is.EqualTo(first));
    }
}
