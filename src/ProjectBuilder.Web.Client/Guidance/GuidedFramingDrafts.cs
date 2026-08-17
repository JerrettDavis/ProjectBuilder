namespace ProjectBuilder.Web.Client.Guidance;

public sealed class GuidedActorDraft
{
    public string Name { get; set; } = string.Empty;
    public string ActorKind { get; set; } = "humanRole";
    public string ContextualRole { get; set; } = string.Empty;
    public string Goals { get; set; } = string.Empty;
    public string Responsibilities { get; set; } = string.Empty;
    public string Authority { get; set; } = string.Empty;
    public string Constraints { get; set; } = string.Empty;
    public string Reason { get; set; } = "Frame an accountable project participant through the Guide Rail.";
    public string KnowledgeStatus { get; set; } = "known";
}

public sealed class GuidedOutcomeDraft
{
    public string Name { get; set; } = string.Empty;
    public string Statement { get; set; } = string.Empty;
    public string SuccessSignals { get; set; } = string.Empty;
    public string BeneficiaryActorId { get; set; } = string.Empty;
    public string Reason { get; set; } = "Frame a beneficiary-linked observable outcome through the Guide Rail.";
    public string KnowledgeStatus { get; set; } = "known";
}

public sealed class GuidedScenarioDraft
{
    public string OutcomeId { get; set; } = string.Empty;
    public string InitiatorId { get; set; } = string.Empty;
    public string ReceiverId { get; set; } = string.Empty;
    public string EpisodeName { get; set; } = string.Empty;
    public string EpisodeStart { get; set; } = string.Empty;
    public string EpisodeEnd { get; set; } = string.Empty;
    public string ScenarioName { get; set; } = string.Empty;
    public string Classification { get; set; } = "Happy";
    public string StartingFacts { get; set; } = string.Empty;
    public string Trigger { get; set; } = string.Empty;
    public string ExpectedOutcome { get; set; } = string.Empty;
    public string SceneName { get; set; } = string.Empty;
    public string Setting { get; set; } = string.Empty;
    public string Responsibility { get; set; } = string.Empty;
    public string InteractionName { get; set; } = string.Empty;
    public string Intent { get; set; } = string.Empty;
    public string Step { get; set; } = string.Empty;
    public string Observation { get; set; } = string.Empty;
    public string SemanticResults { get; set; } = string.Empty;
    public string Reason { get; set; } = "Describe one ordinary end-to-end scenario through the Guide Rail.";
}

public sealed record GuidedCommitResult(long Revision, string Title, string Summary);
