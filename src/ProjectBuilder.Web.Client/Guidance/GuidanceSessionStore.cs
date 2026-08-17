using System.Text.Json;
using Microsoft.JSInterop;

namespace ProjectBuilder.Web.Client.Guidance;

public sealed class GuidanceSessionStore(IJSRuntime javascript)
{
    private const string Prefix = "projectbuilder:guidance:v1:";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async ValueTask<GuidanceSession?> ReadAsync(string projectId)
    {
        var json = await javascript.InvokeAsync<string?>("projectBuilderGuidance.read", Prefix + projectId);
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            return JsonSerializer.Deserialize<GuidanceSession>(json, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public ValueTask WriteAsync(string projectId, GuidanceSession session) =>
        javascript.InvokeVoidAsync("projectBuilderGuidance.write", Prefix + projectId, JsonSerializer.Serialize(session, JsonOptions));
}

public sealed record GuidanceSession(
    string RegistryVersion,
    long ModelRevision,
    string? SelectedPromptId,
    IReadOnlyDictionary<string, string> Answers,
    DateTimeOffset UpdatedAt);
