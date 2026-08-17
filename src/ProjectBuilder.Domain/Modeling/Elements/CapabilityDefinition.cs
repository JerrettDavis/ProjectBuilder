using System.Collections.Immutable;
using ProjectBuilder.Domain.Modeling.Primitives;

namespace ProjectBuilder.Domain.Modeling.Elements;

public enum CapabilityPriority { Critical, High, Normal, Low }

public sealed record CapabilityDefinition : ModelElement
{
    public CapabilityDefinition(
        ElementId id, ProjectId projectId, ElementName name, Description ability,
        ImmutableArray<ElementId> outcomeIds, CapabilityPriority priority, int order,
        UtcTimestamp createdAt, string createdBy, KnowledgeStatus knowledgeStatus = KnowledgeStatus.Known)
        : base(id, projectId, null, name, ability, DefinitionStatus.Defined, knowledgeStatus,
            order, createdAt, createdBy, 1)
    {
        if (outcomeIds.IsDefaultOrEmpty) throw new ArgumentException("A capability must contribute to at least one outcome.", nameof(outcomeIds));
        if (outcomeIds.Distinct().Count() != outcomeIds.Length) throw new ArgumentException("Capability outcomes must be unique.", nameof(outcomeIds));
        OutcomeIds = outcomeIds;
        Priority = priority;
    }

    public ImmutableArray<ElementId> OutcomeIds { get; }
    public CapabilityPriority Priority { get; }
}
