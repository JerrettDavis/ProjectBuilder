using System.Collections.Immutable;
using System.Globalization;
using ProjectBuilder.Domain.Modeling.Elements;
using ProjectBuilder.Domain.Modeling.Primitives;

namespace ProjectBuilder.Domain.Modeling.Traceability;

public enum ClaimKind { Behavior, Invariant, Property, Contract, Accessibility, Security, Performance, Operational, Compliance, Other }
public enum ClaimStatus { Draft, Required, AcceptedRisk, Deprecated }
public enum EvidenceKind { ExampleTest, PropertyTest, StateTransitionTest, IntegrationTest, ContractTest, ComponentTest, EndToEndTest, AccessibilityReview, SecurityReview, PerformanceTest, ResilienceExperiment, OperationalRehearsal, Source, ManualReview, Other }
public enum EvidenceStatus { Planned, Produced, Passed, Failed, Stale, Superseded, AcceptedRisk }

public sealed record ClaimId
{
    private ClaimId(Guid value) => Value = value;
    public Guid Value { get; }
    public static ClaimId From(ElementId id) => new(id.Value);
    public override string ToString() => Value.ToString("D", CultureInfo.InvariantCulture);
}

public sealed record EvidenceId
{
    private EvidenceId(Guid value) => Value = value;
    public Guid Value { get; }
    public static EvidenceId From(ElementId id) => new(id.Value);
    public override string ToString() => Value.ToString("D", CultureInfo.InvariantCulture);
}

public sealed record ClaimDefinition(
    ClaimId Id, ProjectId ProjectId, ClaimKind Kind, LogicStatement Statement, ClaimStatus Status,
    ImmutableArray<ElementId> ElementIds, EvidenceId EvidenceId, ElementId OwnerId,
    ImmutableArray<LogicTerm> Tags, UtcTimestamp CreatedAt, string CreatedBy);

public sealed record EvidenceDefinition(
    EvidenceId Id, ProjectId ProjectId, EvidenceKind Kind, EvidenceStatus Status, ClaimId ClaimId,
    LogicTerm Producer, UtcTimestamp ProducedAt, Revision ModelRevision, LogicTerm Environment,
    LogicStatement Summary, ImmutableArray<LogicTerm> Limitations, UtcTimestamp CreatedAt, string CreatedBy);
