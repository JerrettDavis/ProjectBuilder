using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Json.Schema;
using ProjectBuilder.Application.Portability;

namespace ProjectBuilder.Infrastructure.Portability;

internal sealed class JsonPortableProjectCodec : IPortableProjectCodec
{
    internal const int MaximumDocumentBytes = 1_048_576;
    internal const int MaximumDepth = 64;
    internal const string CurrentFormatVersion = "1.0.0";

    private static readonly UTF8Encoding StrictUtf8 = new(false, true);
    private static readonly JsonSchema Schema = LoadSchema();
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = false,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public PortableProjectReadResult Read(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return Rejected("import.document.required", "$", "A Project Builder JSON document is required.");

        byte[] bytes;
        try
        {
            bytes = StrictUtf8.GetBytes(content);
        }
        catch (EncoderFallbackException)
        {
            return Rejected("import.encoding.invalid", "$", "The document must be valid UTF-8 text.");
        }

        if (bytes.Length > MaximumDocumentBytes)
            return Rejected("import.limit.bytes", "$", $"The document cannot exceed {MaximumDocumentBytes} UTF-8 bytes.");

        JsonDocument parsed;
        try
        {
            parsed = JsonDocument.Parse(bytes, new JsonDocumentOptions
            {
                AllowTrailingCommas = false,
                CommentHandling = JsonCommentHandling.Disallow,
                MaxDepth = MaximumDepth,
            });
        }
        catch (JsonException exception)
        {
            return Rejected("import.json.invalid", "$", $"The document is not valid bounded JSON: {exception.Message}");
        }

        using (parsed)
        {
            if (!parsed.RootElement.TryGetProperty("formatVersion", out var versionNode) ||
                versionNode.ValueKind != JsonValueKind.String)
                return Rejected("import.version.required", "$.formatVersion", "A semantic formatVersion is required.");

            var version = versionNode.GetString();
            if (!string.Equals(version, CurrentFormatVersion, StringComparison.Ordinal))
                return Rejected("import.version.unsupported", "$.formatVersion",
                    $"Format version '{version}' is not supported; this runtime accepts exactly {CurrentFormatVersion}.");

            var schemaResult = Schema.Evaluate(parsed.RootElement, new EvaluationOptions
            {
                OutputFormat = OutputFormat.List,
                RequireFormatValidation = true,
            });
            if (!schemaResult.IsValid)
                return Rejected("import.schema.invalid", "$", "The document does not satisfy the Project Builder 1.0 schema.");

            var unsafeFinding = FindUnsafeUri(parsed.RootElement);
            if (unsafeFinding is not null)
                return new PortableProjectReadResult.Rejected([unsafeFinding]);

            PortableProjectDocument? document;
            try
            {
                document = JsonSerializer.Deserialize<PortableProjectDocument>(parsed.RootElement, SerializerOptions);
            }
            catch (JsonException exception)
            {
                return Rejected("import.contract.invalid", "$", $"The schema-valid document could not be mapped: {exception.Message}");
            }

            if (document is null)
                return Rejected("import.contract.invalid", "$", "The document could not be mapped.");

            var duplicate = FindDuplicateIdentifier(document);
            if (duplicate is not null)
                return new PortableProjectReadResult.Rejected([duplicate]);

            var normalized = document with
            {
                Project = document.Project with
                {
                    IntendedOutcomeIds = document.Project.IntendedOutcomeIds.Order().ToArray(),
                    Tags = document.Project.Tags.Order(StringComparer.Ordinal).ToArray(),
                },
                Elements = document.Elements
                    .Select(element => element with { Tags = element.Tags.Order(StringComparer.Ordinal).ToArray() })
                    .OrderBy(element => element.Order)
                    .ThenBy(element => element.Id)
                    .ToArray(),
                Relations = document.Relations.OrderBy(relation => relation.Id).ToArray(),
                Views = OrderById(document.Views),
                Claims = OrderById(document.Claims),
                Evidence = OrderById(document.Evidence),
            };
            var canonical = JsonSerializer.Serialize(normalized, SerializerOptions).Replace("\r\n", "\n", StringComparison.Ordinal) + "\n";
            var hash = "sha256:" + Convert.ToHexString(SHA256.HashData(StrictUtf8.GetBytes(canonical))).ToLowerInvariant();
            return new PortableProjectReadResult.Accepted(normalized, canonical, hash);
        }
    }

    public PortableProjectReadResult Write(PortableProjectDocument document) =>
        Read(JsonSerializer.Serialize(document, SerializerOptions));

    private static JsonElement[] OrderById(IReadOnlyList<JsonElement> values) =>
        values.OrderBy(value => value.TryGetProperty("id", out var id) ? id.GetString() : string.Empty, StringComparer.Ordinal).ToArray();

    private static PortableProjectFinding? FindDuplicateIdentifier(PortableProjectDocument document)
    {
        var ids = new HashSet<Guid> { document.Project.Id };
        foreach (var (id, path) in document.Elements.Select((value, index) => (value.Id, $"$.elements[{index}].id"))
                     .Concat(document.Relations.Select((value, index) => (value.Id, $"$.relations[{index}].id")))
                     .Concat(JsonIds(document.Views, "views"))
                     .Concat(JsonIds(document.Claims, "claims"))
                     .Concat(JsonIds(document.Evidence, "evidence")))
        {
            if (!ids.Add(id)) return new("import.identifier.duplicate", path, $"Identifier '{id}' occurs more than once.");
        }
        return null;
    }

    private static IEnumerable<(Guid Id, string Path)> JsonIds(IReadOnlyList<JsonElement> values, string collection) =>
        values.Select((value, index) => (
            Guid.Parse(value.GetProperty("id").GetString()!),
            $"$.{collection}[{index}].id"));

    private static PortableProjectFinding? FindUnsafeUri(JsonElement root)
    {
        if (root.TryGetProperty("extensions", out var extensions))
        {
            foreach (var extension in extensions.EnumerateObject())
            {
                if (extension.Value.TryGetProperty("schema", out var schema) && IsUnsafeUri(schema.GetString()))
                    return new("import.uri.unsafe", $"$.extensions.{extension.Name}.schema", "Only HTTPS extension schema URIs are permitted.");
            }
        }

        if (root.TryGetProperty("evidence", out var evidence))
        {
            var index = 0;
            foreach (var item in evidence.EnumerateArray())
            {
                if (item.TryGetProperty("artifact", out var artifact) && artifact.TryGetProperty("uri", out var uri) && IsUnsafeUri(uri.GetString()))
                    return new("import.uri.unsafe", $"$.evidence[{index}].artifact.uri", "Only HTTPS artifact URIs are permitted.");
                index++;
            }
        }
        return null;
    }

    private static bool IsUnsafeUri(string? value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri) && !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);

    private static JsonSchema LoadSchema()
    {
        var assembly = typeof(JsonPortableProjectCodec).Assembly;
        var resource = assembly.GetManifestResourceNames().Single(name => name.EndsWith("project-builder-model.schema.json", StringComparison.Ordinal));
        using var stream = assembly.GetManifestResourceStream(resource)!;
        using var reader = new StreamReader(stream, StrictUtf8, false);
        return JsonSchema.FromText(reader.ReadToEnd());
    }

    private static PortableProjectReadResult.Rejected Rejected(string code, string path, string message) =>
        new([new(code, path, message)]);
}
