using System.Collections.Immutable;
using ProjectBuilder.Domain.Modeling.Primitives;

namespace ProjectBuilder.Domain.Modeling.Elements;

public enum ScenarioClassification
{
    Happy,
    Alternate,
    Exceptional,
    Degraded,
    Recovery,
    Cancellation,
    Compensation,
}

public sealed record NarrativeText
{
    public const int MaxLength = Description.MaxLength;
    private NarrativeText(string value) => Value = value;
    public string Value { get; }

    public static SemanticResult<NarrativeText> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return SemanticResult.Reject<NarrativeText>("narrative.text.required", "Narrative text is required.");
        var normalized = value.Trim();
        return normalized.EnumerateRunes().Count() > MaxLength
            ? SemanticResult.Reject<NarrativeText>("narrative.text.too_long", $"Narrative text cannot exceed {MaxLength} Unicode code points.")
            : SemanticResult.Accept(new NarrativeText(normalized));
    }
}

public sealed record NarrativeFact
{
    public const int MaxLength = 500;
    private NarrativeFact(string value) => Value = value;
    public string Value { get; }

    public static SemanticResult<NarrativeFact> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return SemanticResult.Reject<NarrativeFact>("narrative.fact.required", "A narrative fact cannot be blank.");
        var normalized = value.Trim();
        return normalized.EnumerateRunes().Count() > MaxLength
            ? SemanticResult.Reject<NarrativeFact>("narrative.fact.too_long", $"A narrative fact cannot exceed {MaxLength} Unicode code points.")
            : SemanticResult.Accept(new NarrativeFact(normalized));
    }
}

public abstract record NarrativeElement : ModelElement
{
    protected NarrativeElement(
        ElementId id, ProjectId projectId, ElementId? parentId, ElementName name,
        NarrativeText description, int order, UtcTimestamp createdAt, string createdBy)
        : base(id, projectId, parentId, name, AcceptedDescription(description), DefinitionStatus.Defined,
            KnowledgeStatus.Known, order, createdAt, createdBy, 1)
    { }

    private static Description AcceptedDescription(NarrativeText text) =>
        ((SemanticResult<Description>.Accepted)Description.Create(text.Value)).Value;
}

public sealed record EpisodeDefinition : NarrativeElement
{
    public EpisodeDefinition(ElementId id, ProjectId projectId, ElementName name, NarrativeText start,
        NarrativeText end, ElementId outcomeId, ImmutableArray<ElementId> participantIds, int order,
        UtcTimestamp createdAt, string createdBy)
        : base(id, projectId, null, name, end, order, createdAt, createdBy)
    { Start = start; End = end; OutcomeId = outcomeId; ParticipantIds = participantIds; }
    public NarrativeText Start { get; }
    public NarrativeText End { get; }
    public ElementId OutcomeId { get; }
    public ImmutableArray<ElementId> ParticipantIds { get; }
}

public sealed record ScenarioDefinition : NarrativeElement
{
    public ScenarioDefinition(ElementId id, ProjectId projectId, ElementId episodeId, ElementName name,
        ScenarioClassification classification, ImmutableArray<NarrativeFact> startingFacts,
        NarrativeText trigger, NarrativeText expectedOutcome, ImmutableArray<ElementId> participantIds,
        int order, UtcTimestamp createdAt, string createdBy)
        : base(id, projectId, episodeId, name, expectedOutcome, order, createdAt, createdBy)
    { Classification = classification; StartingFacts = startingFacts; Trigger = trigger; ExpectedOutcome = expectedOutcome; ParticipantIds = participantIds; }
    public ScenarioClassification Classification { get; }
    public ImmutableArray<NarrativeFact> StartingFacts { get; }
    public NarrativeText Trigger { get; }
    public NarrativeText ExpectedOutcome { get; }
    public ImmutableArray<ElementId> ParticipantIds { get; }
}

public sealed record SceneDefinition : NarrativeElement
{
    public SceneDefinition(ElementId id, ProjectId projectId, ElementId scenarioId, ElementName name,
        NarrativeText setting, NarrativeText responsibility, ImmutableArray<ElementId> participantIds,
        int order, UtcTimestamp createdAt, string createdBy)
        : base(id, projectId, scenarioId, name, responsibility, order, createdAt, createdBy)
    { Setting = setting; Responsibility = responsibility; ParticipantIds = participantIds; }
    public NarrativeText Setting { get; }
    public NarrativeText Responsibility { get; }
    public ImmutableArray<ElementId> ParticipantIds { get; }
}

public sealed record InteractionDefinition : NarrativeElement
{
    public InteractionDefinition(ElementId id, ProjectId projectId, ElementId sceneId, ElementName name,
        ElementId initiatorId, ElementId receiverId, ImmutableArray<NarrativeFact> semanticResults,
        int order, UtcTimestamp createdAt, string createdBy)
        : base(id, projectId, sceneId, name, AcceptedText(name.Value), order, createdAt, createdBy)
    { InitiatorId = initiatorId; ReceiverId = receiverId; SemanticResults = semanticResults; }
    public ElementId InitiatorId { get; }
    public ElementId ReceiverId { get; }
    public ImmutableArray<NarrativeFact> SemanticResults { get; }
    private static NarrativeText AcceptedText(string text) => ((SemanticResult<NarrativeText>.Accepted)NarrativeText.Create(text)).Value;
}

public sealed record StepDefinition : NarrativeElement
{
    public StepDefinition(ElementId id, ProjectId projectId, ElementId interactionId, ElementName name,
        NarrativeText statement, int order, UtcTimestamp createdAt, string createdBy)
        : base(id, projectId, interactionId, name, statement, order, createdAt, createdBy) => Statement = statement;
    public NarrativeText Statement { get; }
}

public sealed record IntentDefinition : NarrativeElement
{
    public IntentDefinition(ElementId id, ProjectId projectId, ElementId interactionId, ElementName name,
        NarrativeText statement, ElementId expressedById, int order, UtcTimestamp createdAt, string createdBy)
        : base(id, projectId, interactionId, name, statement, order, createdAt, createdBy)
    { Statement = statement; ExpressedById = expressedById; }
    public NarrativeText Statement { get; }
    public ElementId ExpressedById { get; }
}

public sealed record ObservationDefinition : NarrativeElement
{
    public ObservationDefinition(ElementId id, ProjectId projectId, ElementId interactionId, ElementName name,
        NarrativeText statement, ElementId visibleToId, int order, UtcTimestamp createdAt, string createdBy)
        : base(id, projectId, interactionId, name, statement, order, createdAt, createdBy)
    { Statement = statement; VisibleToId = visibleToId; }
    public NarrativeText Statement { get; }
    public ElementId VisibleToId { get; }
}
