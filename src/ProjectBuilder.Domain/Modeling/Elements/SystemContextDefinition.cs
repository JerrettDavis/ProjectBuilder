using System.Collections.Immutable;
using ProjectBuilder.Domain.Modeling.Primitives;

namespace ProjectBuilder.Domain.Modeling.Elements;

public enum SystemClassification { Owned, External }
public enum InterfaceKind { Graphical, Cli, Http, Rpc, Event, Mcp, Device, Document, HumanProcedure }
public enum BoundaryKind { Ownership, Trust, Transaction, Process, Deployment, Protocol, DataResidency, Vendor, FailureDomain, HumanHandoff }
public enum ContractKind { Api, Message, Event, Device, File, Human, Schema, Policy, Other }

public abstract record SystemContextElement : ModelElement
{
    protected SystemContextElement(ElementId id, ProjectId projectId, ElementId? parentId, ElementName name,
        Description description, int order, UtcTimestamp createdAt, string createdBy, KnowledgeStatus knowledgeStatus)
        : base(id, projectId, parentId, name, description, DefinitionStatus.Defined, knowledgeStatus,
            order, createdAt, createdBy, 1)
    { }
}

public sealed record SystemDefinition : SystemContextElement
{
    public SystemDefinition(ElementId id, ProjectId projectId, ElementName name, Description purpose,
        SystemClassification classification, ElementId ownerId, ImmutableArray<LogicTerm> responsibilities,
        int order, UtcTimestamp createdAt, string createdBy, KnowledgeStatus knowledgeStatus = KnowledgeStatus.Known)
        : base(id, projectId, null, name, purpose, order, createdAt, createdBy, knowledgeStatus)
    {
        if (responsibilities.IsDefaultOrEmpty) throw new ArgumentException("System responsibilities are required.", nameof(responsibilities));
        Purpose = purpose; Classification = classification; OwnerId = ownerId; Responsibilities = responsibilities;
    }
    public Description Purpose { get; }
    public SystemClassification Classification { get; }
    public ElementId OwnerId { get; }
    public ImmutableArray<LogicTerm> Responsibilities { get; }
}

public sealed record InterfaceDefinition : SystemContextElement
{
    public InterfaceDefinition(ElementId id, ProjectId projectId, ElementId systemId, ElementName name,
        Description description, InterfaceKind kind, ImmutableArray<ElementId> participantIds,
        ImmutableArray<LogicTerm> acceptedIntents, ImmutableArray<LogicTerm> observations,
        ImmutableArray<LogicTerm> accessibilityConstraints, ElementId contractId,
        int order, UtcTimestamp createdAt, string createdBy)
        : base(id, projectId, systemId, name, description, order, createdAt, createdBy, KnowledgeStatus.Known)
    {
        if (participantIds.IsDefaultOrEmpty) throw new ArgumentException("Interface participants are required.", nameof(participantIds));
        if (acceptedIntents.IsDefaultOrEmpty) throw new ArgumentException("Accepted intents are required.", nameof(acceptedIntents));
        if (observations.IsDefaultOrEmpty) throw new ArgumentException("Interface observations are required.", nameof(observations));
        Kind = kind; ParticipantIds = participantIds; AcceptedIntents = acceptedIntents; Observations = observations;
        AccessibilityConstraints = accessibilityConstraints; ContractId = contractId;
    }
    public InterfaceKind Kind { get; }
    public ImmutableArray<ElementId> ParticipantIds { get; }
    public ImmutableArray<LogicTerm> AcceptedIntents { get; }
    public ImmutableArray<LogicTerm> Observations { get; }
    public ImmutableArray<LogicTerm> AccessibilityConstraints { get; }
    public ElementId ContractId { get; }
}

public sealed record BoundaryDefinition : SystemContextElement
{
    public BoundaryDefinition(ElementId id, ProjectId projectId, ElementId interfaceId, ElementName name,
        Description description, ImmutableArray<BoundaryKind> kinds, ImmutableArray<ElementId> ownerIds,
        ElementId sourceSystemId, ElementId targetSystemId, ElementId? crossingEffectId,
        int order, UtcTimestamp createdAt, string createdBy, KnowledgeStatus knowledgeStatus)
        : base(id, projectId, interfaceId, name, description, order, createdAt, createdBy, knowledgeStatus)
    {
        if (kinds.IsDefaultOrEmpty) throw new ArgumentException("Boundary kinds are required.", nameof(kinds));
        if (ownerIds.IsDefaultOrEmpty) throw new ArgumentException("Boundary owners are required.", nameof(ownerIds));
        Kinds = kinds; OwnerIds = ownerIds; SourceSystemId = sourceSystemId; TargetSystemId = targetSystemId;
        CrossingEffectId = crossingEffectId;
    }
    public ImmutableArray<BoundaryKind> Kinds { get; }
    public ImmutableArray<ElementId> OwnerIds { get; }
    public ElementId SourceSystemId { get; }
    public ElementId TargetSystemId { get; }
    public ElementId? CrossingEffectId { get; }
}

public sealed record ContractDefinition : SystemContextElement
{
    public ContractDefinition(ElementId id, ProjectId projectId, ElementId boundaryId, ElementName name,
        Description description, ContractKind kind, LogicTerm contractVersion, ElementId ownerId,
        LogicStatement schemaReference, LogicStatement compatibilityPolicy,
        LogicTerm requestData, LogicTerm responseData, LogicTerm dataClassification,
        int order, UtcTimestamp createdAt, string createdBy, KnowledgeStatus knowledgeStatus)
        : base(id, projectId, boundaryId, name, description, order, createdAt, createdBy, knowledgeStatus)
    {
        Kind = kind; ContractVersion = contractVersion; OwnerId = ownerId; SchemaReference = schemaReference;
        CompatibilityPolicy = compatibilityPolicy; RequestData = requestData; ResponseData = responseData;
        DataClassification = dataClassification;
    }
    public ContractKind Kind { get; }
    public LogicTerm ContractVersion { get; }
    public ElementId OwnerId { get; }
    public LogicStatement SchemaReference { get; }
    public LogicStatement CompatibilityPolicy { get; }
    public LogicTerm RequestData { get; }
    public LogicTerm ResponseData { get; }
    public LogicTerm DataClassification { get; }
}
