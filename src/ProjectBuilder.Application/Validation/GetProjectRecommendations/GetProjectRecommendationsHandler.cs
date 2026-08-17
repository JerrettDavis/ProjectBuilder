using ProjectBuilder.Application.Modeling;
using ProjectBuilder.Application.Modeling.GetProjectModel;
using ProjectBuilder.Application.Validation.GetProjectFindings;

namespace ProjectBuilder.Application.Validation.GetProjectRecommendations;

public sealed record RecommendationSignalOverview(string Kind, string Label, string Value, string Explanation);
public sealed record RecommendationCandidateOverview(
    string Id, int Rank, string Stage, string Title, string ActionLabel, string Path,
    string Status, string Priority, string Rationale, IReadOnlyList<string> FindingCodes,
    IReadOnlyList<string> Dependencies, IReadOnlyList<RecommendationSignalOverview> Signals);
public sealed record ProjectRecommendationsOverview(
    string ProjectId, string ProjectName, long Revision, string RuleVersion,
    PurposeProfileOverview Profile, string? RecentChangeKind, long? RecentChangeRevision,
    string PrimaryRecommendationId, IReadOnlyList<RecommendationCandidateOverview> Candidates);

public abstract record GetProjectRecommendationsResult
{
    private GetProjectRecommendationsResult() { }
    public sealed record Found(ProjectRecommendationsOverview Overview) : GetProjectRecommendationsResult;
    public sealed record Invalid(string Code, string Message) : GetProjectRecommendationsResult;
    public sealed record NotFound : GetProjectRecommendationsResult;
}

public sealed class GetProjectRecommendationsHandler(GetProjectModelHandler models)
{
    public const string RuleVersion = "builtin/1";

    public async ValueTask<GetProjectRecommendationsResult> HandleAsync(
        string projectId, string profile = "discovery", CancellationToken cancellationToken = default)
    {
        if (profile is not ("discovery" or "implementation-ready"))
            return new GetProjectRecommendationsResult.Invalid(
                "purpose-profile.invalid", "Purpose profile must be discovery or implementation-ready.");

        return await models.HandleAsync(projectId, cancellationToken) switch
        {
            GetProjectModelResult.Found found => new GetProjectRecommendationsResult.Found(Evaluate(found.Model, profile)),
            GetProjectModelResult.Invalid invalid => new GetProjectRecommendationsResult.Invalid(invalid.Error.Code, invalid.Error.Message),
            GetProjectModelResult.NotFound => new GetProjectRecommendationsResult.NotFound(),
            _ => throw new InvalidOperationException("Unknown project model result."),
        };
    }

    public static ProjectRecommendationsOverview Evaluate(ProjectModelOverview model, string profile = "discovery")
    {
        if (profile is not ("discovery" or "implementation-ready"))
            throw new ArgumentException("Purpose profile must be discovery or implementation-ready.", nameof(profile));

        var findings = GetProjectFindingsHandler.Evaluate(model, profile);
        var recent = model.ChangeSets.OrderByDescending(item => item.ResultRevision).FirstOrDefault();
        var specifications = Specifications(model, findings, profile, recent?.ChangeKind);
        var incomplete = specifications.Where(item => !item.Complete).ToArray();
        var ordered = incomplete
            .OrderBy(item => item.DependenciesReady ? 0 : 1)
            .ThenBy(item => item.Required ? 0 : 1)
            .ThenBy(item => item.SeverityOrder)
            .ThenByDescending(item => item.RecentContinuation)
            .ThenBy(item => item.Order)
            .ThenBy(item => item.Id, StringComparer.Ordinal)
            .ToArray();
        var primary = ordered.FirstOrDefault(item => item.DependenciesReady) ?? specifications.Single(item => item.Id == "recommend.review");

        var ranked = ordered.Select((item, index) => Candidate(item, index + 1, item.Id == primary.Id, model.Project.Revision, profile, recent)).ToList();
        if (incomplete.Length == 0)
        {
            var review = specifications.Single(item => item.Id == "recommend.review");
            ranked.Add(Candidate(review, 1, true, model.Project.Revision, profile, recent));
        }

        return new(model.Project.Id, model.Project.Name, model.Project.Revision, RuleVersion, findings.Profile,
            recent?.ChangeKind, recent?.ResultRevision, primary.Id, ranked);
    }

    private static RecommendationCandidateOverview Candidate(
        RecommendationSpecification item, int rank, bool primary, long revision, string profile, ChangeSetOverview? recent)
    {
        var status = primary ? "Recommended" : item.DependenciesReady ? "Available" : "Blocked";
        var priority = item.Required ? "Required for profile" : "Advisory for profile";
        var findings = item.Findings.Select(finding => finding.Code).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
        var signals = new List<RecommendationSignalOverview>
        {
            new("purpose", "Purpose pressure", item.Required ? "Required" : "Advisory",
                item.Required ? $"{Label(profile)} requires this dimension." : $"{Label(profile)} keeps this visible without making it a gate."),
            new("finding", "Finding pressure", findings.Length == 0 ? "No direct finding" : string.Join(" · ", findings),
                item.Findings.Count == 0 ? "The action follows dependency order rather than a direct finding." : $"{item.Findings.Count} deterministic finding{(item.Findings.Count == 1 ? "" : "s")} point here."),
            new("dependency", "Dependency gate", item.DependenciesReady ? "Ready" : "Blocked",
                item.DependenciesReady ? "Every semantic prerequisite exists at this revision." : $"First establish: {string.Join(", ", item.Dependencies)}."),
            new("recent", "Recent-work continuity", item.RecentContinuation ? "Aligned" : recent?.ChangeKind ?? "No prior change",
                item.RecentContinuation ? $"The latest {recent!.ChangeKind} change naturally opens this next question." : "Recent work does not override purpose, severity, or dependency gates."),
        };
        return new(item.Id, rank, item.Stage, item.Title, item.ActionLabel, item.Path, status, priority,
            $"At revision {revision}, {item.Rationale}", findings, item.Dependencies, signals);
    }

