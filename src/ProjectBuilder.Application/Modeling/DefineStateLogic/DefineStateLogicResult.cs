using ProjectBuilder.Domain.Modeling.Primitives;

namespace ProjectBuilder.Application.Modeling.DefineStateLogic;

public abstract record DefineStateLogicResult
{
    private DefineStateLogicResult() { }
    public sealed record Defined(StateLogicOverview Definitions, long Revision, string AllowedNextAction) : DefineStateLogicResult;
    public sealed record Invalid(IReadOnlyList<SemanticError> Errors) : DefineStateLogicResult;
    public sealed record Denied(string Reason) : DefineStateLogicResult;
    public sealed record ProjectNotFound(string ProjectId) : DefineStateLogicResult;
    public sealed record ReferenceNotFound(string Reference) : DefineStateLogicResult;
    public sealed record Conflict(long Expected, long Actual, IReadOnlyList<ChangeSetConflictOverview> Conflicts) : DefineStateLogicResult;
    public sealed record IdempotencyConflict(string OperationId) : DefineStateLogicResult;
}
