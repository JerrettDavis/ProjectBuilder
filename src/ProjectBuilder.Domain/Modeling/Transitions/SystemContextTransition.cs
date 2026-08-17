using System.Collections.Immutable;
using ProjectBuilder.Domain.Modeling.Elements;
using ProjectBuilder.Domain.Modeling.Primitives;
using ProjectBuilder.Domain.Projects;

namespace ProjectBuilder.Domain.Modeling.Transitions;

public sealed record SystemContextIds(ElementId OwnedSystemId, ElementId ExternalSystemId,
    ElementId InterfaceId, ElementId BoundaryId, ElementId ContractId);

public sealed record SystemContextDraft(
    ElementName OwnedSystemName, Description OwnedSystemPurpose, ElementId OwnedSystemOwnerId,
    ImmutableArray<LogicTerm> OwnedResponsibilities,
    ElementName ExternalSystemName, Description ExternalSystemPurpose, ElementId ExternalSystemOwnerId,
    ImmutableArray<LogicTerm> ExternalResponsibilities, KnowledgeStatus ExternalKnowledgeStatus,
    ElementName InterfaceName, Description InterfaceDescription, InterfaceKind InterfaceKind,
    ImmutableArray<ElementId> ActorParticipantIds, ImmutableArray<LogicTerm> AcceptedIntents,
    ImmutableArray<LogicTerm> Observations, ImmutableArray<LogicTerm> AccessibilityConstraints,
    ElementName BoundaryName, Description BoundaryDescription, ImmutableArray<BoundaryKind> BoundaryKinds,
    ImmutableArray<ElementId> BoundaryOwnerIds, KnowledgeStatus BoundaryKnowledgeStatus, ElementId? CrossingEffectId,
    ElementName ContractName, Description ContractDescription, ContractKind ContractKind,
    LogicTerm ContractVersion, ElementId ContractOwnerId, LogicStatement SchemaReference,
    LogicStatement CompatibilityPolicy, LogicTerm RequestData, LogicTerm ResponseData,
    LogicTerm DataClassification, KnowledgeStatus ContractKnowledgeStatus);

public sealed record SystemContextDefinitionSet(SystemDefinition OwnedSystem, SystemDefinition ExternalSystem,
    InterfaceDefinition Interface, BoundaryDefinition Boundary, ContractDefinition Contract)
{
    public ImmutableArray<ModelElement> Elements => [OwnedSystem, ExternalSystem, Interface, Boundary, Contract];
}

public abstract record DefineSystemContextTransitionResult
{
    private DefineSystemContextTransitionResult() { }
    public sealed record Accepted(ProjectDefinition Project, SystemContextDefinitionSet Definitions,
        ProjectModelChangeSet ChangeSet) : DefineSystemContextTransitionResult;
    public sealed record Conflict(Revision Expected, Revision Actual, ImmutableArray<SemanticConflict> Conflicts)
        : DefineSystemContextTransitionResult;
    public sealed record Invalid(IReadOnlyList<SemanticError> Errors) : DefineSystemContextTransitionResult;
}

public static class SystemContextTransition
{
    public static DefineSystemContextTransitionResult Define(ProjectDefinition project, Revision expectedRevision,
        SystemContextIds ids, SystemContextDraft draft, int firstOrder, ChangeSetId changeSetId,
        ChangeReason reason, UtcTimestamp occurredAt, string createdBy)
    {
        if (project.Revision != expectedRevision)
            return new DefineSystemContextTransitionResult.Conflict(expectedRevision, project.Revision,
                ProjectChangeSetTransition.RevisionConflicts(expectedRevision, project.Revision));
        if (draft.ActorParticipantIds.IsDefaultOrEmpty)
            return new DefineSystemContextTransitionResult.Invalid([new("PB-SYS-001", "A system interface requires at least one human or system-role participant.")]);
        if (draft.OwnedSystemOwnerId == draft.ExternalSystemOwnerId && draft.ExternalKnowledgeStatus == KnowledgeStatus.Known)
            return new DefineSystemContextTransitionResult.Invalid([new("PB-SYS-002", "A known external authority must not silently reuse the owned-system authority.")]);

        var owned = new SystemDefinition(ids.OwnedSystemId, project.Id, draft.OwnedSystemName,
            draft.OwnedSystemPurpose, SystemClassification.Owned, draft.OwnedSystemOwnerId,
            draft.OwnedResponsibilities, firstOrder, occurredAt, createdBy);
        var external = new SystemDefinition(ids.ExternalSystemId, project.Id, draft.ExternalSystemName,
            draft.ExternalSystemPurpose, SystemClassification.External, draft.ExternalSystemOwnerId,
            draft.ExternalResponsibilities, firstOrder + 1, occurredAt, createdBy, draft.ExternalKnowledgeStatus);
        var contract = new ContractDefinition(ids.ContractId, project.Id, ids.BoundaryId, draft.ContractName,
            draft.ContractDescription, draft.ContractKind, draft.ContractVersion, draft.ContractOwnerId,
            draft.SchemaReference, draft.CompatibilityPolicy, draft.RequestData, draft.ResponseData,
            draft.DataClassification, firstOrder + 4, occurredAt, createdBy, draft.ContractKnowledgeStatus);
        var participantIds = draft.ActorParticipantIds.Append(external.Id).Distinct().ToImmutableArray();
        var systemInterface = new InterfaceDefinition(ids.InterfaceId, project.Id, owned.Id, draft.InterfaceName,
            draft.InterfaceDescription, draft.InterfaceKind, participantIds, draft.AcceptedIntents,
            draft.Observations, draft.AccessibilityConstraints, contract.Id, firstOrder + 2, occurredAt, createdBy);
        var boundary = new BoundaryDefinition(ids.BoundaryId, project.Id, systemInterface.Id, draft.BoundaryName,
            draft.BoundaryDescription, draft.BoundaryKinds, draft.BoundaryOwnerIds, owned.Id, external.Id,
            draft.CrossingEffectId, firstOrder + 3, occurredAt, createdBy, draft.BoundaryKnowledgeStatus);
        var definitions = new SystemContextDefinitionSet(owned, external, systemInterface, boundary, contract);
        var committed = ProjectChangeSetTransition.Commit(project, expectedRevision,
            new(changeSetId, owned.Id, "system-context.defined", reason,
                ProjectChangeSetTransition.AddedElements(definitions.Elements)), occurredAt, createdBy);
        var accepted = (ProjectChangeSetTransitionResult.Accepted)committed;
        return new DefineSystemContextTransitionResult.Accepted(accepted.Project, definitions, accepted.ChangeSet);
    }
}
