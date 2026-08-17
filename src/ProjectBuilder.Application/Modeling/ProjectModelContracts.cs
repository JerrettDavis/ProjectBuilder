using System.Collections.Immutable;
using ProjectBuilder.Application.Projects.CreateProject;
using ProjectBuilder.Domain.Modeling.Elements;
using ProjectBuilder.Domain.Modeling.Gaps;
using ProjectBuilder.Domain.Modeling.Primitives;
using ProjectBuilder.Domain.Modeling.Relations;
using ProjectBuilder.Domain.Modeling.Transitions;
using ProjectBuilder.Domain.Projects;

namespace ProjectBuilder.Application.Modeling;

public sealed record ActorOverview(
    string Id,
    string Name,
    string ActorKind,
    string ContextualRole,
    IReadOnlyList<string> Goals,
    IReadOnlyList<string> Responsibilities,
    IReadOnlyList<string> Authority,
    IReadOnlyList<string> Constraints,
    string KnowledgeStatus);

public sealed record OutcomeOverview(
    string Id,
    string Name,
    string Statement,
    IReadOnlyList<string> SuccessSignals,
    string BeneficiaryActorId,
    string BeneficiaryName,
    string KnowledgeStatus);

public sealed record CapabilityOverview(
    string Id, string Name, string Ability, IReadOnlyList<string> OutcomeIds,
    string Priority, string KnowledgeStatus);

public sealed record ProjectModelOverview(
    ProjectOverview Project,
    IReadOnlyList<ActorOverview> Actors,
    IReadOnlyList<OutcomeOverview> Outcomes,
    IReadOnlyList<NarrativeOverview> Narratives,
    IReadOnlyList<StateLogicOverview> StateLogic,
    IReadOnlyList<PathOverview> Paths,
    IReadOnlyList<RelationOverview> Relations,
    IReadOnlyList<ChangeSetOverview> ChangeSets,
    IReadOnlyList<GapDispositionOverview>? GapDispositions = null,
    IReadOnlyList<CapabilityOverview>? Capabilities = null,
    IReadOnlyList<SystemContextOverview>? SystemContexts = null);

public sealed record ChangeSetOverview(
    string Id,
    long? BaseRevision,
    long ResultRevision,
    string ChangeKind,
    string Reason,
    string CreatedBy,
    string OccurredAt,
    int OperationCount,
    string SemanticSummary,
    IReadOnlyList<ChangeOperationOverview> Operations);

public sealed record ChangeOperationOverview(
    int Sequence,
    string Kind,
    string SubjectKind,
    string? ElementId,
    string? RelationId,
    string Summary);

public sealed record ChangeSetConflictOverview(
    string Code,
    string Message,
    long ExpectedRevision,
    long ActualRevision);

public sealed record RelationOverview(
    string Id,
    string Kind,
    string DisplayName,
    string SourceElementId,
    string SourceKind,
    string SourceName,
    string TargetElementId,
    string TargetKind,
    string TargetName,
    string Direction,
    string Cardinality,
    bool IsUnique,
    string Ownership,
    string DeletionBehavior);

public sealed record NarrativeOverview(
    string EpisodeId, string EpisodeName, string Start, string End, string OutcomeName,
    string ScenarioId, string ScenarioName, string Classification, IReadOnlyList<string> StartingFacts,
    string Trigger, string ExpectedOutcome, string SceneName, string Setting,
    string Responsibility, string InteractionName, string InitiatorName, string ReceiverName,
    string Intent, string Step, string Observation, IReadOnlyList<string> SemanticResults,
    string OutcomeId = "", string SceneId = "", string InitiatorId = "", string ReceiverId = "",
    string InteractionId = "", string IntentId = "", string StepId = "", string ObservationId = "");

public sealed record StateLogicOverview(
    string StateId, string StateName, string StateCategory, IReadOnlyList<string> Structure,
    IReadOnlyList<string> Values, string OwnerName,
    string FactId, string FactName, string FactValueType, string FactAuthority, string FactMutability,
    string RuleId, string RuleName, string RuleKind, string RuleStatement,
    string InvariantName, string InvariantStatement, string FalsifyingExample,
    IReadOnlyList<string> ProofExpectation, IReadOnlyList<SemanticResultOverview> Results,
    string TransitionId, string TransitionName, string SourcePredicate, string Trigger, string TargetPredicate,
    string OwnerId = "", IReadOnlyList<string>? FactAllowedKnowledge = null,
    string RuleAuthorityOwnerId = "", string InvariantId = "", IReadOnlyList<string>? InvariantScopeIds = null,
    IReadOnlyList<string>? ChangedFactIds = null, IReadOnlyList<string>? RuleIds = null,
    IReadOnlyList<string>? InvariantIds = null, IReadOnlyList<string>? ResultIds = null,
    string RuleAuthorityOwnerName = "");

