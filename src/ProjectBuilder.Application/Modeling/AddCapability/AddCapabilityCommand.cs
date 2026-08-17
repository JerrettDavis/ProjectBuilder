namespace ProjectBuilder.Application.Modeling.AddCapability;

public sealed record AddCapabilityCommand(
    string ProjectId, string ExpectedRevision, string OperationId, string Name, string Ability,
    IReadOnlyList<string> OutcomeIds, string Priority, string Reason, string KnowledgeStatus = "known");
