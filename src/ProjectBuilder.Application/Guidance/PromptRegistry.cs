using System.Collections.Immutable;
using System.Text.RegularExpressions;

namespace ProjectBuilder.Application.Guidance;

public sealed class PromptRegistry
{
    private static readonly Regex IdPattern = new("^guide\\.[a-z]+\\.[a-z0-9-]+$", RegexOptions.CultureInvariant);
    private readonly ImmutableArray<PromptDescriptor> prompts;

    public PromptRegistry() : this(BuiltInPrompts) { }

    internal PromptRegistry(IEnumerable<PromptDescriptor> descriptors)
    {
        prompts = descriptors.OrderBy(item => item.Stage).ThenBy(item => item.Order).ThenBy(item => item.Id, StringComparer.Ordinal).ToImmutableArray();
        Findings = Validate(prompts);
        if (Findings.Length > 0)
            throw new InvalidOperationException($"The guidance prompt registry is invalid: {string.Join("; ", Findings.Select(item => $"{item.Code}:{item.PromptId}"))}");
    }

    public ImmutableArray<PromptRegistryFinding> Findings { get; }
    public ImmutableArray<PromptDescriptor> All => prompts;

    public ImmutableArray<PromptDescriptor> Applicable(IReadOnlyDictionary<GuidanceFact, bool> facts) =>
        prompts.Where(prompt => prompt.AppliesWhen.All(rule => facts.GetValueOrDefault(rule.Fact) == rule.Expected)).ToImmutableArray();

    public static ImmutableArray<PromptRegistryFinding> Validate(IEnumerable<PromptDescriptor> descriptors)
    {
        var source = descriptors.ToArray();
        var findings = ImmutableArray.CreateBuilder<PromptRegistryFinding>();
        foreach (var duplicate in source.GroupBy(item => item.Id, StringComparer.Ordinal).Where(group => group.Count() > 1))
            findings.Add(new("GUIDE-REG-001", duplicate.Key, "Prompt identifiers must be unique."));
        foreach (var prompt in source)
        {
            if (!IdPattern.IsMatch(prompt.Id)) findings.Add(new("GUIDE-REG-002", prompt.Id, "Prompt identifier must use guide.<stage>.<name>."));
            if (prompt.Version < 1) findings.Add(new("GUIDE-REG-003", prompt.Id, "Prompt version must be positive."));
            if (prompt.Order < 1) findings.Add(new("GUIDE-REG-004", prompt.Id, "Prompt order must be positive."));
            if (string.IsNullOrWhiteSpace(prompt.Question) || string.IsNullOrWhiteSpace(prompt.WhyThisMatters) ||
                string.IsNullOrWhiteSpace(prompt.LearningContent) || string.IsNullOrWhiteSpace(prompt.TriggerExplanation))
                findings.Add(new("GUIDE-REG-005", prompt.Id, "Question, rationale, learning content, and trigger explanation are required."));
            if (prompt.AppliesWhen.GroupBy(item => item.Fact).Any(group => group.Select(item => item.Expected).Distinct().Count() > 1))
                findings.Add(new("GUIDE-REG-006", prompt.Id, "Prompt applicability is unreachable because the same fact must be both true and false."));
            if (prompt.AnswerMappings.Length == 0 || prompt.AnswerMappings.Select(item => item.Kind).Distinct().Count() != prompt.AnswerMappings.Length)
                findings.Add(new("GUIDE-REG-007", prompt.Id, "Answer mappings must be present and have unique semantic kinds."));
            if (prompt.AnswerMappings.Any(item => string.IsNullOrWhiteSpace(item.Key) || string.IsNullOrWhiteSpace(item.Label) || string.IsNullOrWhiteSpace(item.ResultingChange)))
                findings.Add(new("GUIDE-REG-008", prompt.Id, "Every answer mapping requires a key, label, and resulting change."));
            if (string.IsNullOrWhiteSpace(prompt.PrimaryRepairCommand) || !prompt.PrimaryRepairCommand.StartsWith("/projects/{projectId}", StringComparison.Ordinal))
                findings.Add(new("GUIDE-REG-009", prompt.Id, "Primary repair command must be a project-relative route template."));
        }
        return findings.OrderBy(item => item.Code, StringComparer.Ordinal).ThenBy(item => item.PromptId, StringComparer.Ordinal).ToImmutableArray();
    }

    private static PromptAnswerMapping[] Answers(string change, string repair) =>
    [
        new("author", "Author definition", GuidanceAnswerKind.Author, change, false, repair),
        new("unknown", "Unknown", GuidanceAnswerKind.Unknown, "Record explicit unknown knowledge status.", true, null),
        new("assumed", "Assumed", GuidanceAnswerKind.Assumed, "Record a governed assumption with authority and review date.", true, null),
        new("deferred", "Deferred", GuidanceAnswerKind.Deferred, "Record a governed deferral with consequence, review date, and milestone.", true, null),
        new("not-applicable", "Not applicable", GuidanceAnswerKind.NotApplicable, "Record a governed not-applicable rationale.", true, null),
    ];

