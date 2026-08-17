namespace ProjectBuilder.Application.Modeling.UpdateActor;

public sealed record UpdateActorCommand(string ProjectId, string ActorId, string ExpectedRevision, string OperationId,
    string Name, string ActorKind, string ContextualRole, string Goals, string Responsibilities,
    string Authority, string Constraints, string Reason, string KnowledgeStatus);
