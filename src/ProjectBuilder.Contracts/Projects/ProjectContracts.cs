namespace ProjectBuilder.Contracts.Projects;

public sealed record CreateProjectRequest(
    string Name,
    string Purpose,
    string IntendedOutcome,
    string Reason);

public sealed record ProjectResponse(
    string Id,
    string WorkspaceId,
    string Name,
    string Purpose,
    string IntendedOutcome,
    long Revision,
    string CreationReason,
    string CreatedAt,
    string AllowedNextAction);

public sealed record ProjectProblemResponse(
    string Code,
    string Title,
    IReadOnlyDictionary<string, string[]> Errors);

public sealed record LocalWorkspaceResponse(string Id, string Name, string AccessMode);

public sealed record AddActorRequest(
    string Name,
    string ActorKind,
    string ContextualRole,
    string Goals,
    string Responsibilities,
    string Authority,
    string Constraints,
    string Reason,
    string KnowledgeStatus = "known");

public sealed record AddActorResponse(ActorResponse Actor, long Revision, string AllowedNextAction);
public sealed record UpdateActorRequest(string Name, string ActorKind, string ContextualRole, string Goals, string Responsibilities, string Authority, string Constraints, string Reason, string KnowledgeStatus = "known");
public sealed record UpdateActorResponse(ActorResponse Actor, long Revision, string AllowedNextAction);

public sealed record AddOutcomeRequest(
    string Name,
    string Statement,
    string SuccessSignals,
    string BeneficiaryActorId,
    string Reason,
    string KnowledgeStatus = "known");

public sealed record AddOutcomeResponse(OutcomeResponse Outcome, long Revision, string AllowedNextAction);
public sealed record UpdateOutcomeRequest(string Name, string Statement, string SuccessSignals, string BeneficiaryActorId, string Reason, string KnowledgeStatus = "known");
public sealed record UpdateOutcomeResponse(OutcomeResponse Outcome, long Revision, string AllowedNextAction);

public sealed record AddCapabilityRequest(
    string Name, string Ability, IReadOnlyList<string> OutcomeIds, string Priority,
    string Reason, string KnowledgeStatus = "known");
public sealed record AddCapabilityResponse(CapabilityResponse Capability, long Revision, string AllowedNextAction);

public sealed record ActorResponse(
    string Id,
    string Name,
    string ActorKind,
    string ContextualRole,
    IReadOnlyList<string> Goals,
    IReadOnlyList<string> Responsibilities,
    IReadOnlyList<string> Authority,
    IReadOnlyList<string> Constraints,
    string KnowledgeStatus);

public sealed record OutcomeResponse(
    string Id,
    string Name,
    string Statement,
    IReadOnlyList<string> SuccessSignals,
    string BeneficiaryActorId,
    string BeneficiaryName,
    string KnowledgeStatus);

public sealed record CapabilityResponse(
    string Id, string Name, string Ability, IReadOnlyList<string> OutcomeIds,
    string Priority, string KnowledgeStatus);

public sealed record ProjectModelResponse(
    ProjectResponse Project,
    IReadOnlyList<ActorResponse> Actors,
    IReadOnlyList<OutcomeResponse> Outcomes,
    IReadOnlyList<NarrativeResponse> Narratives,
    IReadOnlyList<StateLogicResponse> StateLogic,
    IReadOnlyList<PathResponse> Paths,
    IReadOnlyList<RelationResponse> Relations,
    IReadOnlyList<ChangeSetResponse> ChangeSets,
    IReadOnlyList<CapabilityResponse>? Capabilities = null,
    IReadOnlyList<SystemContextResponse>? SystemContexts = null);

public sealed record ProjectFindingsResponse(
    string ProjectId, string ProjectName, long Revision, PurposeProfileResponse Profile,
    IReadOnlyList<PurposeProfileResponse> AvailableProfiles,
    IReadOnlyList<CoverageDimensionResponse> Coverage,
    IReadOnlyList<ProfilePredicateResponse> Predicates,
    IReadOnlyList<ProjectFindingResponse> Findings,
    IReadOnlyList<EvidenceRequirementResponse> EvidenceRequirements,
    IReadOnlyList<GapAuthorityResponse> Authorities);

