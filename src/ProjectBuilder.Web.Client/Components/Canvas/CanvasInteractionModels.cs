namespace ProjectBuilder.Web.Client.Components.Canvas;

public sealed record CanvasNodeView(
    string Id,
    string Kind,
    string Title,
    string Subtitle,
    string Status,
    string Frame,
    int Order,
    bool HasInput,
    bool HasOutput);

public sealed record CanvasEdgeView(
    string Id,
    string SourceId,
    string TargetId,
    string Kind,
    string Label);

internal sealed record CanvasNodeGeometry(CanvasNodeView Node, double X, double Y, double Width, double Height);

internal sealed record CanvasFrameGeometry(string Name, double X, double Y, double Width, double Height, int Count);

public sealed record CanvasNodePlacementView(
    string ElementId, double X, double Y, double Width, double Height, bool Collapsed);

public sealed record CanvasViewState(
    double PanX,
    double PanY,
    double Zoom,
    string Alignment,
    IReadOnlyList<CanvasNodePlacementView> Nodes);
