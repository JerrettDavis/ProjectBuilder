using ProjectBuilder.Domain.Modeling.Primitives;

namespace ProjectBuilder.Domain.Modeling.Elements;

public enum DefinitionStatus
{
    Draft,
    Defined,
    ReviewRequested,
    Approved,
    Implemented,
    Verified,
    Deprecated,
    Superseded,
    Rejected,
}

public enum KnowledgeStatus
{
    Known,
    Unknown,
    Assumed,
    Deferred,
    Disputed,
    NotApplicable,
}

public abstract record ModelElement
{
    protected ModelElement(
        ElementId id,
        ProjectId projectId,
        ElementId? parentId,
        ElementName name,
        Description description,
        DefinitionStatus definitionStatus,
        KnowledgeStatus knowledgeStatus,
        int order,
        UtcTimestamp createdAt,
        string createdBy,
        long version)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(order);
        ArgumentOutOfRangeException.ThrowIfLessThan(version, 1);
        ArgumentException.ThrowIfNullOrWhiteSpace(createdBy);

        Id = id;
        ProjectId = projectId;
        ParentId = parentId;
        Name = name;
        Description = description;
        DefinitionStatus = definitionStatus;
        KnowledgeStatus = knowledgeStatus;
        Order = order;
        CreatedAt = createdAt;
        CreatedBy = createdBy;
        Version = version;
    }

    public ElementId Id { get; }
    public ProjectId ProjectId { get; }
    public ElementId? ParentId { get; }
    public ElementName Name { get; }
    public Description Description { get; }
    public DefinitionStatus DefinitionStatus { get; }
    public KnowledgeStatus KnowledgeStatus { get; }
    public int Order { get; }
    public UtcTimestamp CreatedAt { get; }
    public string CreatedBy { get; }
    public long Version { get; }
}
