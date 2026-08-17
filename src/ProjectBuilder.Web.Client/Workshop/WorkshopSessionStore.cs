using System.Text.Json;
using Microsoft.JSInterop;

namespace ProjectBuilder.Web.Client.Workshop;

public sealed class WorkshopSessionStore(IJSRuntime javascript)
{
    private const string Prefix = "projectbuilder:workshop:v1:";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async ValueTask<WorkshopSession?> ReadAsync(string projectId)
    {
        var json = await javascript.InvokeAsync<string?>("projectBuilderGuidance.read", Prefix + projectId);
        if (string.IsNullOrWhiteSpace(json)) return null;
        try { return JsonSerializer.Deserialize<WorkshopSession>(json, JsonOptions); }
        catch (JsonException) { return null; }
    }

    public ValueTask WriteAsync(string projectId, WorkshopSession session) =>
        javascript.InvokeVoidAsync("projectBuilderGuidance.write", Prefix + projectId, JsonSerializer.Serialize(session, JsonOptions));
}

public sealed record WorkshopCapture(string Id, string Kind, string Text, string Owner, string Status);
public sealed record WorkshopSession(
    string BriefVersion, long ModelRevision, int ActiveAgendaIndex, bool IsRunning,
    IReadOnlyList<string> DiscussedAgendaIds, IReadOnlyList<WorkshopCapture> Captures,
    IReadOnlyList<string> ParkingLot, DateTimeOffset UpdatedAt);
