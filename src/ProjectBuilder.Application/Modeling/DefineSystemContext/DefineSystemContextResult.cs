using ProjectBuilder.Domain.Modeling.Primitives;

namespace ProjectBuilder.Application.Modeling.DefineSystemContext;

public abstract record DefineSystemContextResult
{
    private DefineSystemContextResult() { }
    public sealed record Defined(SystemContextOverview Context, long Revision, string AllowedNextAction) : DefineSystemContextResult;
    public sealed record Invalid(IReadOnlyList<SemanticError> Errors) : DefineSystemContextResult;
    public sealed record Denied(string Reason) : DefineSystemContextResult;
    public sealed record ProjectNotFound(string ProjectId) : DefineSystemContextResult;
    public sealed record ReferenceNotFound(string Reference) : DefineSystemContextResult;
    public sealed record Conflict(long Expected, long Actual, IReadOnlyList<ChangeSetConflictOverview> Conflicts) : DefineSystemContextResult;
    public sealed record IdempotencyConflict(string OperationId) : DefineSystemContextResult;
}
