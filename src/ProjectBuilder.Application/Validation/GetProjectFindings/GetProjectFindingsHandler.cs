using ProjectBuilder.Application.Modeling;
using ProjectBuilder.Application.Modeling.GetProjectModel;

namespace ProjectBuilder.Application.Validation.GetProjectFindings;

public sealed record ProjectFindingOverview(
    string Code, string Severity, string Status, string Category, string Title, string Explanation,
    string Rule, string ScopeId, string ScopeKind, string ScopeName, string Owner,
    string RepairLabel, string RepairPath, bool RepairAvailable,
    string? DispositionId = null, string? DispositionRationale = null, string? DispositionConsequence = null,
    string? AuthorityActorId = null, string? AuthorityName = null, string? ReviewOn = null, string? TargetMilestone = null);

public sealed record EvidenceRequirementOverview(
    string ClaimKind, string ClaimName, string Requirement, string Status, string Owner, string ScopeId, string ScopePath);

public sealed record PurposeProfileOverview(string Id, string Name, string Description);
public sealed record GapAuthorityOverview(string Id, string Name, string ContextualRole);
public sealed record CoverageDimensionOverview(
    string Id, string Name, string Status, bool Required, int FindingCount, string Explanation, string RepairPath);
public sealed record ProfilePredicateOverview(string Code, string Name, bool Satisfied, string Explanation);

public sealed record ProjectFindingsOverview(
    string ProjectId, string ProjectName, long Revision, PurposeProfileOverview Profile,
    IReadOnlyList<PurposeProfileOverview> AvailableProfiles,
    IReadOnlyList<CoverageDimensionOverview> Coverage,
    IReadOnlyList<ProfilePredicateOverview> Predicates,
    IReadOnlyList<ProjectFindingOverview> Findings,
    IReadOnlyList<EvidenceRequirementOverview> EvidenceRequirements,
    IReadOnlyList<GapAuthorityOverview> Authorities);

public abstract record GetProjectFindingsResult
{
    private GetProjectFindingsResult() { }
    public sealed record Found(ProjectFindingsOverview Overview) : GetProjectFindingsResult;
    public sealed record Invalid(string Code, string Message) : GetProjectFindingsResult;
    public sealed record NotFound : GetProjectFindingsResult;
}

public sealed class GetProjectFindingsHandler(GetProjectModelHandler models)
{
    public async ValueTask<GetProjectFindingsResult> HandleAsync(string projectId, string profile = "discovery", CancellationToken cancellationToken = default)
    {
        if (!Profiles.Any(item => item.Id == profile))
            return new GetProjectFindingsResult.Invalid("purpose-profile.invalid", "Purpose profile must be discovery or implementation-ready.");
        var result = await models.HandleAsync(projectId, cancellationToken);
        return result switch
        {
            GetProjectModelResult.Found found => new GetProjectFindingsResult.Found(Evaluate(found.Model, profile)),
            GetProjectModelResult.Invalid invalid => new GetProjectFindingsResult.Invalid(invalid.Error.Code, invalid.Error.Message),
            GetProjectModelResult.NotFound => new GetProjectFindingsResult.NotFound(),
            _ => throw new InvalidOperationException("Unknown project model result."),
        };
    }

