using ProjectBuilder.Application.Projects.CreateProject;

namespace ProjectBuilder.Application.Portability;

public sealed record ImportProjectCommand(
    string WorkspaceId,
    string OperationId,
    string? Document,
    string? Reason);

public sealed record PortableProjectImport(
    PortableProjectDocument Document,
    string CanonicalJson,
    string ContentHash,
    string Reason,
    ProjectActor Actor,
    DateTimeOffset ImportedAt,
    Guid ChangeSetId);

public sealed record StoredPortableProject(
    string ProjectId,
    string Name,
    long Revision,
    int ElementCount,
    int RelationCount,
    string ContentHash);

public interface IPortableProjectStore
{
    ValueTask<PortableImportStoreResult> ImportAsync(
        Guid workspaceId,
        PortableProjectImport import,
        CancellationToken cancellationToken);

    ValueTask<PortableExportStoreResult> ExportAsync(Guid projectId, CancellationToken cancellationToken);
}

public abstract record PortableImportStoreResult
{
    private PortableImportStoreResult() { }
    public sealed record Imported(StoredPortableProject Project) : PortableImportStoreResult;
    public sealed record DuplicateProject : PortableImportStoreResult;
    public sealed record OperationConflict : PortableImportStoreResult;
}

public abstract record PortableExportStoreResult
{
    private PortableExportStoreResult() { }
    public sealed record Exported(string CanonicalJson, string ContentHash) : PortableExportStoreResult;
    public sealed record NotFound : PortableExportStoreResult;
    public sealed record SnapshotStale(long SnapshotRevision, long CurrentRevision) : PortableExportStoreResult;
}

public abstract record ImportProjectResult
{
    private ImportProjectResult() { }
    public sealed record Imported(StoredPortableProject Project) : ImportProjectResult;
    public sealed record Invalid(IReadOnlyList<PortableProjectFinding> Findings) : ImportProjectResult;
    public sealed record Denied(string Reason) : ImportProjectResult;
    public sealed record DuplicateProject : ImportProjectResult;
    public sealed record OperationConflict : ImportProjectResult;
}
