using ProjectBuilder.Domain.Modeling.Primitives;

namespace ProjectBuilder.Application.Views;

public sealed record CanvasViewportOverview(double X, double Y, double Zoom);

public sealed record CanvasNodePlacementOverview(
    string ElementId, double X, double Y, double Width, double Height, bool Collapsed);

public sealed record CanvasLayoutOverview(
    CanvasViewportOverview Viewport,
    string Alignment,
    IReadOnlyList<CanvasNodePlacementOverview> Nodes,
    string InputHash);

public sealed record CanvasViewOverview(
    string Id,
    string ProjectId,
    string Name,
    string Lens,
    string ScopeKey,
    string Visibility,
    string OwnerKey,
    long ModelRevision,
    long LayoutVersion,
    CanvasLayoutOverview Layout,
    string UpdatedAt,
    string UpdatedBy,
    bool IsStale);

public sealed record SaveCanvasViewCommand(
    string ProjectId,
    string Name,
    string Lens,
    string ScopeKey,
    string Visibility,
    long ModelRevision,
    long ExpectedLayoutVersion,
    CanvasLayoutOverview Layout,
    string ActorSubject);

public sealed record ResetCanvasViewCommand(
    string ProjectId,
    string Lens,
    string ScopeKey,
    string Visibility,
    long ExpectedLayoutVersion,
    string ActorSubject);

public abstract record CanvasViewResult
{
    private CanvasViewResult() { }
    public sealed record Found(CanvasViewOverview View, long SemanticRevision) : CanvasViewResult;
    public sealed record Saved(CanvasViewOverview View, long SemanticRevision) : CanvasViewResult;
    public sealed record Reset(long SemanticRevision) : CanvasViewResult;
    public sealed record Missing(long SemanticRevision) : CanvasViewResult;
    public sealed record Invalid(IReadOnlyDictionary<string, string[]> Errors) : CanvasViewResult;
    public sealed record Conflict(long ExpectedLayoutVersion, long ActualLayoutVersion) : CanvasViewResult;
    public sealed record ProjectNotFound : CanvasViewResult;
}

public interface ICanvasViewStore
{
    ValueTask<CanvasViewOverview?> FindAsync(
        ProjectId projectId, string lens, string scopeKey, string visibility, string ownerKey,
        long currentModelRevision, CancellationToken cancellationToken);

    ValueTask<CanvasViewStoreResult> SaveAsync(
        ProjectId projectId, string name, string lens, string scopeKey, string visibility, string ownerKey,
        long modelRevision, long expectedLayoutVersion, CanvasLayoutOverview layout,
        UtcTimestamp updatedAt, string updatedBy, CancellationToken cancellationToken);

    ValueTask<CanvasViewStoreResult> ResetAsync(
        ProjectId projectId, string lens, string scopeKey, string visibility, string ownerKey,
        long expectedLayoutVersion, CancellationToken cancellationToken);
}

public abstract record CanvasViewStoreResult
{
    private CanvasViewStoreResult() { }
    public sealed record Saved(CanvasViewOverview View) : CanvasViewStoreResult;
    public sealed record Reset : CanvasViewStoreResult;
    public sealed record Missing : CanvasViewStoreResult;
    public sealed record Conflict(long ActualLayoutVersion) : CanvasViewStoreResult;
}