    public static ProjectFindingsOverview Evaluate(ProjectModelOverview model, string profile = "discovery")
    {
        var selectedProfile = Profiles.Single(item => item.Id == profile);
        var implementationReady = profile == "implementation-ready";
        var projectId = model.Project.Id;
        var findings = new List<ProjectFindingOverview>();
        var evidence = new List<EvidenceRequirementOverview>();
        var defaultOwner = model.Actors.Count > 0 ? model.Actors[0].Name : "Unassigned";
        var overview = $"/projects/{projectId}";

        if (model.Actors.Count == 0)
            findings.Add(Finding("PB-CONTEXT-001", "Blocker", "Context", "No participant can own modeled behavior",
                "At least one modeled actor is required before outcomes, state authority, or human-observable paths can be defined.",
                "A project with behavior must identify a participant or accountable system role.", projectId, "Project", model.Project.Name,
                "Unassigned", "Add an actor", $"{overview}/actors/new"));

        if (model.Outcomes.Count == 0)
            findings.Add(Finding("PB-OUTCOME-001", "Error", "Outcome", "No observable outcome is defined",
                "The intended outcome is project intent, but no beneficiary-linked Outcome element makes success reviewable.",
                "A modeled project requires an observable outcome with a beneficiary and success signals.", projectId, "Project", model.Project.Name,
                defaultOwner, model.Actors.Count == 0 ? "Add prerequisite actor" : "Define an outcome",
                model.Actors.Count == 0 ? $"{overview}/actors/new" : $"{overview}/outcomes/new"));

        if (model.Narratives.Count == 0)
            findings.Add(Finding("PB-NARR-001", implementationReady ? "Error" : "Warning", "Behavior", "No complete scenario narrative is defined",
                "The model does not yet show how participants interact to produce an outcome.",
                "At least one Episode-to-Observation packet must reference an outcome and participants.", projectId, "Project", model.Project.Name,
                defaultOwner, "Compose a scenario", $"{overview}/narratives/new", model.Actors.Count >= 2 && model.Outcomes.Count > 0));

        if (model.StateLogic.Count == 0)
            findings.Add(Finding("PB-STATE-011", implementationReady ? "Error" : "Info", "State", "Behavior has no explicit state and logic model",
                "State facts, authority, governing rules, invariants, results, and transition predicates are not modeled.",
                "State-changing behavior must distinguish its state category and explicit semantic results.", projectId, "Project", model.Project.Name,
                defaultOwner, "Define state and logic", $"{overview}/state-logic/new", model.Actors.Count > 0));

        foreach (var actor in model.Actors.Where(value => value.KnowledgeStatus != "known"))
            findings.Add(Finding("PB-KNOW-001", "Warning", "Knowledge", $"{actor.Name} has {Label(actor.KnowledgeStatus)} knowledge status",
                "The uncertainty is explicit and remains material until its basis is reviewed.",
                "Non-Known model meaning must remain visible with reviewable provenance.", actor.Id, "Actor", actor.Name,
                actor.Name, "Review actor meaning", $"{overview}/actors/{actor.Id}/edit"));

        foreach (var outcome in model.Outcomes)
        {
            if (outcome.KnowledgeStatus != "known")
                findings.Add(Finding("PB-KNOW-002", "Warning", "Knowledge", $"{outcome.Name} has {Label(outcome.KnowledgeStatus)} knowledge status",
                    "The observable outcome is explicitly uncertain and must not be treated as verified intent.",
                    "Non-Known model meaning must remain visible with reviewable provenance.", outcome.Id, "Outcome", outcome.Name,
                    outcome.BeneficiaryName, "Review outcome meaning", $"{overview}/outcomes/{outcome.Id}/edit"));
            evidence.Add(new("Outcome", outcome.Name, $"Observe: {string.Join("; ", outcome.SuccessSignals)}", "Unknown",
                outcome.BeneficiaryName, outcome.Id, $"{overview}?selected={outcome.Id}"));
        }

        foreach (var state in model.StateLogic)
        {
            var modeledResultNames = model.Paths.SelectMany(path => new[] { path.TerminalResultName, path.RecoveryResultName })
                .ToHashSet(StringComparer.Ordinal);
            foreach (var semanticResult in state.Results.Where(value => !modeledResultNames.Contains(value.Name)))
                findings.Add(Finding("PB-PATH-008", implementationReady ? "Error" : "Info", "Path", $"{semanticResult.Name} has no modeled path",
                    $"The {semanticResult.Kind} result is explicit, but no branch or recovery path describes its participant observation and terminal state.",
                    "Every material non-success result requires an observable path or an explicit Not Applicable decision.", semanticResult.Id,
                    "Semantic result", semanticResult.Name, state.OwnerName, "Model branch and recovery", $"{overview}/paths/new",
                    model.Narratives.Count > 0));

            evidence.Add(new("Invariant", state.InvariantName,
                $"Prove: {string.Join("; ", state.ProofExpectation)}", "Required", state.OwnerName,
                state.StateId, $"{overview}?selected={state.StateId}#state-heading"));
            findings.Add(Finding("PB-EVID-001", implementationReady ? "Error" : "Info", "Evidence", $"{state.InvariantName} has no evidence artifact",
                "Proof expectations are modeled, but the runtime query exposes no evidence artifact linked to this invariant.",
                "Every material invariant requires attributable, current evidence appropriate to its proof expectation.", state.StateId,
                "Invariant", state.InvariantName, state.OwnerName, "Review evidence requirement", $"{overview}/problems?view=evidence", false));
        }

        if (model.StateLogic.Count == 0)
            evidence.Add(new("Project outcome", model.Project.IntendedOutcome,
                "Decide what would persuade a reviewer that this outcome is true.", "Unknown", defaultOwner,
                projectId, $"{overview}/problems?view=evidence"));

        var governedFindings = findings.Select(finding =>
        {
            var disposition = model.GapDispositions?.SingleOrDefault(item =>
                item.ProfileId == profile && item.RuleCode == finding.Code && item.ScopeId == finding.ScopeId);
            return disposition is null ? finding : finding with
            {
                Status = LabelDisposition(disposition.Disposition),
                DispositionId = disposition.Id,
                DispositionRationale = disposition.Rationale,
                DispositionConsequence = disposition.Consequence,
                AuthorityActorId = disposition.AuthorityActorId,
                AuthorityName = disposition.AuthorityName,
                ReviewOn = disposition.ReviewOn,
                TargetMilestone = disposition.TargetMilestone,
            };
        });
        var orderedFindings = governedFindings.OrderBy(item => SeverityOrder(item.Severity)).ThenBy(item => item.Code, StringComparer.Ordinal)
            .ThenBy(item => item.ScopeName, StringComparer.Ordinal).ToArray();
        var coverage = Coverage(model, orderedFindings, implementationReady, overview);
        var predicates = Predicates(model, orderedFindings, implementationReady);
        return new(projectId, model.Project.Name, model.Project.Revision, selectedProfile, Profiles, coverage, predicates, orderedFindings,
            evidence.OrderBy(item => item.ClaimKind, StringComparer.Ordinal).ThenBy(item => item.ClaimName, StringComparer.Ordinal).ToArray(),
            model.Actors.Select(actor => new GapAuthorityOverview(actor.Id, actor.Name, actor.ContextualRole)).ToArray());
    }

