namespace ProjectBuilder.Application.Modeling.AddOutcome;

public sealed record AddOutcomeCommand(
    string ProjectId,
    string ExpectedRevision,
    string OperationId,
    string Name,
    string Statement,
    string SuccessSignals,
    string BeneficiaryActorId,
    string Reason,
    string KnowledgeStatus = "known");
