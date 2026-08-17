using System.Globalization;
using System.Text.RegularExpressions;
using ProjectBuilder.Domain.Modeling.Primitives;

namespace ProjectBuilder.Domain.Modeling.Gaps;

public enum GapDispositionKind
{
    Assumed,
    Deferred,
    AcceptedRisk,
    NotApplicable,
}

public sealed record GapDispositionId
{
    private GapDispositionId(Guid value) => Value = value;
    public Guid Value { get; }
    public static GapDispositionId From(ElementId id) => new(id.Value);
    public override string ToString() => Value.ToString("D", CultureInfo.InvariantCulture);
}

public sealed record GapDispositionDefinition
{
    internal GapDispositionDefinition(
        GapDispositionId id, ProjectId projectId, string profileId, string ruleCode, ElementId scopeId,
        GapDispositionKind disposition, Description rationale, Description consequence,
        ElementId authorityActorId, string? reviewOn, string? targetMilestone,
        UtcTimestamp createdAt, string createdBy)
    {
        Id = id;
        ProjectId = projectId;
        ProfileId = profileId;
        RuleCode = ruleCode;
        ScopeId = scopeId;
        Disposition = disposition;
        Rationale = rationale;
        Consequence = consequence;
        AuthorityActorId = authorityActorId;
        ReviewOn = reviewOn;
        TargetMilestone = targetMilestone;
        CreatedAt = createdAt;
        CreatedBy = createdBy;
    }

    public GapDispositionId Id { get; }
    public ProjectId ProjectId { get; }
    public string ProfileId { get; }
    public string RuleCode { get; }
    public ElementId ScopeId { get; }
    public GapDispositionKind Disposition { get; }
    public Description Rationale { get; }
    public Description Consequence { get; }
    public ElementId AuthorityActorId { get; }
    public string? ReviewOn { get; }
    public string? TargetMilestone { get; }
    public UtcTimestamp CreatedAt { get; }
    public string CreatedBy { get; }

    internal static bool IsValidRuleCode(string? value) =>
        value is not null && Regex.IsMatch(value, "^PB-[A-Z]+-[0-9]{3}$", RegexOptions.CultureInvariant);

    internal static bool IsValidProfile(string? value) => value is "discovery" or "implementation-ready";
    internal static bool IsValidDate(string? value) => value is null || DateOnly.TryParseExact(
        value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _);
}
