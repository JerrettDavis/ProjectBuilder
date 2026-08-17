namespace ProjectBuilder.Application.Modeling.UpdateOutcome;

public sealed record UpdateOutcomeCommand(string ProjectId, string OutcomeId, string ExpectedRevision, string OperationId,
    string Name, string Statement, string SuccessSignals, string BeneficiaryActorId, string Reason, string KnowledgeStatus);