public sealed record SemanticResultOverview(string Id, string Name, string Kind, string Meaning);

public sealed record PathOverview(
    string BranchPathId, string BranchName, string BranchClassification,
    string ScenarioName, string SourceTransitionName,
    string BranchConditionName, string BranchConditionKind, string BranchCondition,
    IReadOnlyList<string> BranchSegments, string TerminalResultName, string TerminalResultKind,
    string BranchTerminalState, string BranchObservation, string OwnerName,
    string EffectName, string EffectKind, string EffectStatement,
    string RecoveryPathId, string RecoveryName, string RecoveryStrategy,
    string RecoveryCondition, IReadOnlyList<string> RecoverySegments,
    string RecoveryResultName, string RecoveryTerminalState, string RecoveryObservation,
    string RetryPolicy, string IdempotencyAnalysis, string ExitCondition, string Reconciliation,
    string ScenarioId = "", string SourceTransitionId = "", string BranchConditionId = "",
    string TerminalResultId = "", string OwnerId = "", string EffectId = "",
    string RecoveryConditionId = "", string RecoveryResultId = "");

public sealed record SystemContextOverview(
    string OwnedSystemId, string OwnedSystemName, string OwnedSystemPurpose, string OwnedSystemOwnerId,
    string OwnedSystemOwnerName, IReadOnlyList<string> OwnedResponsibilities,
    string ExternalSystemId, string ExternalSystemName, string ExternalSystemPurpose, string ExternalSystemOwnerId,
    string ExternalSystemOwnerName, IReadOnlyList<string> ExternalResponsibilities, string ExternalKnowledgeStatus,
    string InterfaceId, string InterfaceName, string InterfaceDescription, string InterfaceKind,
    IReadOnlyList<string> ParticipantIds, IReadOnlyList<string> ParticipantNames,
    IReadOnlyList<string> AcceptedIntents, IReadOnlyList<string> Observations,
    IReadOnlyList<string> AccessibilityConstraints,
    string BoundaryId, string BoundaryName, string BoundaryDescription, IReadOnlyList<string> BoundaryKinds,
    IReadOnlyList<string> BoundaryOwnerIds, IReadOnlyList<string> BoundaryOwnerNames,
    string BoundaryKnowledgeStatus, string? CrossingEffectId, string? CrossingEffectName,
    string ContractId, string ContractName, string ContractDescription, string ContractKind,
    string ContractVersion, string ContractOwnerId, string ContractOwnerName, string SchemaReference,
    string CompatibilityPolicy, string RequestData, string ResponseData, string DataClassification,
    string ContractKnowledgeStatus);

public interface IProjectElementStore
{
    ValueTask<StoredElementCommit?> FindCommitByOperationAsync(
        ChangeSetId operationId,
        CancellationToken cancellationToken);

    ValueTask<ActorDefinition?> FindActorAsync(
        ProjectId projectId,
        ElementId actorId,
        CancellationToken cancellationToken);

    ValueTask<StoredOutcome?> FindOutcomeAsync(
        ProjectId projectId,
        ElementId outcomeId,
        CancellationToken cancellationToken);

    ValueTask<CapabilityDefinition?> FindCapabilityAsync(ProjectId projectId, ElementId capabilityId, CancellationToken cancellationToken);

    ValueTask<int> NextElementOrderAsync(ProjectId projectId, CancellationToken cancellationToken);

    ValueTask<ElementStoreCommitResult> CommitActorAsync(
        AddActorTransitionResult.Accepted commit,
        string requestFingerprint,
        CancellationToken cancellationToken);

    ValueTask<ElementStoreCommitResult> CommitOutcomeAsync(
        AddOutcomeTransitionResult.Accepted commit,
        string requestFingerprint,
        CancellationToken cancellationToken);

    ValueTask<ElementStoreCommitResult> CommitCapabilityAsync(AddCapabilityTransitionResult.Accepted commit, string requestFingerprint, CancellationToken cancellationToken);

    ValueTask<ElementStoreCommitResult> UpdateActorAsync(
        UpdateActorTransitionResult.Accepted commit,
        string requestFingerprint,
        CancellationToken cancellationToken);

