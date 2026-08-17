namespace ProjectBuilder.Application.Modeling.DefineNarrative;

public sealed record DefineNarrativeCommand(
    string ProjectId, string ExpectedRevision, string OperationId, string OutcomeId,
    string ParticipantIds, string InitiatorId, string ReceiverId,
    string EpisodeName, string EpisodeStart, string EpisodeEnd,
    string ScenarioName, string Classification, string StartingFacts, string Trigger, string ExpectedOutcome,
    string SceneName, string Setting, string Responsibility,
    string InteractionName, string Intent, string Step, string Observation,
    string SemanticResults, string Reason);
