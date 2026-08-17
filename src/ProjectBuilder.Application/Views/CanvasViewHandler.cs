using ProjectBuilder.Application.Projects.CreateProject;
using ProjectBuilder.Domain.Modeling.Primitives;
using ProjectBuilder.Domain.Projects;

namespace ProjectBuilder.Application.Views;

public sealed class CanvasViewHandler(
    IProjectCreationStore projects,
    ICanvasViewStore views,
    IApplicationClock clock)
{
    private static readonly string[] AllowedVisibilities = ["personal", "team"];
    private static readonly string[] AllowedAlignments = ["horizontal", "vertical"];

    public async ValueTask<CanvasViewResult> GetAsync(
        string projectId, string lens, string scopeKey, string visibility, string actorSubject,
        CancellationToken cancellationToken = default)
    {
        var context = await ContextAsync(projectId, lens, scopeKey, visibility, actorSubject, cancellationToken);
        if (context.Result is not null) return context.Result;
        var view = await views.FindAsync(context.Project!.Id, lens, scopeKey, visibility, context.OwnerKey!,
            context.Project.Revision.Value, cancellationToken);
        return view is null
            ? new CanvasViewResult.Missing(context.Project.Revision.Value)
            : new CanvasViewResult.Found(view, context.Project.Revision.Value);
    }

    public async ValueTask<CanvasViewResult> SaveAsync(
        SaveCanvasViewCommand command,
        CancellationToken cancellationToken = default)
    {
        var context = await ContextAsync(command.ProjectId, command.Lens, command.ScopeKey,
            command.Visibility, command.ActorSubject, cancellationToken);
        if (context.Result is not null) return context.Result;
        var errors = Validate(command, context.Project!.Revision.Value);
        if (errors.Count > 0) return new CanvasViewResult.Invalid(errors);
        var stored = await views.SaveAsync(context.Project.Id, command.Name.Trim(), command.Lens,
            command.ScopeKey, command.Visibility, context.OwnerKey!, command.ModelRevision,
            command.ExpectedLayoutVersion, command.Layout, clock.GetCurrentTimestamp(),
            command.ActorSubject, cancellationToken);
        return stored switch
        {
            CanvasViewStoreResult.Saved saved => new CanvasViewResult.Saved(saved.View, context.Project.Revision.Value),
            CanvasViewStoreResult.Conflict conflict => new CanvasViewResult.Conflict(
                command.ExpectedLayoutVersion, conflict.ActualLayoutVersion),
            _ => throw new InvalidOperationException("The canvas view store returned an invalid save result."),
        };
    }

    public async ValueTask<CanvasViewResult> ResetAsync(
        ResetCanvasViewCommand command,
        CancellationToken cancellationToken = default)
    {
        var context = await ContextAsync(command.ProjectId, command.Lens, command.ScopeKey,
            command.Visibility, command.ActorSubject, cancellationToken);
        if (context.Result is not null) return context.Result;
        if (command.ExpectedLayoutVersion < 1)
            return Invalid("expectedLayoutVersion", "A saved layout version is required before reset.");
        var stored = await views.ResetAsync(context.Project!.Id, command.Lens, command.ScopeKey,
            command.Visibility, context.OwnerKey!, command.ExpectedLayoutVersion, cancellationToken);
        return stored switch
        {
            CanvasViewStoreResult.Reset => new CanvasViewResult.Reset(context.Project.Revision.Value),
            CanvasViewStoreResult.Missing => new CanvasViewResult.Missing(context.Project.Revision.Value),
            CanvasViewStoreResult.Conflict conflict => new CanvasViewResult.Conflict(
                command.ExpectedLayoutVersion, conflict.ActualLayoutVersion),
            _ => throw new InvalidOperationException("The canvas view store returned an invalid reset result."),
        };
    }

    private async ValueTask<(ProjectDefinition? Project, string? OwnerKey, CanvasViewResult? Result)> ContextAsync(
        string projectId, string lens, string scopeKey, string visibility, string actorSubject,
        CancellationToken cancellationToken)
    {
        var parsed = ProjectId.Parse(projectId);
        if (parsed is SemanticResult<ProjectId>.Rejected rejected)
            return (null, null, Invalid("projectId", rejected.Error.Message));
        var errors = new Dictionary<string, string[]>();
        if (lens != "custom") errors["lens"] = ["The initial canvas view contract supports the custom project-definition lens only."];
        if (scopeKey != "project-definition") errors["scopeKey"] = ["The initial canvas view scope must be project-definition."];
        if (!AllowedVisibilities.Contains(visibility, StringComparer.Ordinal))
            errors["visibility"] = ["Visibility must be personal or team."];
        if (string.IsNullOrWhiteSpace(actorSubject)) errors["actorSubject"] = ["A view owner is required."];
        if (errors.Count > 0) return (null, null, new CanvasViewResult.Invalid(errors));
        var id = ((SemanticResult<ProjectId>.Accepted)parsed).Value;
        var project = await projects.FindByIdAsync(id, cancellationToken);
        if (project is null) return (null, null, new CanvasViewResult.ProjectNotFound());
        return (project, visibility == "team" ? "__team__" : actorSubject.Trim(), null);
    }

    private static Dictionary<string, string[]> Validate(SaveCanvasViewCommand command, long currentRevision)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(command.Name) || command.Name.Trim().Length > 500)
            errors["name"] = ["A view name between 1 and 500 characters is required."];
        if (command.ModelRevision != currentRevision)
            errors["modelRevision"] = [$"The view baseline is revision {command.ModelRevision}; the current semantic revision is {currentRevision}."];
        if (command.ExpectedLayoutVersion < 0)
            errors["expectedLayoutVersion"] = ["Expected layout version cannot be negative."];
        if (!AllowedAlignments.Contains(command.Layout.Alignment, StringComparer.Ordinal))
            errors["layout.alignment"] = ["Alignment must be horizontal or vertical."];
        if (!Finite(command.Layout.Viewport.X) || !Finite(command.Layout.Viewport.Y) ||
            !Finite(command.Layout.Viewport.Zoom) || command.Layout.Viewport.Zoom is < .5 or > 2)
            errors["layout.viewport"] = ["Viewport coordinates must be finite and zoom must be between 0.5 and 2."];
        if (command.Layout.Nodes.Count is < 1 or > 1_000)
            errors["layout.nodes"] = ["A canvas view must contain between 1 and 1000 placements."];
        else if (command.Layout.Nodes.Select(node => node.ElementId).Distinct(StringComparer.Ordinal).Count() != command.Layout.Nodes.Count)
            errors["layout.nodes"] = ["Canvas node placements must have unique element identifiers."];
        else if (command.Layout.Nodes.Any(node => string.IsNullOrWhiteSpace(node.ElementId) ||
                     !Finite(node.X) || !Finite(node.Y) || !Finite(node.Width) || !Finite(node.Height) ||
                     node.Width <= 0 || node.Height <= 0))
            errors["layout.nodes"] = ["Every placement requires an identifier and finite positive geometry."];
        if (string.IsNullOrWhiteSpace(command.Layout.InputHash) || command.Layout.InputHash.Length > 128)
            errors["layout.inputHash"] = ["A deterministic layout input hash is required."];
        return errors;
    }

    private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    private static CanvasViewResult.Invalid Invalid(string key, string message) => new(
        new Dictionary<string, string[]> { [key] = [message] });
}