    ValueTask<ElementStoreCommitResult> UpdateOutcomeAsync(
        UpdateOutcomeTransitionResult.Accepted commit,
        string requestFingerprint,
        CancellationToken cancellationToken);

    ValueTask<ElementStoreCommitResult> CommitNarrativeAsync(
        DefineNarrativeTransitionResult.Accepted commit,
        string requestFingerprint,
        CancellationToken cancellationToken);

    ValueTask<NarrativeOverview?> FindNarrativeAsync(
        ProjectId projectId,
        ElementId episodeId,
        CancellationToken cancellationToken);

    ValueTask<ElementStoreCommitResult> CommitStateLogicAsync(
        DefineStateLogicTransitionResult.Accepted commit,
        string requestFingerprint,
        CancellationToken cancellationToken);

    ValueTask<StateLogicOverview?> FindStateLogicAsync(
        ProjectId projectId,
        ElementId stateId,
        CancellationToken cancellationToken);

    ValueTask<ElementStoreCommitResult> CommitSystemContextAsync(
        DefineSystemContextTransitionResult.Accepted commit,
        string requestFingerprint,
        CancellationToken cancellationToken);

    ValueTask<SystemContextOverview?> FindSystemContextAsync(
        ProjectId projectId,
        ElementId ownedSystemId,
        CancellationToken cancellationToken);

    ValueTask<SemanticResultDefinition?> FindSemanticResultAsync(
        ProjectId projectId,
        ElementId resultId,
        CancellationToken cancellationToken);

    ValueTask<ElementStoreCommitResult> CommitPathAsync(
        DefinePathTransitionResult.Accepted commit,
        string requestFingerprint,
        CancellationToken cancellationToken);

    ValueTask<ElementStoreCommitResult> CommitGapDispositionAsync(
        RecordGapDispositionTransitionResult.Accepted commit,
        string requestFingerprint,
        CancellationToken cancellationToken);

    ValueTask<PathOverview?> FindPathAsync(
        ProjectId projectId,
        ElementId branchPathId,
        CancellationToken cancellationToken);

    ValueTask<ProjectModelSnapshot> LoadModelAsync(ProjectId projectId, CancellationToken cancellationToken);

    ValueTask<ImmutableArray<ChangeSetOverview>> LoadChangeHistoryAsync(
        ProjectId projectId,
        CancellationToken cancellationToken);
}

public sealed record StoredElementCommit(
    string ChangeKind,
    string RequestFingerprint,
    Revision ResultRevision,
    ElementId ElementId);

public sealed record ProjectModelSnapshot(
    ImmutableArray<ActorDefinition> Actors,
    ImmutableArray<StoredOutcome> Outcomes,
    ImmutableArray<CapabilityDefinition> Capabilities,
    ImmutableArray<NarrativeOverview> Narratives,
    ImmutableArray<StateLogicOverview> StateLogic,
    ImmutableArray<PathOverview> Paths,
    ImmutableArray<StoredModelRelation> Relations,
    ImmutableArray<GapDispositionOverview> GapDispositions = default,
    ImmutableArray<SystemContextOverview> SystemContexts = default);

public sealed record GapDispositionOverview(
    string Id, string ProfileId, string RuleCode, string ScopeId, string Disposition,
    string Rationale, string Consequence, string AuthorityActorId, string AuthorityName,
    string? ReviewOn, string? TargetMilestone, string CreatedAt, string CreatedBy);

public sealed record StoredOutcome(
    OutcomeDefinition Outcome,
    RelationId BeneficiaryRelationId,
    ElementId BeneficiaryActorId,
    string BeneficiaryName);

public sealed record StoredModelRelation(
    ModelRelationDefinition Relation,
    string SourceName,
    string TargetName);

public abstract record ElementStoreCommitResult
{
    private ElementStoreCommitResult()
    {
    }

    public sealed record Committed : ElementStoreCommitResult;
    public sealed record RevisionConflict(Revision Actual) : ElementStoreCommitResult;
    public sealed record OperationConflict : ElementStoreCommitResult;
}

public interface IProjectEditAuthorizer
{
    ValueTask<ProjectCreationAuthorization> AuthorizeEditAsync(
        ProjectActor actor,
        WorkspaceId workspaceId,
        CancellationToken cancellationToken);
}

public interface IModelIdentitySource
{
    ElementId NextElementId();
    RelationId NextRelationId();
}
