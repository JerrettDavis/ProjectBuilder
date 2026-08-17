using ProjectBuilder.Domain.Modeling.Primitives;

namespace ProjectBuilder.Application.Projects.CreateProject;

public abstract record CreateProjectResult
{
    private CreateProjectResult()
    {
    }

    public sealed record Created(ProjectOverview Project, string AllowedNextAction) : CreateProjectResult;

    public sealed record Invalid(IReadOnlyList<SemanticError> Errors) : CreateProjectResult;

    public sealed record Denied(string Reason) : CreateProjectResult;

    public sealed record DuplicateName(string Name) : CreateProjectResult;

    public sealed record IdempotencyConflict(string OperationId) : CreateProjectResult;
}

public sealed record ProjectOverview(
    string Id,
    string WorkspaceId,
    string Name,
    string Purpose,
    string IntendedOutcome,
    long Revision,
    string CreationReason,
    string CreatedAt);
