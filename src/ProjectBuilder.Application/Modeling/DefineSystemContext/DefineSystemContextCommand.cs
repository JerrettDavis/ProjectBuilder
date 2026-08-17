namespace ProjectBuilder.Application.Modeling.DefineSystemContext;

public sealed record DefineSystemContextCommand(
    string ProjectId, string ExpectedRevision, string OperationId,
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