    private static IReadOnlyList<RecommendationSpecification> Specifications(
        ProjectModelOverview model, ProjectFindingsOverview findings, string profile, string? recentChangeKind)
    {
        var implementationReady = profile == "implementation-ready";
        return
        [
            Spec("recommend.participant", 10, "Context", "Identify the first actor", "Add an actor",
                $"/projects/{model.Project.Id}/actors/new", model.Actors.Count > 0, true, true, [], "the project needs a participant who can act, decide, receive value, or supply authority.",
                findings, ["Context", "Knowledge"], recentChangeKind, "project.created"),
            Spec("recommend.outcome", 20, "Outcome", "Make success observable", "Define an outcome",
                $"/projects/{model.Project.Id}/outcomes/new", model.Outcomes.Count > 0, true, model.Actors.Count > 0, ["an accountable participant"], "an existing participant must be connected to observable value and success signals.",
                findings, ["Outcome"], recentChangeKind, "actor.added", "actor.updated"),
            Spec("recommend.behavior", 30, "Behavior", "Define the complete scenario", "Compose scenario",
                $"/projects/{model.Project.Id}/guide", model.Narratives.Count > 0, true, model.Actors.Count > 0 && model.Outcomes.Count > 0, ["an accountable participant", "a beneficiary-linked outcome"], "starting facts, trigger, interaction, result, and observation must explain how value is obtained.",
                findings, ["Behavior"], recentChangeKind, "outcome.added", "outcome.updated"),
            Spec("recommend.state", 40, "State", "Make facts, rules, and results explicit", "Define state and logic",
                $"/projects/{model.Project.Id}/state-logic/new", model.StateLogic.Count > 0, implementationReady, model.Actors.Count > 0 && model.Narratives.Count > 0, ["an accountable participant", "a complete scenario"], "the modeled behavior needs authoritative facts, rules, invariant, transition, and semantic results.",
                findings, ["State"], recentChangeKind, "narrative.defined"),
            Spec("recommend.paths", 50, "Recovery", "Close the most material unmodeled result", "Model branch and recovery",
                $"/projects/{model.Project.Id}/paths/new", model.Paths.Count > 0 && findings.Findings.All(item => item.Code != "PB-PATH-008"), implementationReady,
                model.Narratives.Count > 0 && model.StateLogic.Count > 0, ["a complete scenario", "state logic with semantic results"], "each material non-success result needs a participant-visible terminal state and owned recovery.",
                findings, ["Path"], recentChangeKind, "state-logic.defined"),
            Spec("recommend.evidence", 60, "Evidence", "Plan proof for the material invariant", "Review evidence debt",
                $"/projects/{model.Project.Id}/problems?view=evidence&profile={profile}", findings.EvidenceRequirements.Count == 0 || findings.EvidenceRequirements.All(item => item.Status is "Passing" or "Not applicable"), implementationReady,
                model.StateLogic.Count > 0, ["an explicit invariant and proof expectation"], "the current invariant claims need proportionate, attributable evidence rather than a test-count proxy.",
                findings, ["Evidence"], recentChangeKind, "path.defined"),
            new("recommend.review", 70, "Review", "Inspect the current revision trail", "Review history",
                $"/projects/{model.Project.Id}/history", false, false, true, [], "the supported semantic packets are present; inspect authored operations before choosing another outcome.", [], 4, recentChangeKind == "path.defined"),
        ];
    }

    private static RecommendationSpecification Spec(
        string id, int order, string stage, string title, string action, string path, bool complete, bool required,
        bool dependenciesReady, IReadOnlyList<string> dependencies, string rationale, ProjectFindingsOverview overview,
        IReadOnlyList<string> categories, string? recent, params string[] continuationKinds)
    {
        var matching = overview.Findings.Where(item => categories.Contains(item.Category, StringComparer.Ordinal)).ToArray();
        return new(id, order, stage, title, action, path, complete, required, dependenciesReady, dependencies, rationale,
            matching, matching.Select(item => Severity(item.Severity)).DefaultIfEmpty(4).Min(), continuationKinds.Contains(recent, StringComparer.Ordinal));
    }

    private static int Severity(string severity) => severity switch { "Blocker" => 0, "Error" => 1, "Warning" => 2, "Info" => 3, _ => 4 };
    private static string Label(string value) => value == "implementation-ready" ? "Implementation Ready" : "Discovery";

    private sealed record RecommendationSpecification(
        string Id, int Order, string Stage, string Title, string ActionLabel, string Path,
        bool Complete, bool Required, bool DependenciesReady, IReadOnlyList<string> Dependencies,
        string Rationale, IReadOnlyList<ProjectFindingOverview> Findings, int SeverityOrder, bool RecentContinuation);
}