public sealed record GapAuthorityResponse(string Id, string Name, string ContextualRole);

public sealed record PurposeProfileResponse(string Id, string Name, string Description);
public sealed record CoverageDimensionResponse(
    string Id, string Name, string Status, bool Required, int FindingCount, string Explanation, string RepairPath);
public sealed record ProfilePredicateResponse(string Code, string Name, bool Satisfied, string Explanation);

public sealed record ProjectFindingResponse(
    string Code, string Severity, string Status, string Category, string Title, string Explanation,
    string Rule, string ScopeId, string ScopeKind, string ScopeName, string Owner,
    string RepairLabel, string RepairPath, bool RepairAvailable,
    string? DispositionId = null, string? DispositionRationale = null, string? DispositionConsequence = null,
    string? AuthorityActorId = null, string? AuthorityName = null, string? ReviewOn = null, string? TargetMilestone = null);

public sealed record RecordGapDispositionRequest(
    string ProfileId, string RuleCode, string ScopeId, string Disposition,
    string Rationale, string Consequence, string AuthorityActorId,
    string? ReviewOn, string? TargetMilestone, string Reason);

public sealed record GapDispositionResponse(
    string Id, string ProfileId, string RuleCode, string ScopeId, string Disposition,
    string Rationale, string Consequence, string AuthorityActorId, string AuthorityName,
    string? ReviewOn, string? TargetMilestone, string CreatedAt, string CreatedBy, long Revision);

public sealed record ProjectGuidanceResponse(
    string ProjectId, string ProjectName, long Revision, string RegistryVersion,
    IReadOnlyList<GuidanceStageResponse> Stages, IReadOnlyList<GuidancePromptResponse> Prompts);

public sealed record GuidanceStageResponse(
    string Id, string Name, string Status, int ApplicablePromptCount, string Explanation);

public sealed record GuidancePromptResponse(
    string Id, int Version, string Stage, int Order, string Question, string WhyThisMatters,
    string LearningContent, string TriggerExplanation, IReadOnlyList<string> RelatedFactKinds,
    IReadOnlyList<string> Examples, IReadOnlyList<GuidanceAnswerResponse> AnswerMappings,
    string PrimaryRepairPath);

public sealed record GuidanceAnswerResponse(
    string Key, string Label, string Kind, string ResultingChange, bool RequiresRationale, string? RepairPath);

public sealed record EvidenceRequirementResponse(
    string ClaimKind, string ClaimName, string Requirement, string Status, string Owner,
    string ScopeId, string ScopePath);

public sealed record ProjectRecommendationsResponse(
    string ProjectId, string ProjectName, long Revision, string RuleVersion,
    PurposeProfileResponse Profile, string? RecentChangeKind, long? RecentChangeRevision,
    string PrimaryRecommendationId, IReadOnlyList<RecommendationCandidateResponse> Candidates);

public sealed record RecommendationCandidateResponse(
    string Id, int Rank, string Stage, string Title, string ActionLabel, string Path,
    string Status, string Priority, string Rationale, IReadOnlyList<string> FindingCodes,
    IReadOnlyList<string> Dependencies, IReadOnlyList<RecommendationSignalResponse> Signals);

public sealed record RecommendationSignalResponse(string Kind, string Label, string Value, string Explanation);

public sealed record ProjectWorkshopResponse(
    string ProjectId, string ProjectName, string Purpose, string IntendedOutcome, long Revision,
    string ProfileId, string ProfileName, string BriefVersion, string PrimaryRecommendation,
    IReadOnlyList<WorkshopParticipantResponse> Participants,
    IReadOnlyList<WorkshopAgendaItemResponse> Agenda,
    IReadOnlyList<WorkshopFocusResponse> FocusItems);

