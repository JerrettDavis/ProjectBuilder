using ProjectBuilder.Domain.Modeling.Primitives;

namespace ProjectBuilder.Application.Modeling.DefineNarrative;

public abstract record DefineNarrativeResult
{
    private DefineNarrativeResult() { }
    public sealed record Defined(NarrativeOverview Narrative, long Revision, string AllowedNextAction) : DefineNarrativeResult;
    public sealed record Invalid(IReadOnlyList<SemanticError> Errors) : DefineNarrativeResult;
    public sealed record Denied(string Reason) : DefineNarrativeResult;
    public sealed record ProjectNotFound(string ProjectId) : DefineNarrativeResult;
    public sealed record ReferenceNotFound(string Reference) : DefineNarrativeResult;
    public sealed record Conflict(long Expected, long Actual, IReadOnlyList<ChangeSetConflictOverview> Conflicts) : DefineNarrativeResult;
    public sealed record IdempotencyConflict(string OperationId) : DefineNarrativeResult;
}
