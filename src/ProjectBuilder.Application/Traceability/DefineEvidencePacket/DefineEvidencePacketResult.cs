using ProjectBuilder.Application.Modeling;
using ProjectBuilder.Domain.Modeling.Primitives;

namespace ProjectBuilder.Application.Traceability.DefineEvidencePacket;

public abstract record DefineEvidencePacketResult
{
    private DefineEvidencePacketResult() { }
    public sealed record Defined(ClaimOverview Claim, EvidenceOverview Evidence, long Revision, string AllowedNextAction) : DefineEvidencePacketResult;
    public sealed record Invalid(IReadOnlyList<SemanticError> Errors) : DefineEvidencePacketResult;
    public sealed record Denied(string Reason) : DefineEvidencePacketResult;
    public sealed record ProjectNotFound(string ProjectId) : DefineEvidencePacketResult;
    public sealed record ReferenceNotFound(string Reference) : DefineEvidencePacketResult;
    public sealed record Conflict(long Expected, long Actual, IReadOnlyList<ChangeSetConflictOverview> Conflicts) : DefineEvidencePacketResult;
    public sealed record IdempotencyConflict(string OperationId) : DefineEvidencePacketResult;
}
