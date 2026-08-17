using System.Text.Json;

namespace ProjectBuilder.Application.Portability;

public sealed record PortableProjectDocument(
    string Format,
    string FormatVersion,
    DateTimeOffset ExportedAt,
    PortableGenerator Generator,
    PortableProject Project,
    IReadOnlyList<PortableElement> Elements,
    IReadOnlyList<PortableRelation> Relations,
    IReadOnlyList<JsonElement> Views,
    IReadOnlyList<JsonElement> Claims,
    IReadOnlyList<JsonElement> Evidence,
    JsonElement Extensions);

public sealed record PortableGenerator(string Name, string Version, string? ProjectionVersion);

public sealed record PortableProject(
    Guid Id,
    Guid? WorkspaceId,
    string Name,
    string Purpose,
    string Status,
    string DefinitionStatus,
    string? PurposeProfile,
    long Revision,
    IReadOnlyList<Guid> IntendedOutcomeIds,
    IReadOnlyList<string> Tags,
    DateTimeOffset CreatedAt,
    DateTimeOffset ModifiedAt,
    string? ContentHash);

public sealed record PortableElement(
    Guid Id,
    Guid ProjectId,
    Guid? ParentId,
    string Kind,
    string Name,
    string? Description,
    string DefinitionStatus,
    string KnowledgeStatus,
    int Order,
    IReadOnlyList<string> Tags,
    IReadOnlyList<JsonElement> Sources,
    DateTimeOffset CreatedAt,
    DateTimeOffset ModifiedAt,
    long Version,
    JsonElement Payload);

public sealed record PortableRelation(
    Guid Id,
    Guid ProjectId,
    string Kind,
    Guid SourceId,
    Guid TargetId,
    string? Name,
    string DefinitionStatus,
    string KnowledgeStatus,
    DateTimeOffset CreatedAt,
    DateTimeOffset ModifiedAt,
    long Version,
    JsonElement Payload);

public sealed record PortableProjectFinding(string Code, string Path, string Message);

public abstract record PortableProjectReadResult
{
    private PortableProjectReadResult() { }

    public sealed record Accepted(
        PortableProjectDocument Document,
        string CanonicalJson,
        string ContentHash) : PortableProjectReadResult;

    public sealed record Rejected(IReadOnlyList<PortableProjectFinding> Findings) : PortableProjectReadResult;
}

public interface IPortableProjectCodec
{
    PortableProjectReadResult Read(string? content);
    PortableProjectReadResult Write(PortableProjectDocument document);
}
