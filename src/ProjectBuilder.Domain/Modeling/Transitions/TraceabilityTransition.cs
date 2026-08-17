using System.Collections.Immutable;
using ProjectBuilder.Domain.Modeling.Elements;
using ProjectBuilder.Domain.Modeling.Primitives;
using ProjectBuilder.Domain.Modeling.Traceability;
using ProjectBuilder.Domain.Projects;

namespace ProjectBuilder.Domain.Modeling.Transitions;

public sealed record EvidencePacketDraft(
    ClaimKind ClaimKind, LogicStatement ClaimStatement, ClaimStatus ClaimStatus,
    ImmutableArray<ElementId> ElementIds, ElementId OwnerId, ImmutableArray<LogicTerm> Tags,
    EvidenceKind EvidenceKind, EvidenceStatus EvidenceStatus, LogicTerm Producer,
    LogicTerm Environment, LogicStatement Summary, ImmutableArray<LogicTerm> Limitations);

public sealed record EvidencePacket(ClaimDefinition Claim, EvidenceDefinition Evidence);

public abstract record DefineEvidencePacketTransitionResult
{
    private DefineEvidencePacketTransitionResult() { }
    public sealed record Accepted(ProjectDefinition Project, EvidencePacket Packet, ProjectModelChangeSet ChangeSet) : DefineEvidencePacketTransitionResult;
    public sealed record Conflict(Revision Expected, Revision Actual, ImmutableArray<SemanticConflict> Conflicts) : DefineEvidencePacketTransitionResult;
    public sealed record Invalid(IReadOnlyList<SemanticError> Errors) : DefineEvidencePacketTransitionResult;
}

public static class TraceabilityTransition
{
    public static DefineEvidencePacketTransitionResult Define(ProjectDefinition project, Revision expectedRevision,
        ClaimId claimId, EvidenceId evidenceId, EvidencePacketDraft draft, ChangeSetId operationId,
        ChangeReason reason, UtcTimestamp occurredAt, string createdBy)
    {
        if (project.Revision != expectedRevision)
            return new DefineEvidencePacketTransitionResult.Conflict(expectedRevision, project.Revision,
                ProjectChangeSetTransition.RevisionConflicts(expectedRevision, project.Revision));
        if (draft.ElementIds.IsDefaultOrEmpty)
            return new DefineEvidencePacketTransitionResult.Invalid([new("PB-EVID-001", "A material claim must link at least one semantic definition.")]);
        if (draft.EvidenceStatus is EvidenceStatus.Passed or EvidenceStatus.Failed && draft.Summary.Value.Equals("Unknown", StringComparison.OrdinalIgnoreCase))
            return new DefineEvidencePacketTransitionResult.Invalid([new("PB-EVID-004", "Passing or failing evidence requires an explicit result summary.")]);
        var nextRevision = ((SemanticResult<Revision>.Accepted)project.Revision.Next()).Value;
        var claim = new ClaimDefinition(claimId, project.Id, draft.ClaimKind, draft.ClaimStatement,
            draft.ClaimStatus, draft.ElementIds, evidenceId, draft.OwnerId, draft.Tags, occurredAt, createdBy);
        var evidence = new EvidenceDefinition(evidenceId, project.Id, draft.EvidenceKind, draft.EvidenceStatus,
            claimId, draft.Producer, occurredAt, nextRevision, draft.Environment, draft.Summary,
            draft.Limitations, occurredAt, createdBy);
        ImmutableArray<ProjectChangeOperation> operations =
        [
            new ProjectChangeOperation.ClaimAdded(0, claimId, draft.ElementIds[0], draft.ClaimKind),
            new ProjectChangeOperation.EvidenceAdded(1, evidenceId, claimId, draft.EvidenceKind),
        ];
        var committed = ProjectChangeSetTransition.Commit(project, expectedRevision,
            new(operationId, draft.ElementIds[0], "evidence-packet.defined", reason, operations), occurredAt, createdBy);
        var accepted = (ProjectChangeSetTransitionResult.Accepted)committed;
        return new DefineEvidencePacketTransitionResult.Accepted(accepted.Project, new(claim, evidence), accepted.ChangeSet);
    }
}