public sealed record WorkshopParticipantResponse(string Id, string Name, string Role, string Contribution);
public sealed record WorkshopAgendaItemResponse(
    string Id, int Order, string Phase, string Title, string IntendedResult, int Minutes,
    string Status, string SourceLabel, string SourcePath);
public sealed record WorkshopFocusResponse(string Kind, string Code, string Title, string Severity, string Path);

public sealed record LensProjectionResponse(
    string ProjectionId, string ContractVersion, string Lens, LensScopeResponse Scope,
    LensFilterResponse Filter, string ContentHash, IReadOnlyList<LensNodeResponse> Nodes,
    IReadOnlyList<LensEdgeResponse> Edges, IReadOnlyList<LensDiagnosticResponse> Diagnostics,
    IReadOnlyList<LensAccessibilityItemResponse> AccessibilityTree);

public sealed record LensScopeResponse(string ProjectId, long Revision, string RootId, string RootKind, string RootName);
public sealed record LensFilterResponse(
    IReadOnlyList<string> Kinds, IReadOnlyList<string> Statuses, string? Text,
    IReadOnlyList<string>? Overlays = null);
public sealed record LensNodeResponse(
    string Id, string SemanticId, string Kind, string Title, string Subtitle, string Status,
    string Group, int Order, IReadOnlyList<LensPortResponse> Ports, IReadOnlyList<string> Badges,
    IReadOnlyList<LensInspectorSectionResponse> Inspector);
public sealed record LensPortResponse(string Id, string Direction, string Label, IReadOnlyList<string> RelationKinds);
public sealed record LensEdgeResponse(
    string Id, string SemanticRelationId, string Kind, string SourceNodeId, string SourcePortId,
    string TargetNodeId, string TargetPortId, string Label, string Pattern,
    string Origin = "semantic-relation");
public sealed record LensDiagnosticResponse(string Code, string Severity, string Message, string? SemanticId);
public sealed record LensInspectorSectionResponse(string Id, string Label, IReadOnlyList<LensInspectorFieldResponse> Fields);
public sealed record LensInspectorFieldResponse(string Key, string Label, string Value, string KnowledgeStatus);
public sealed record LensAccessibilityItemResponse(
    string NodeId, string SemanticId, string Kind, string Label, string Status,
    int Position, int SetSize, int InboundCount, int OutboundCount);

public sealed record ScenarioFlowProjectionResponse(
    string ProjectionId, string ContractVersion, ScenarioFlowScopeResponse Scope, string ContentHash,
    IReadOnlyList<ScenarioFlowLaneResponse> Lanes, IReadOnlyList<ScenarioFlowNodeResponse> Nodes,
    IReadOnlyList<ScenarioFlowEdgeResponse> Edges, IReadOnlyList<ScenarioFlowPathResponse> Paths,
    IReadOnlyList<ScenarioFlowPlaybackStepResponse> Playback,
    IReadOnlyList<ScenarioFlowOverlayResponse> Overlays,
    IReadOnlyList<LensDiagnosticResponse> Diagnostics,
    IReadOnlyList<LensAccessibilityItemResponse> AccessibilityTree);

public sealed record ScenarioFlowScopeResponse(
    string ProjectId, long Revision, string ScenarioId, string ScenarioName,
    string Classification, string EpisodeName, string SceneName, string OutcomeName);

public sealed record ScenarioFlowLaneResponse(
    string Id, string Label, string ParticipantId, string ParticipantName, string Role, int Order);

public sealed record ScenarioFlowNodeResponse(
    string Id, string SemanticReference, string Origin, string Kind, string Title, string Detail,
    string Status, string LaneId, int Order, string Shape, IReadOnlyList<string> Badges,
    IReadOnlyList<LensInspectorSectionResponse> Inspector);

public sealed record ScenarioFlowEdgeResponse(
    string Id, string Kind, string SourceNodeId, string TargetNodeId, string Label,
    string Pattern, string Origin, string PathId);

