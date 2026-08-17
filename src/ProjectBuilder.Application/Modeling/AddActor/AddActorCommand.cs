namespace ProjectBuilder.Application.Modeling.AddActor;

public sealed record AddActorCommand(
    string ProjectId,
    string ExpectedRevision,
    string OperationId,
    string Name,
    string ActorKind,
    string ContextualRole,
    string Goals,
    string Responsibilities,
    string Authority,
    string Constraints,
    string Reason,
    string KnowledgeStatus = "known");
