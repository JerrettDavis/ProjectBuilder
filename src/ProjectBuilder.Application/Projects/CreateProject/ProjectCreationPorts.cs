using ProjectBuilder.Domain.Modeling.Primitives;
using ProjectBuilder.Domain.Projects;

namespace ProjectBuilder.Application.Projects.CreateProject;

public interface IProjectCreationStore
{
    ValueTask<StoredProjectCreation?> FindByOperationAsync(
        ChangeSetId operationId,
        CancellationToken cancellationToken);

    ValueTask<bool> NameExistsAsync(
        WorkspaceId workspaceId,
        ElementName name,
        CancellationToken cancellationToken);

    ValueTask<bool> TrySaveAsync(
        ProjectDefinition project,
        string requestFingerprint,
        CancellationToken cancellationToken);

    ValueTask<ProjectDefinition?> FindByIdAsync(ProjectId projectId, CancellationToken cancellationToken);
}

public sealed record StoredProjectCreation(ProjectDefinition Project, string RequestFingerprint);

public interface IProjectCreationAuthorizer
{
    ValueTask<ProjectCreationAuthorization> AuthorizeAsync(
        ProjectActor actor,
        WorkspaceId workspaceId,
        CancellationToken cancellationToken);
}

public sealed record ProjectCreationAuthorization(bool IsAllowed, string Reason)
{
    public static ProjectCreationAuthorization Allowed { get; } = new(true, string.Empty);

    public static ProjectCreationAuthorization Denied(string reason) => new(false, reason);
}

public interface IProjectIdentitySource
{
    ProjectId NextProjectId();
}

public interface IApplicationClock
{
    UtcTimestamp GetCurrentTimestamp();
}