public sealed record ScenarioFlowPathResponse(
    string Id, string SemanticReference, string Classification, string Label, string Condition,
    IReadOnlyList<string> NodeIds, string TerminalResult, string Pattern, string Status);

public sealed record ScenarioFlowPlaybackStepResponse(
    int Position, int SetSize, string NodeId, string Phase, string Narration,
    string ActiveLaneId, string PathId, bool IsDecision, bool IsTerminal);

public sealed record ScenarioFlowOverlayResponse(
    string PathId, string SourceTransitionId, string TransitionName,
    string BeforeState, string AfterState, IReadOnlyList<string> ChangedFacts,
    string Observation, string InvariantId, string InvariantName, string InvariantStatement,
    string StopNodeId, string StopReason);

public sealed record StateRuleProjectionResponse(
    string ProjectionId, string ContractVersion, StateRuleScopeResponse Scope, string ContentHash,
    IReadOnlyList<string> Representations, IReadOnlyList<StateRuleNodeResponse> Nodes,
    IReadOnlyList<StateRuleEdgeResponse> Edges, IReadOnlyList<StateTransitionRowResponse> Transitions,
    IReadOnlyList<StateRuleRowResponse> Rules, IReadOnlyList<StateInvariantResponse> Invariants,
    IReadOnlyList<LensDiagnosticResponse> Diagnostics,
    IReadOnlyList<LensAccessibilityItemResponse> AccessibilityTree);

public sealed record StateRuleScopeResponse(
    string ProjectId, long Revision, string StateId, string StateName, string StateCategory,
    string OwnerId, string OwnerName, IReadOnlyList<string> Structure, IReadOnlyList<string> Values);

public sealed record StateRuleNodeResponse(
    string Id, string SemanticReference, string Origin, string Kind, string Title, string Detail,
    string KnowledgeStatus, string Column, int Order, string Shape, IReadOnlyList<string> Badges,
    IReadOnlyList<LensInspectorSectionResponse> Inspector);

public sealed record StateRuleEdgeResponse(
    string Id, string Kind, string SourceNodeId, string TargetNodeId, string Label,
    string Pattern, string Origin);

public sealed record StateTransitionRowResponse(
    string TransitionId, string Name, string SourcePredicate, string Trigger,
    IReadOnlyList<string> Rules, IReadOnlyList<string> ChangedFacts, string TargetPredicate,
    IReadOnlyList<SemanticResultResponse> Results, IReadOnlyList<string> Effects);

public sealed record StateRuleRowResponse(
    string RuleId, string Name, string Kind, string Statement, string Authority,
    IReadOnlyList<string> AppliesToTransitions, IReadOnlyList<string> ObservedFacts);

public sealed record StateInvariantResponse(
    string InvariantId, string Name, string Statement, string FalsifyingExample,
    IReadOnlyList<string> ScopeIds, IReadOnlyList<string> ProofExpectation,
    IReadOnlyList<string> CheckedByTransitions);

public sealed record ChangeSetResponse(
    string Id, long? BaseRevision, long ResultRevision, string ChangeKind,
    string Reason, string CreatedBy, string OccurredAt, int OperationCount,
    string SemanticSummary, IReadOnlyList<ChangeOperationResponse> Operations);

public sealed record ChangeOperationResponse(
    int Sequence, string Kind, string SubjectKind,
    string? ElementId, string? RelationId, string Summary);

public sealed record RelationResponse(
    string Id, string Kind, string DisplayName,
    string SourceElementId, string SourceKind, string SourceName,
    string TargetElementId, string TargetKind, string TargetName,
    string Direction, string Cardinality, bool IsUnique,
    string Ownership, string DeletionBehavior);

public sealed record DefineNarrativeRequest(
    string OutcomeId, IReadOnlyList<string> ParticipantIds, string InitiatorId, string ReceiverId,
    string EpisodeName, string EpisodeStart, string EpisodeEnd,
    string ScenarioName, string Classification, string StartingFacts, string Trigger, string ExpectedOutcome,
    string SceneName, string Setting, string Responsibility,
    string InteractionName, string Intent, string Step, string Observation,
    string SemanticResults, string Reason);

