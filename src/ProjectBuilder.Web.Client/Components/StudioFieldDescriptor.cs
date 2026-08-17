namespace ProjectBuilder.Web.Client.Components;

public sealed record StudioFieldDescriptor(
    string Id,
    string Label,
    string Hint,
    bool Required = false,
    string? SourceHref = null,
    string? SourceLabel = null);
