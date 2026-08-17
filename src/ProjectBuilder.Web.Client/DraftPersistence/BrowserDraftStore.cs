using System.Text.Json;
using Microsoft.JSInterop;

namespace ProjectBuilder.Web.Client.DraftPersistence;

public sealed class BrowserDraftStore(IJSRuntime javascript)
{
    private const string Prefix = "projectbuilder:draft:v1:";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async ValueTask<BrowserDraft<T>?> ReadAsync<T>(string key)
    {
        var json = await javascript.InvokeAsync<string?>("projectBuilderDrafts.read", Prefix + key);
        return string.IsNullOrWhiteSpace(json) ? null : JsonSerializer.Deserialize<BrowserDraft<T>>(json, JsonOptions);
    }

    public ValueTask WriteAsync<T>(string key, BrowserDraft<T> draft) =>
        javascript.InvokeVoidAsync("projectBuilderDrafts.write", Prefix + key, JsonSerializer.Serialize(draft, JsonOptions));

    public ValueTask RemoveAsync(string key) =>
        javascript.InvokeVoidAsync("projectBuilderDrafts.remove", Prefix + key);
}

public sealed record BrowserDraft<T>(long BaseRevision, string OperationId, DateTimeOffset UpdatedAt, T Value);
