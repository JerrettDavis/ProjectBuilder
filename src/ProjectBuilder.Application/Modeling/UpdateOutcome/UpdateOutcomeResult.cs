using ProjectBuilder.Domain.Modeling.Primitives;

namespace ProjectBuilder.Application.Modeling.UpdateOutcome;

public abstract record UpdateOutcomeResult
{
    private UpdateOutcomeResult() { }
    public sealed record Updated(OutcomeOverview Outcome, long Revision, string AllowedNextAction) : UpdateOutcomeResult;
    public sealed record Invalid(IReadOnlyList<SemanticError> Errors) : UpdateOutcomeResult;
    public sealed record Denied(string Reason) : UpdateOutcomeResult;
    public sealed record ProjectNotFound(string ProjectId) : UpdateOutcomeResult;
    public sealed record OutcomeNotFound(string OutcomeId) : UpdateOutcomeResult;
    public sealed record BeneficiaryNotFound(string ActorId) : UpdateOutcomeResult;
    public sealed record Conflict(long Expected, long Actual, IReadOnlyList<ChangeSetConflictOverview> Conflicts) : UpdateOutcomeResult;
    public sealed record IdempotencyConflict(string OperationId) : UpdateOutcomeResult;
}
