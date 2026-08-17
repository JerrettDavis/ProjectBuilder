using ProjectBuilder.Application.Projects.CreateProject;
using ProjectBuilder.Domain.Modeling.Primitives;

namespace ProjectBuilder.Application.Modeling.GetProjectModel;

public sealed class GetProjectModelHandler(IProjectCreationStore projects, IProjectElementStore elements)
{
    public async ValueTask<GetProjectModelResult> HandleAsync(
        string projectId,
        CancellationToken cancellationToken = default)
    {
        var parsed = ProjectId.Parse(projectId);
        if (parsed is SemanticResult<ProjectId>.Rejected rejected)
        {
            return new GetProjectModelResult.Invalid(rejected.Error);
        }

        var id = ((SemanticResult<ProjectId>.Accepted)parsed).Value;
        var project = await projects.FindByIdAsync(id, cancellationToken);
        if (project is null)
        {
            return new GetProjectModelResult.NotFound(projectId);
        }

        var model = await elements.LoadModelAsync(id, cancellationToken);
        var changeSets = await elements.LoadChangeHistoryAsync(id, cancellationToken);
        return new GetProjectModelResult.Found(new ProjectModelOverview(
            CreateProjectHandler.ToOverview(project),
            model.Actors.Select(ModelApplicationMapping.Actor).ToArray(),
            model.Outcomes.Select(ModelApplicationMapping.Outcome).ToArray(),
            model.Narratives,
            model.StateLogic,
            model.Paths,
            model.Relations.Select(ModelApplicationMapping.Relation).ToArray(),
            changeSets,
            model.GapDispositions,
            model.Capabilities.Select(ModelApplicationMapping.Capability).ToArray(),
            model.SystemContexts.IsDefault ? [] : model.SystemContexts));
    }
}

public abstract record GetProjectModelResult
{
    private GetProjectModelResult() { }

    public sealed record Found(ProjectModelOverview Model) : GetProjectModelResult;
    public sealed record Invalid(SemanticError Error) : GetProjectModelResult;
    public sealed record NotFound(string ProjectId) : GetProjectModelResult;
}