    private static CoverageDimensionOverview[] Coverage(ProjectModelOverview model, IReadOnlyList<ProjectFindingOverview> findings, bool implementationReady, string overview)
    {
        var specifications = new[]
        {
            ("purpose", "Purpose", true, true, $"{overview}"),
            ("participants", "Participants", true, true, $"{overview}/actors/new"),
            ("behavior", "Behavior", true, true, $"{overview}/narratives/new"),
            ("state", "State", false, true, $"{overview}/state-logic/new"),
            ("paths", "Paths", false, true, $"{overview}/paths/new"),
            ("evidence", "Evidence", false, true, $"{overview}/problems?view=evidence")
        };
        return specifications.Select(item =>
        {
            var required = item.Item3 || implementationReady && item.Item4;
            var categories = item.Item1 switch { "participants" => ParticipantCategories, "behavior" => BehaviorCategories, _ => [string.Concat(item.Item1[..1].ToUpperInvariant(), item.Item1.AsSpan(1))] };
            var directCount = findings.Count(finding => categories.Contains(finding.Category, StringComparer.Ordinal) && (required || finding.Severity != "Info"));
            var missingPrerequisite = item.Item1 switch
            {
                "participants" => model.Actors.Count == 0,
                "behavior" => model.Outcomes.Count == 0 || model.Narratives.Count == 0,
                "state" => model.StateLogic.Count == 0,
                "paths" => model.Narratives.Count == 0 || model.StateLogic.Count == 0,
                "evidence" => model.StateLogic.Count == 0,
                _ => false
            };
            var count = required && missingPrerequisite ? Math.Max(1, directCount) : directCount;
            var status = count == 0 ? (required ? "Defined" : "Not required") : required ? "Gap" : "Advisory";
            var explanation = status switch { "Defined" => "Current facts satisfy this profile dimension.", "Not required" => "This dimension is visible but does not gate the selected purpose.", "Advisory" => "Material for later work, but not a gate for this purpose.", _ when missingPrerequisite && directCount == 0 => "A prerequisite definition is missing; this dimension cannot yet be established.", _ => $"{count} deterministic finding{(count == 1 ? "" : "s")} must be addressed for this purpose." };
            return new CoverageDimensionOverview(item.Item1, item.Item2, status, required, count, explanation, item.Item5);
        }).ToArray();
    }

