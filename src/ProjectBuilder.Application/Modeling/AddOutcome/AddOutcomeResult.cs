using ProjectBuilder.Domain.Modeling.Primitives;

namespace ProjectBuilder.Application.Modeling.AddOutcome;

public abstract record AddOutcomeResult
{
    private AddOutcomeResult() { }

    public sealed record Added(OutcomeOverview Outcome, long Revision, string AllowedNextAction) : AddOutcomeResult;
    public sealed record Invalid(IReadOnlyList<SemanticError> Errors) : AddOutcomeResult;
    public sealed record Denied(string Reason) : AddOutcomeResult;
    public sealed record ProjectNotFound(string ProjectId) : AddOutcomeResult;
    public sealed record BeneficiaryNotFound(string ActorId) : AddOutcomeResult;
    public sealed record Conflict(long Expected, long Actual, IReadOnlyList<ChangeSetConflictOverview> Conflicts) : AddOutcomeResult;
    public sealed record IdempotencyConflict(string OperationId) : AddOutcomeResult;
}
