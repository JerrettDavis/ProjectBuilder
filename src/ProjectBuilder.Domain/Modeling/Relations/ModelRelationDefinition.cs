using System.Collections.Immutable;
using ProjectBuilder.Domain.Modeling.Primitives;

namespace ProjectBuilder.Domain.Modeling.Relations;

public enum ModelElementKind
{
    Actor,
    Outcome,
    Capability,
    Episode,
    Scenario,
    Scene,
    Interaction,
    Intent,
    Step,
    Observation,
    StateDefinition,
    FactDefinition,
    RuleDefinition,
    InvariantDefinition,
    ResultDefinition,
    TransitionDefinition,
    Path,
    Condition,
    EffectDefinition,
    System,
    Interface,
    Boundary,
    Contract,
}

public enum ModelRelationKind
{
    BenefitsFrom,
}

public enum RelationDirection
{
    Directed,
}

public enum RelationCardinality
{
    OneToMany,
}

public enum RelationOwnership
{
    Target,
}

public enum RelationDeletionBehavior
{
    Restrict,
}

public sealed record RelationEndpoint(ModelElementKind SourceKind, ModelElementKind TargetKind);

public sealed record ModelRelationDescriptor(
    ModelRelationKind Kind,
    string Key,
    string DisplayName,
    ImmutableArray<RelationEndpoint> AllowedEndpoints,
    RelationDirection Direction,
    RelationCardinality Cardinality,
    bool IsUnique,
    RelationOwnership Ownership,
    RelationDeletionBehavior DeletionBehavior,
    bool AllowsCycles);

public sealed record ModelRelationDefinition
{
    internal ModelRelationDefinition(
        RelationId id,
        ProjectId projectId,
        ModelRelationKind kind,
        ElementId sourceId,
        ModelElementKind sourceKind,
        ElementId targetId,
        ModelElementKind targetKind,
        UtcTimestamp createdAt,
        string createdBy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(createdBy);
        Id = id;
        ProjectId = projectId;
        Kind = kind;
        SourceId = sourceId;
        SourceKind = sourceKind;
        TargetId = targetId;
        TargetKind = targetKind;
        CreatedAt = createdAt;
        CreatedBy = createdBy;
    }

    public RelationId Id { get; }
    public ProjectId ProjectId { get; }
    public ModelRelationKind Kind { get; }
    public ElementId SourceId { get; }
    public ModelElementKind SourceKind { get; }
    public ElementId TargetId { get; }
    public ModelElementKind TargetKind { get; }
    public UtcTimestamp CreatedAt { get; }
    public string CreatedBy { get; }
}

public static class ModelRelationRegistry
{
    private static readonly ImmutableArray<ModelRelationDescriptor> Registered =
    [
        new(
            ModelRelationKind.BenefitsFrom,
            "benefitsFrom",
            "benefits from",
            [new(ModelElementKind.Actor, ModelElementKind.Outcome)],
            RelationDirection.Directed,
            RelationCardinality.OneToMany,
            true,
            RelationOwnership.Target,
            RelationDeletionBehavior.Restrict,
            false),
    ];

    static ModelRelationRegistry()
    {
        var kinds = Enum.GetValues<ModelRelationKind>();
        if (Registered.Length != kinds.Length ||
            Registered.Select(descriptor => descriptor.Kind).Distinct().Count() != kinds.Length ||
            Registered.Select(descriptor => descriptor.Key).Distinct(StringComparer.Ordinal).Count() != kinds.Length)
        {
            throw new InvalidOperationException("Every relation kind must have exactly one descriptor and stable key.");
        }
    }

    public static ImmutableArray<ModelRelationDescriptor> All => Registered;

    public static ModelRelationDescriptor Describe(ModelRelationKind kind) =>
        Registered.Single(descriptor => descriptor.Kind == kind);

    public static ModelRelationDescriptor Describe(string key) =>
        Registered.Single(descriptor => string.Equals(descriptor.Key, key, StringComparison.Ordinal));

    public static SemanticResult<ModelRelationDefinition> Create(
        RelationId id,
        ProjectId projectId,
        ModelRelationKind kind,
        ElementId sourceId,
        ModelElementKind sourceKind,
        ElementId targetId,
        ModelElementKind targetKind,
        UtcTimestamp createdAt,
        string createdBy,
        IEnumerable<ModelRelationDefinition>? existingRelations = null)
    {
        var descriptor = Describe(kind);
        if (!descriptor.AllowedEndpoints.Contains(new RelationEndpoint(sourceKind, targetKind)))
        {
            return SemanticResult.Reject<ModelRelationDefinition>(
                "PB-REF-002",
                $"Relation '{descriptor.Key}' does not permit {sourceKind} to {targetKind}.");
        }

        if (!descriptor.AllowsCycles && sourceId == targetId)
        {
            return SemanticResult.Reject<ModelRelationDefinition>(
                "PB-REF-002",
                $"Relation '{descriptor.Key}' does not permit a self-reference.");
        }

        var existing = existingRelations?.Where(relation =>
            relation.ProjectId == projectId && relation.Kind == kind).ToArray() ?? [];
        if (descriptor.IsUnique && existing.Any(relation =>
                relation.SourceId == sourceId && relation.TargetId == targetId))
        {
            return SemanticResult.Reject<ModelRelationDefinition>(
                "PB-REF-003",
                $"Relation '{descriptor.Key}' must be unique for the same source and target.");
        }

        var cardinalityViolated = descriptor.Cardinality switch
        {
            RelationCardinality.OneToMany => existing.Any(relation => relation.TargetId == targetId),
            _ => throw new InvalidOperationException($"Cardinality '{descriptor.Cardinality}' has no validator."),
        };
        if (cardinalityViolated)
        {
            return SemanticResult.Reject<ModelRelationDefinition>(
                "PB-REF-003",
                $"Relation '{descriptor.Key}' permits only one source for each target.");
        }

        return SemanticResult.Accept(new ModelRelationDefinition(
            id, projectId, kind, sourceId, sourceKind, targetId, targetKind, createdAt, createdBy));
    }
}
