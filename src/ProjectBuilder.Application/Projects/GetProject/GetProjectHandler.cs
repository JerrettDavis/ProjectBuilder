using ProjectBuilder.Application.Projects.CreateProject;
using ProjectBuilder.Domain.Modeling.Primitives;

namespace ProjectBuilder.Application.Projects.GetProject;

public sealed class GetProjectHandler(IProjectCreationStore store)
{
    public async ValueTask<GetProjectResult> HandleAsync(
        string projectId,
        CancellationToken cancellationToken = default)
    {
        var parsed = ProjectId.Parse(projectId);
        if (parsed is SemanticResult<ProjectId>.Rejected rejected)
        {
            return new GetProjectResult.Invalid(rejected.Error);
        }

        var id = ((SemanticResult<ProjectId>.Accepted)parsed).Value;
        var project = await store.FindByIdAsync(id, cancellationToken);
        return project is null
            ? new GetProjectResult.NotFound(id.ToString())
            : new GetProjectResult.Found(CreateProjectHandler.ToOverview(project));
    }
}

public abstract record GetProjectResult
{
    private GetProjectResult()
    {
    }

    public sealed record Found(ProjectOverview Project) : GetProjectResult;

    public sealed record Invalid(SemanticError Error) : GetProjectResult;

    public sealed record NotFound(string ProjectId) : GetProjectResult;
}
