using ProjectBuilder.Domain.Modeling.Primitives;

namespace ProjectBuilder.Application.Modeling.DefinePath;

public abstract record DefinePathResult
{
    private DefinePathResult() { }
    public sealed record Defined(PathOverview Path, long Revision, string AllowedNextAction) : DefinePathResult;
    public sealed record Invalid(IReadOnlyList<SemanticError> Errors) : DefinePathResult;
    public sealed record Denied(string Reason) : DefinePathResult;
    public sealed record ProjectNotFound(string ProjectId) : DefinePathResult;
    public sealed record ReferenceNotFound(string Reference) : DefinePathResult;
    public sealed record Conflict(long Expected, long Actual, IReadOnlyList<ChangeSetConflictOverview> Conflicts) : DefinePathResult;
    public sealed record IdempotencyConflict(string OperationId) : DefinePathResult;
}
