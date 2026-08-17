namespace ProjectBuilder.Application.Validation.RecordGapDisposition;

public sealed record RecordGapDispositionCommand(
    string ProjectId, string ExpectedRevision, string OperationId,
    string ProfileId, string RuleCode, string ScopeId, string Disposition,
    string Rationale, string Consequence, string AuthorityActorId,
    string? ReviewOn, string? TargetMilestone, string Reason);
