namespace ProjectBuilder.Application.Traceability.DefineEvidencePacket;

public sealed record DefineEvidencePacketCommand(
    string ProjectId, string ExpectedRevision, string OperationId,
    string ClaimKind, string ClaimStatement, string ClaimStatus, IReadOnlyList<string> ElementIds,
    string OwnerId, string Tags, string EvidenceKind, string EvidenceStatus, string Producer,
    string Environment, string Summary, string Limitations, string Reason);
