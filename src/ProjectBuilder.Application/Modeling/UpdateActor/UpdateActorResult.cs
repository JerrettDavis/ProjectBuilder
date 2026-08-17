using ProjectBuilder.Domain.Modeling.Primitives;

namespace ProjectBuilder.Application.Modeling.UpdateActor;

public abstract record UpdateActorResult
{
    private UpdateActorResult() { }
    public sealed record Updated(ActorOverview Actor, long Revision, string AllowedNextAction) : UpdateActorResult;
    public sealed record Invalid(IReadOnlyList<SemanticError> Errors) : UpdateActorResult;
    public sealed record Denied(string Reason) : UpdateActorResult;
    public sealed record ProjectNotFound(string ProjectId) : UpdateActorResult;
    public sealed record ActorNotFound(string ActorId) : UpdateActorResult;
    public sealed record Conflict(long Expected, long Actual, IReadOnlyList<ChangeSetConflictOverview> Conflicts) : UpdateActorResult;
    public sealed record IdempotencyConflict(string OperationId) : UpdateActorResult;
}
