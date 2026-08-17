using ProjectBuilder.Domain.Modeling.Primitives;

namespace ProjectBuilder.Application.Modeling.AddActor;

public abstract record AddActorResult
{
    private AddActorResult()
    {
    }

    public sealed record Added(ActorOverview Actor, long Revision, string AllowedNextAction) : AddActorResult;
    public sealed record Invalid(IReadOnlyList<SemanticError> Errors) : AddActorResult;
    public sealed record Denied(string Reason) : AddActorResult;
    public sealed record ProjectNotFound(string ProjectId) : AddActorResult;
    public sealed record Conflict(long Expected, long Actual, IReadOnlyList<ChangeSetConflictOverview> Conflicts) : AddActorResult;
    public sealed record IdempotencyConflict(string OperationId) : AddActorResult;
}