public sealed record DefineNarrativeResponse(
    NarrativeResponse Narrative, long Revision, string AllowedNextAction);

public sealed record NarrativeResponse(
    string EpisodeId, string EpisodeName, string Start, string End, string OutcomeName, string ScenarioId,
    string ScenarioName, string Classification, IReadOnlyList<string> StartingFacts,
    string Trigger, string ExpectedOutcome, string SceneName, string Setting,
    string Responsibility, string InteractionName, string InitiatorName, string ReceiverName,
    string Intent, string Step, string Observation, IReadOnlyList<string> SemanticResults,
    string OutcomeId = "", string SceneId = "", string InitiatorId = "", string ReceiverId = "",
    string InteractionId = "", string IntentId = "", string StepId = "", string ObservationId = "");

public sealed record DefineStateLogicRequest(
    string StateName, string StateCategory, string StateStructure, string StateValues, string OwnerId,
    string FactName, string FactValueType, string FactAuthority, string FactMutability,
    string RuleName, string RuleKind, string RuleStatement, string RuleAuthorityOwnerId,
    string InvariantName, string InvariantStatement, string FalsifyingExample, string ProofExpectation,
    string SemanticResults, string TransitionName, string SourcePredicate, string Trigger,
    string TargetPredicate, string Reason);

public sealed record DefineStateLogicResponse(
    StateLogicResponse Definitions, long Revision, string AllowedNextAction);

public sealed record SemanticResultResponse(string Id, string Name, string Kind, string Meaning);

public sealed record StateLogicResponse(
    string StateId, string StateName, string StateCategory, IReadOnlyList<string> Structure,
    IReadOnlyList<string> Values, string OwnerName, string FactId, string FactName, string FactValueType,
    string FactAuthority, string FactMutability, string RuleId, string RuleName, string RuleKind, string RuleStatement,
    string InvariantName, string InvariantStatement, string FalsifyingExample,
    IReadOnlyList<string> ProofExpectation, IReadOnlyList<SemanticResultResponse> Results,
    string TransitionId, string TransitionName, string SourcePredicate, string Trigger, string TargetPredicate,
    string OwnerId = "", IReadOnlyList<string>? FactAllowedKnowledge = null,
    string RuleAuthorityOwnerId = "", string InvariantId = "", IReadOnlyList<string>? InvariantScopeIds = null,
    IReadOnlyList<string>? ChangedFactIds = null, IReadOnlyList<string>? RuleIds = null,
    IReadOnlyList<string>? InvariantIds = null, IReadOnlyList<string>? ResultIds = null,
    string RuleAuthorityOwnerName = "");

public sealed record DefinePathRequest(
    string ScenarioId, string SourceTransitionId, string TerminalResultId, string RecoveryResultId,
    string OwnerId, string BranchName, string BranchClassification,
    string BranchConditionName, string BranchConditionKind, string BranchCondition,
    string BranchFactIds, string BranchRuleIds, string BranchSegments,
    string BranchTerminalState, string BranchObservation,
    string EffectName, string EffectKind, string EffectStatement,
    string RecoveryName, string RecoveryStrategy, string RecoveryConditionName, string RecoveryCondition,
    string RecoverySegments, string RecoveryTerminalState, string RecoveryObservation,
    string RetryPolicy, string IdempotencyAnalysis, string ExitCondition, string Reconciliation,
    string Reason);