    private static IReadOnlyList<ProfilePredicateOverview> Predicates(ProjectModelOverview model, IReadOnlyList<ProjectFindingOverview> findings, bool implementationReady) =>
    [
        new("profile.intent", "Purpose and intended outcome are explicit", !string.IsNullOrWhiteSpace(model.Project.Purpose) && !string.IsNullOrWhiteSpace(model.Project.IntendedOutcome), "Project purpose and intended outcome are canonical project facts."),
        new("profile.participants", "At least one accountable participant exists", model.Actors.Count > 0, "Participants supply beneficiary, authority, and observation ownership."),
        new("profile.behavior", "An observable outcome and complete narrative exist", model.Outcomes.Count > 0 && model.Narratives.Count > 0, "Behavior must connect participant intent to an observable outcome."),
        new("profile.state", "State, rules, invariants, and results are explicit", !implementationReady || model.StateLogic.Count > 0, implementationReady ? "Implementation Ready requires explicit state-changing semantics." : "Discovery keeps state visible as a later refinement rather than a gate."),
        new("profile.paths", "Material results have modeled paths", !implementationReady || model.Narratives.Count > 0 && model.StateLogic.Count > 0 && !findings.Any(item => item.Code == "PB-PATH-008"), implementationReady ? "Implementation Ready requires behavior and state before observable path closure can be established." : "Discovery may carry explicit path gaps forward."),
        new("profile.evidence", "Material invariants have evidence", !implementationReady || model.StateLogic.Count > 0 && !findings.Any(item => item.Code == "PB-EVID-001"), implementationReady ? "Implementation Ready requires explicit invariants and a proof plan; linked runtime artifacts remain a known product gap." : "Discovery records evidence debt without treating it as a gate.")
    ];

    private static readonly PurposeProfileOverview[] Profiles =
    [
        new("discovery", "Discovery", "Clarify intent, participants, observable outcomes, major behavior, uncertainty, and material gaps."),
        new("implementation-ready", "Implementation Ready", "Require explicit behavior, state, rules, paths, and evidence expectations for a bounded implementation slice.")
    ];
    private static readonly string[] ParticipantCategories = ["Context", "Knowledge"];
    private static readonly string[] BehaviorCategories = ["Outcome", "Behavior"];

    private static ProjectFindingOverview Finding(string code, string severity, string category, string title,
        string explanation, string rule, string scopeId, string scopeKind, string scopeName, string owner,
        string repairLabel, string repairPath, bool repairAvailable = true) =>
        new(code, severity, "Open", category, title, explanation, rule, scopeId, scopeKind, scopeName, owner,
            repairLabel, repairPath, repairAvailable);

    private static int SeverityOrder(string severity) => severity switch { "Blocker" => 0, "Error" => 1, "Warning" => 2, "Info" => 3, _ => 4 };
    private static string Label(string value) => value.Length == 0 ? value : string.Concat(value[..1].ToUpperInvariant(), value.AsSpan(1));
    private static string LabelDisposition(string value) => value switch
    {
        "AcceptedRisk" => "Accepted risk",
        "NotApplicable" => "Not applicable",
        _ => Label(value),
    };
}