    private static PromptDescriptor Prompt(string id, GuidanceStage stage, int order, string question, string why,
        string learning, string trigger, PromptApplicability[] applies, string[] related, string[] examples,
        string repair, string change) => new(id, 1, stage, order, question, why, learning, trigger,
            [.. applies], [.. related], [.. examples], [.. Answers(change, repair)], repair);

    private static readonly ImmutableArray<PromptDescriptor> BuiltInPrompts =
    [
        Prompt("guide.frame.observable-outcome", GuidanceStage.Frame, 10, "Who receives value, and what would they observe when this works?",
            "Project intent is not reviewable until success belongs to a beneficiary and has observable signals.",
            "An Outcome describes changed reality for a beneficiary; it is not a feature list or delivery task.",
            "No beneficiary-linked Outcome exists in the current canonical model.", [new(GuidanceFact.HasOutcomes, false)],
            ["Project", "Outcome", "Actor"], ["A contributor can run and verify the repository from a clean clone."],
            "/projects/{projectId}/outcomes/new", "Create a typed Outcome and benefitsFrom relation."),
        Prompt("guide.participants.accountable-actor", GuidanceStage.Participants, 10, "Who can act, decide, or authoritatively answer questions in this situation?",
            "Behavior, authority, observations, and recovery need accountable participants.",
            "Actors include people, roles, systems, devices, organizations, and time authorities—not database users.",
            "No Actor exists in the current canonical model.", [new(GuidanceFact.HasActors, false)],
            ["Project", "Actor"], ["Modeler", "Reviewer", "Price authority"],
            "/projects/{projectId}/actors/new", "Create a typed Actor with role, goals, responsibility, authority, and constraints."),
        Prompt("guide.behavior.coherent-scenario", GuidanceStage.Behavior, 10, "Describe one end-to-end situation in which a participant obtains the outcome.",
            "A narrow scenario connects intent, interaction, result, and observation before architecture is selected.",
            "Begin with one ordinary example. Episode, Scenario, Scene, and Interaction add structure only where responsibility or setting changes.",
            "Actors and an Outcome exist, but no complete narrative packet exists.", [new(GuidanceFact.HasActors, true), new(GuidanceFact.HasOutcomes, true), new(GuidanceFact.HasNarratives, false)],
            ["Actor", "Outcome", "Episode", "Scenario"], ["A contributor builds and runs a clean clone while required services are available."],
            "/projects/{projectId}/narratives/new", "Create an Episode-to-Observation narrative packet."),
        Prompt("guide.state.explicit-logic", GuidanceStage.State, 10, "Which facts, rules, invariants, and semantic results govern this behavior?",
            "A scenario explains experience; explicit state and rules explain why each result is valid.",
            "Separate domain truth from workflow and presentation state. Name denial, conflict, invalidity, and unavailability distinctly.",
            "A narrative exists, but no typed state-and-logic packet exists.", [new(GuidanceFact.HasNarratives, true), new(GuidanceFact.HasStateLogic, false)],
            ["Scenario", "State", "Fact", "Rule", "Invariant", "Result", "Transition"], ["Accepted creation advances revision exactly once."],
            "/projects/{projectId}/state-logic/new", "Create typed state, fact, rule, invariant, result, and transition definitions."),
        Prompt("guide.recovery.material-paths", GuidanceStage.Recovery, 10, "What must the participant observe and do for each non-success result?",
            "A named failure is incomplete until its terminal state, participant observation, recovery, and exit condition are explicit.",
            "Recovery is owned behavior. Retry, fallback, reconciliation, and escalation have different semantics.",
            "State logic exists, but no branch-and-recovery path packet exists.", [new(GuidanceFact.HasStateLogic, true), new(GuidanceFact.HasPaths, false)],
            ["Semantic result", "Path", "Condition", "Effect", "Recovery"], ["Conflict → refresh current revision → preserve draft → retry."],
            "/projects/{projectId}/paths/new", "Create a typed condition-to-branch-to-effect-to-recovery packet."),
        Prompt("guide.evidence.material-proof", GuidanceStage.Evidence, 10, "What evidence would persuade a reviewer that the modeled invariant is true?",
            "Definitions are claims; material invariants need attributable proof appropriate to their scope.",
            "Examples explain known behavior, properties explore invariants, contracts prove boundaries, and E2E tests prove observable journeys.",
            "Material state exists, but no runtime evidence artifact linkage is exposed.", [new(GuidanceFact.HasStateLogic, true), new(GuidanceFact.HasEvidenceArtifacts, false)],
            ["Invariant", "Evidence requirement", "Evidence artifact"], ["A property test falsifies any double revision advance."],
            "/projects/{projectId}/problems?view=evidence", "Open the evidence requirement matrix; artifact attachment remains a known boundary."),
    ];
}