public sealed record PathResponse(
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

public sealed record DefinePathResponse(PathResponse Path, long Revision, string AllowedNextAction);

public sealed record DefineSystemContextRequest(
    string OwnedSystemName, string OwnedSystemPurpose, string OwnedSystemOwnerId, string OwnedResponsibilities,
    string ExternalSystemName, string ExternalSystemPurpose, string ExternalSystemOwnerId,
    string ExternalResponsibilities, string ExternalKnowledgeStatus,
    string InterfaceName, string InterfaceDescription, string InterfaceKind, IReadOnlyList<string> ParticipantIds,
    string AcceptedIntents, string Observations, string AccessibilityConstraints,
    string BoundaryName, string BoundaryDescription, IReadOnlyList<string> BoundaryKinds,
    IReadOnlyList<string> BoundaryOwnerIds, string BoundaryKnowledgeStatus, string? CrossingEffectId,
    string ContractName, string ContractDescription, string ContractKind, string ContractVersion,
    string ContractOwnerId, string SchemaReference, string CompatibilityPolicy,
    string RequestData, string ResponseData, string DataClassification, string ContractKnowledgeStatus,
    string Reason);

public sealed record DefineSystemContextResponse(SystemContextResponse Context, long Revision, string AllowedNextAction);

public sealed record SystemContextResponse(
    string OwnedSystemId, string OwnedSystemName, string OwnedSystemPurpose, string OwnedSystemOwnerId,
    string OwnedSystemOwnerName, IReadOnlyList<string> OwnedResponsibilities,
    string ExternalSystemId, string ExternalSystemName, string ExternalSystemPurpose, string ExternalSystemOwnerId,
    string ExternalSystemOwnerName, IReadOnlyList<string> ExternalResponsibilities, string ExternalKnowledgeStatus,
    string InterfaceId, string InterfaceName, string InterfaceDescription, string InterfaceKind,
    IReadOnlyList<string> ParticipantIds, IReadOnlyList<string> ParticipantNames,
    IReadOnlyList<string> AcceptedIntents, IReadOnlyList<string> Observations, IReadOnlyList<string> AccessibilityConstraints,
    string BoundaryId, string BoundaryName, string BoundaryDescription, IReadOnlyList<string> BoundaryKinds,
    IReadOnlyList<string> BoundaryOwnerIds, IReadOnlyList<string> BoundaryOwnerNames,
    string BoundaryKnowledgeStatus, string? CrossingEffectId, string? CrossingEffectName,
    string ContractId, string ContractName, string ContractDescription, string ContractKind,
    string ContractVersion, string ContractOwnerId, string ContractOwnerName, string SchemaReference,
    string CompatibilityPolicy, string RequestData, string ResponseData, string DataClassification,
    string ContractKnowledgeStatus);

public sealed record SystemContextProjectionResponse(
    string ProjectionId, string ContractVersion, SystemContextScopeResponse Scope, string Overlay,
    string ContentHash, IReadOnlyList<SystemContextNodeResponse> Nodes,
    IReadOnlyList<SystemContextConnectionResponse> Connections, SystemBoundaryResponse Boundary,
    IReadOnlyList<SystemDataFlowResponse> DataFlows, IReadOnlyList<LensDiagnosticResponse> Diagnostics,
    IReadOnlyList<LensAccessibilityItemResponse> AccessibilityTree);
public sealed record SystemContextScopeResponse(string ProjectId, long Revision, string OwnedSystemId, string OwnedSystemName);
public sealed record SystemContextNodeResponse(string Id, string SemanticReference, string Origin, string Kind,
    string Title, string Detail, string Zone, int Order, IReadOnlyList<string> Badges,
    IReadOnlyList<LensInspectorSectionResponse> Inspector);
public sealed record SystemContextConnectionResponse(string Id, string Kind, string SourceNodeId,
    string TargetNodeId, string Label, string Pattern, string Origin);
public sealed record SystemBoundaryResponse(string Id, string SemanticReference, string Name,
    IReadOnlyList<string> Kinds, string SourceSystemId, string TargetSystemId,
    IReadOnlyList<string> OwnerNames, string KnowledgeStatus, string? CrossingEffectId, string? CrossingEffectName);
public sealed record SystemDataFlowResponse(string Id, string Direction, string SourceNodeId,
    string TargetNodeId, string Data, string Classification, string ContractId, string Origin);

public sealed record DefineEvidencePacketRequest(
    string ClaimKind, string ClaimStatement, string ClaimStatus, IReadOnlyList<string> ElementIds,
    string OwnerId, string Tags, string EvidenceKind, string EvidenceStatus, string Producer,
    string Environment, string Summary, string Limitations, string Reason);
public sealed record DefineEvidencePacketResponse(ClaimResponse Claim, EvidenceResponse Evidence,
    long Revision, string AllowedNextAction);
public sealed record ClaimResponse(string Id, string Kind, string Statement, string Status,
    IReadOnlyList<string> ElementIds, string OwnerId, string OwnerName, IReadOnlyList<string> Tags,
    string CreatedAt, string CreatedBy);
public sealed record EvidenceResponse(string Id, string Kind, string Status, string ClaimId,
    string Producer, string ProducedAt, long ModelRevision, string Environment, string Summary,
    IReadOnlyList<string> Limitations, string CreatedBy);
public sealed record TraceabilityResponse(IReadOnlyList<ClaimResponse> Claims, IReadOnlyList<EvidenceResponse> Evidence);

public sealed record TraceabilityProjectionResponse(
    string ProjectionId, string ContractVersion, TraceabilityScopeResponse Scope, string View,
    string ContentHash, IReadOnlyList<TraceNodeResponse> Nodes, IReadOnlyList<TraceEdgeResponse> Edges,
    IReadOnlyList<OutcomeTraceResponse> OutcomeTraces, IReadOnlyList<MissingTraceResponse> MissingLinks,
    IReadOnlyList<ImpactTraceResponse> Impact, IReadOnlyList<LensDiagnosticResponse> Diagnostics,
    IReadOnlyList<LensAccessibilityItemResponse> AccessibilityTree);
public sealed record TraceabilityScopeResponse(string ProjectId, string ProjectName, long Revision, string PurposeProfile);
public sealed record TraceNodeResponse(string Id, string SemanticReference, string Origin, string Kind,
    string Title, string Detail, string Status, string Lane, int Order, IReadOnlyList<string> Badges,
    IReadOnlyList<LensInspectorSectionResponse> Inspector);
public sealed record TraceEdgeResponse(string Id, string Kind, string SourceNodeId, string TargetNodeId,
    string Label, string Pattern, string Origin);
public sealed record OutcomeTraceResponse(string OutcomeId, string OutcomeName, string Status,
    IReadOnlyList<string> ClaimIds, IReadOnlyList<string> EvidenceIds, string Explanation);
public sealed record MissingTraceResponse(string Code, string Severity, string ScopeId, string ScopeName,
    string MissingLink, string RepairPath, string Explanation);
public sealed record ImpactTraceResponse(string ScopeId, string ScopeName, long ChangedAtRevision,
    IReadOnlyList<string> ClaimIds, IReadOnlyList<string> EvidenceIds, string Status, string Reason);

public sealed record CanvasViewportRequest(double X, double Y, double Zoom);
public sealed record CanvasNodePlacementRequest(
    string ElementId, double X, double Y, double Width, double Height, bool Collapsed);
public sealed record CanvasLayoutRequest(
    CanvasViewportRequest Viewport, string Alignment,
    IReadOnlyList<CanvasNodePlacementRequest> Nodes, string InputHash);
public sealed record SaveCanvasViewRequest(
    string Name, string Lens, string ScopeKey, string Visibility,
    long ModelRevision, long ExpectedLayoutVersion, CanvasLayoutRequest Layout);
public sealed record ResetCanvasViewRequest(
    string Lens, string ScopeKey, string Visibility, long ExpectedLayoutVersion);
public sealed record CanvasViewResponse(
    string Id, string ProjectId, string Name, string Lens, string ScopeKey,
    string Visibility, string OwnerKey, long ModelRevision, long LayoutVersion,
    CanvasLayoutRequest Layout, string UpdatedAt, string UpdatedBy, bool IsStale,
    long SemanticRevision);
