using System.Diagnostics;
using ProjectBuilder.Domain.Modeling.Elements;
using ProjectBuilder.Domain.Modeling.Primitives;
using ProjectBuilder.Domain.Modeling.Relations;

namespace ProjectBuilder.Application.Modeling;

internal static class ModelApplicationMapping
{
    internal static ActorOverview Actor(ActorDefinition actor) =>
        new(
            actor.Id.ToString(),
            actor.Name.Value,
            ActorKindText(actor.ActorKind),
            actor.ContextualRole.Value,
            actor.Goals.Select(statement => statement.Value).ToArray(),
            actor.Responsibilities.Select(statement => statement.Value).ToArray(),
            actor.Authority.Select(statement => statement.Value).ToArray(),
            actor.Constraints.Select(statement => statement.Value).ToArray(),
            KnowledgeStatusText(actor.KnowledgeStatus));

    internal static OutcomeOverview Outcome(StoredOutcome outcome) =>
        new(
            outcome.Outcome.Id.ToString(),
            outcome.Outcome.Name.Value,
            outcome.Outcome.Statement.Value,
            outcome.Outcome.SuccessSignals.Select(signal => signal.Value).ToArray(),
            outcome.BeneficiaryActorId.ToString(),
            outcome.BeneficiaryName,
            KnowledgeStatusText(outcome.Outcome.KnowledgeStatus));

    internal static CapabilityOverview Capability(CapabilityDefinition capability) => new(
        capability.Id.ToString(), capability.Name.Value, capability.Description.Value,
        capability.OutcomeIds.Select(id => id.ToString()).ToArray(),
        char.ToLowerInvariant(capability.Priority.ToString()[0]) + capability.Priority.ToString()[1..],
        KnowledgeStatusText(capability.KnowledgeStatus));

    internal static RelationOverview Relation(StoredModelRelation stored)
    {
        var relation = stored.Relation;
        var descriptor = ModelRelationRegistry.Describe(relation.Kind);
        return new(
            relation.Id.ToString(),
            descriptor.Key,
            descriptor.DisplayName,
            relation.SourceId.ToString(),
            ElementKindText(relation.SourceKind),
            stored.SourceName,
            relation.TargetId.ToString(),
            ElementKindText(relation.TargetKind),
            stored.TargetName,
            DirectionText(descriptor.Direction),
            CardinalityText(descriptor.Cardinality),
            descriptor.IsUnique,
            OwnershipText(descriptor.Ownership),
            DeletionBehaviorText(descriptor.DeletionBehavior));
    }

    internal static IReadOnlyList<ChangeSetConflictOverview> Conflicts(
        IEnumerable<ProjectBuilder.Domain.Modeling.Transitions.SemanticConflict> conflicts) =>
        conflicts.Select(conflict => new ChangeSetConflictOverview(
            conflict.Code,
            conflict.Message,
            conflict.Expected.Value,
            conflict.Actual.Value)).ToArray();

    internal static IReadOnlyList<ChangeSetConflictOverview> RevisionConflict(Revision expected, Revision actual) =>
        Conflicts(ProjectBuilder.Domain.Modeling.Transitions.ProjectChangeSetTransition.RevisionConflicts(expected, actual));

    private static string ElementKindText(ModelElementKind kind) => kind switch
    {
        ModelElementKind.Actor => "actor",
        ModelElementKind.Outcome => "outcome",
        _ => throw new UnreachableException(),
    };

    private static string DirectionText(RelationDirection direction) => direction switch
    {
        RelationDirection.Directed => "directed",
        _ => throw new UnreachableException(),
    };

    private static string CardinalityText(RelationCardinality cardinality) => cardinality switch
    {
        RelationCardinality.OneToMany => "oneToMany",
        _ => throw new UnreachableException(),
    };

    private static string OwnershipText(RelationOwnership ownership) => ownership switch
    {
        RelationOwnership.Target => "target",
        _ => throw new UnreachableException(),
    };

    private static string DeletionBehaviorText(RelationDeletionBehavior behavior) => behavior switch
    {
        RelationDeletionBehavior.Restrict => "restrict",
        _ => throw new UnreachableException(),
    };

    internal static string ActorKindText(ActorKind kind) => kind switch
    {
        ActorKind.HumanRole => "humanRole",
        ActorKind.OrganizationRole => "organizationRole",
        ActorKind.SystemRole => "systemRole",
        ActorKind.DeviceRole => "deviceRole",
        ActorKind.AutomatedRole => "automatedRole",
        ActorKind.ExternalProviderRole => "externalProviderRole",
        _ => throw new UnreachableException(),
    };

    internal static SemanticResult<ActorKind> ParseActorKind(string? value) => value switch
    {
        "humanRole" => SemanticResult.Accept(ActorKind.HumanRole),
        "organizationRole" => SemanticResult.Accept(ActorKind.OrganizationRole),
        "systemRole" => SemanticResult.Accept(ActorKind.SystemRole),
        "deviceRole" => SemanticResult.Accept(ActorKind.DeviceRole),
        "automatedRole" => SemanticResult.Accept(ActorKind.AutomatedRole),
        "externalProviderRole" => SemanticResult.Accept(ActorKind.ExternalProviderRole),
        _ => SemanticResult.Reject<ActorKind>("actor.kind.invalid", "Select a supported actor role kind."),
    };

    internal static string KnowledgeStatusText(KnowledgeStatus status) => status switch
    {
        KnowledgeStatus.Known => "known",
        KnowledgeStatus.Unknown => "unknown",
        KnowledgeStatus.Assumed => "assumed",
        KnowledgeStatus.Deferred => "deferred",
        KnowledgeStatus.Disputed => "disputed",
        KnowledgeStatus.NotApplicable => "notApplicable",
        _ => throw new UnreachableException(),
    };

    internal static SemanticResult<KnowledgeStatus> ParseKnowledgeStatus(string? value) => value switch
    {
        "known" => SemanticResult.Accept(KnowledgeStatus.Known),
        "unknown" => SemanticResult.Accept(KnowledgeStatus.Unknown),
        "assumed" => SemanticResult.Accept(KnowledgeStatus.Assumed),
        "deferred" => SemanticResult.Accept(KnowledgeStatus.Deferred),
        "disputed" => SemanticResult.Accept(KnowledgeStatus.Disputed),
        "notApplicable" => SemanticResult.Accept(KnowledgeStatus.NotApplicable),
        _ => SemanticResult.Reject<KnowledgeStatus>("actor.knowledge_status.invalid", "Select a supported knowledge status."),
    };
}
