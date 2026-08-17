using System.Text.Json.Nodes;
using ProjectBuilder.Application.Portability;
using ProjectBuilder.Application.Projects.CreateProject;
using ProjectBuilder.Domain.Modeling.Primitives;
using ProjectBuilder.Infrastructure.Portability;

namespace ProjectBuilder.Infrastructure.Tests.Portability;

public sealed class JsonPortableProjectCodecTests
{
    private readonly JsonPortableProjectCodec codec = new();

    [Test]
    public void Export_import_export_produces_identical_canonical_bytes()
    {
        var first = Accepted(codec.Read(Fixture()));
        var second = Accepted(codec.Read(first.CanonicalJson));

        Assert.Multiple(() =>
        {
            Assert.That(second.CanonicalJson, Is.EqualTo(first.CanonicalJson));
            Assert.That(second.ContentHash, Is.EqualTo(first.ContentHash));
            Assert.That(first.CanonicalJson, Does.EndWith("\n"));
            Assert.That(first.CanonicalJson, Does.Not.Contain("\r"));
        });
    }

    [Test]
    public void Future_version_is_rejected_before_import()
    {
        var result = codec.Read(Fixture().Replace("\"formatVersion\": \"1.0.0\"", "\"formatVersion\": \"2.0.0\"", StringComparison.Ordinal));

        Assert.That(Rejected(result).Single().Code, Is.EqualTo("import.version.unsupported"));
    }

    [Test]
    public void Unsafe_extension_schema_uri_is_rejected()
    {
        var root = JsonNode.Parse(Fixture())!.AsObject();
        root["extensions"]!["attacker.payload"] = new JsonObject
        {
            ["version"] = "1",
            ["schema"] = "javascript:alert(1)",
            ["data"] = new JsonObject(),
        };

        var result = codec.Read(root.ToJsonString());

        Assert.That(Rejected(result).Single().Code, Is.EqualTo("import.uri.unsafe"));
    }

    [Test]
    public void Oversized_document_is_rejected_before_parsing()
    {
        var result = codec.Read(new string('x', JsonPortableProjectCodec.MaximumDocumentBytes + 1));

        Assert.That(Rejected(result).Single().Code, Is.EqualTo("import.limit.bytes"));
    }

    [Test]
    public async Task Schema_valid_dogfood_with_unowned_kinds_is_rejected_before_store_access()
    {
        var store = new RecordingStore();
        var handler = new ImportProjectHandler(
            codec, store, new AllowedAuthorizer(),
            new FixedClock(UtcTimestamp.Create(new DateTimeOffset(2026, 8, 16, 5, 0, 0, TimeSpan.Zero))));
        var document = await File.ReadAllTextAsync(Path.Combine(
            TestContext.CurrentContext.TestDirectory, "Fixtures", "project-builder-foundation.project-builder.json"));

        var result = await handler.HandleAsync(
            new("0198ad00-0000-7000-8000-000000000700", "0198ad00-0000-7000-9000-000000000999",
                document, "Inspect compatibility before importing."),
            new ProjectActor("modeler"));

        var invalid = result as ImportProjectResult.Invalid;
        Assert.Multiple(() =>
        {
            Assert.That(invalid, Is.Not.Null);
            Assert.That(invalid!.Findings.Any(finding => finding.Code == "import.compatibility.unsupported"), Is.True);
            Assert.That(store.ImportCalls, Is.Zero);
        });
    }

    private static string Fixture() => File.ReadAllText(Path.Combine(
        TestContext.CurrentContext.TestDirectory, "Fixtures", "example-importable-project.project-builder.json"));

    private static PortableProjectReadResult.Accepted Accepted(PortableProjectReadResult result) =>
        result as PortableProjectReadResult.Accepted ?? throw new AssertionException($"Expected acceptance, received {result}.");

    private static IReadOnlyList<PortableProjectFinding> Rejected(PortableProjectReadResult result) =>
        (result as PortableProjectReadResult.Rejected)?.Findings ?? throw new AssertionException($"Expected rejection, received {result}.");

    private sealed class AllowedAuthorizer : IProjectCreationAuthorizer
    {
        public ValueTask<ProjectCreationAuthorization> AuthorizeAsync(
            ProjectActor actor, WorkspaceId workspaceId, CancellationToken cancellationToken) =>
            ValueTask.FromResult(ProjectCreationAuthorization.Allowed);
    }

    private sealed class FixedClock(UtcTimestamp now) : IApplicationClock
    {
        public UtcTimestamp GetCurrentTimestamp() => now;
    }

    private sealed class RecordingStore : IPortableProjectStore
    {
        public int ImportCalls { get; private set; }

        public ValueTask<PortableImportStoreResult> ImportAsync(
            Guid workspaceId, PortableProjectImport import, CancellationToken cancellationToken)
        {
            ImportCalls++;
            throw new AssertionException("An incompatible document must not reach persistence.");
        }

        public ValueTask<PortableExportStoreResult> ExportAsync(Guid projectId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }
}
