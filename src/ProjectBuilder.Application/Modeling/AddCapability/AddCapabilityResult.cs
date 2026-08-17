using ProjectBuilder.Domain.Modeling.Primitives;

namespace ProjectBuilder.Application.Modeling.AddCapability;

public abstract record AddCapabilityResult
{
    private AddCapabilityResult() { }
    public sealed record Added(CapabilityOverview Capability, long Revision, string AllowedNextAction) : AddCapabilityResult;
    public sealed record Invalid(IReadOnlyList<SemanticError> Errors) : AddCapabilityResult;
    public sealed record Denied(string Reason) : AddCapabilityResult;
    public sealed record ProjectNotFound(string ProjectId) : AddCapabilityResult;
    public sealed record OutcomeNotFound(string OutcomeId) : AddCapabilityResult;
    public sealed record Conflict(long Expected, long Actual, IReadOnlyList<ChangeSetConflictOverview> Conflicts) : AddCapabilityResult;
    public sealed record IdempotencyConflict(string OperationId) : AddCapabilityResult;
}
