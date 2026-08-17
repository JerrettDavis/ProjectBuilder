using System.Collections.Immutable;
using ProjectBuilder.Application.Modeling;
using ProjectBuilder.Domain.Modeling.Primitives;
using ProjectBuilder.Domain.Modeling.Traceability;
using ProjectBuilder.Domain.Modeling.Transitions;

namespace ProjectBuilder.Application.Traceability;

public sealed record ClaimOverview(string Id, string Kind, string Statement, string Status,
    IReadOnlyList<string> ElementIds, string OwnerId, string OwnerName, IReadOnlyList<string> Tags,
    string CreatedAt, string CreatedBy);
public sealed record EvidenceOverview(string Id, string Kind, string Status, string ClaimId, string Producer,
    string ProducedAt, long ModelRevision, string Environment, string Summary,
    IReadOnlyList<string> Limitations, string CreatedBy);
public sealed record TraceabilitySnapshot(ImmutableArray<ClaimOverview> Claims, ImmutableArray<EvidenceOverview> Evidence);

public interface ITraceabilityStore
{
    ValueTask<ElementStoreCommitResult> CommitEvidencePacketAsync(
        DefineEvidencePacketTransitionResult.Accepted commit, string requestFingerprint, CancellationToken cancellationToken);
    ValueTask<TraceabilitySnapshot> LoadTraceabilityAsync(ProjectId projectId, CancellationToken cancellationToken);
    ValueTask<(ClaimOverview Claim, EvidenceOverview Evidence)?> FindEvidencePacketAsync(
        ProjectId projectId, ClaimId claimId, CancellationToken cancellationToken);
}
