using System.Collections.Immutable;

namespace ProjectBuilder.Application.Guidance;

public enum GuidanceStage
{
    Frame,
    Participants,
    Behavior,
    State,
    Recovery,
    Evidence,
}

public enum GuidanceFact
{
    HasActors,
    HasOutcomes,
    HasNarratives,
    HasStateLogic,
    HasPaths,
    HasEvidenceArtifacts,
}

public enum GuidanceAnswerKind
{
    Author,
    Unknown,
    Assumed,
    Deferred,
    NotApplicable,
}

public sealed record PromptApplicability(GuidanceFact Fact, bool Expected);

public sealed record PromptAnswerMapping(
    string Key, string Label, GuidanceAnswerKind Kind, string ResultingChange,
    bool RequiresRationale, string? RepairCommand);

public sealed record PromptDescriptor(
    string Id, int Version, GuidanceStage Stage, int Order, string Question,
    string WhyThisMatters, string LearningContent, string TriggerExplanation,
    ImmutableArray<PromptApplicability> AppliesWhen,
    ImmutableArray<string> RelatedFactKinds,
    ImmutableArray<string> Examples,
    ImmutableArray<PromptAnswerMapping> AnswerMappings,
    string PrimaryRepairCommand);

public sealed record PromptRegistryFinding(string Code, string PromptId, string Message);
