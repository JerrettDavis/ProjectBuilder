namespace ProjectBuilder.Application.Projects.CreateProject;

public sealed record CreateProjectCommand(
    string WorkspaceId,
    string OperationId,
    string Name,
    string Purpose,
    string IntendedOutcome,
    string Reason);

public sealed record ProjectActor(string Subject);
