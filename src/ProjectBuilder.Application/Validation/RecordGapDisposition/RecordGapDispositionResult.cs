using ProjectBuilder.Application.Modeling;
using ProjectBuilder.Domain.Modeling.Primitives;

namespace ProjectBuilder.Application.Validation.RecordGapDisposition;

public abstract record RecordGapDispositionResult
{
    private RecordGapDispositionResult() { }
    public sealed record Recorded(GapDispositionOverview Disposition, long Revision) : RecordGapDispositionResult;
    public sealed record Invalid(IReadOnlyList<SemanticError> Errors) : RecordGapDispositionResult;
    public sealed record Denied(string Reason) : RecordGapDispositionResult;
    public sealed record ProjectNotFound(string ProjectId) : RecordGapDispositionResult;
    public sealed record ReferenceNotFound(string Reference) : RecordGapDispositionResult;
    public sealed record FindingNotFound(string RuleCode, string ScopeId, string ProfileId) : RecordGapDispositionResult;
    public sealed record Conflict(long Expected, long Actual, IReadOnlyList<ChangeSetConflictOverview> Conflicts) : RecordGapDispositionResult;
    public sealed record IdempotencyConflict(string OperationId) : RecordGapDispositionResult;
}
