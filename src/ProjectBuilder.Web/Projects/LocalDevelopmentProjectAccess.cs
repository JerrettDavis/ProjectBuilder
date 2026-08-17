using ProjectBuilder.Application.Modeling;
using ProjectBuilder.Application.Projects.CreateProject;
using ProjectBuilder.Domain.Modeling.Primitives;

namespace ProjectBuilder.Web.Projects;

internal sealed class LocalDevelopmentProjectAccess(bool enabled, WorkspaceId workspaceId)
    : IProjectCreationAuthorizer, IProjectEditAuthorizer
{
    internal const string ActorSubject = "local-modeler";

    internal WorkspaceId WorkspaceId { get; } = workspaceId;

    public ValueTask<ProjectCreationAuthorization> AuthorizeAsync(
        ProjectActor actor,
        WorkspaceId requestedWorkspaceId,
        CancellationToken cancellationToken)
    {
        var allowed = enabled &&
            actor.Subject == ActorSubject &&
            requestedWorkspaceId == WorkspaceId;
        return ValueTask.FromResult(allowed
            ? ProjectCreationAuthorization.Allowed
            : ProjectCreationAuthorization.Denied("Project creation is available only in the local development workspace."));
    }

    public ValueTask<ProjectCreationAuthorization> AuthorizeEditAsync(
        ProjectActor actor,
        WorkspaceId requestedWorkspaceId,
        CancellationToken cancellationToken) => AuthorizeAsync(actor, requestedWorkspaceId, cancellationToken);
}
