using ProjectBuilder.Application.Modeling;
using ProjectBuilder.Application.Modeling.GetProjectModel;
using ProjectBuilder.Application.Traceability;
using ProjectBuilder.Domain.Modeling.Primitives;

namespace ProjectBuilder.Application.Guidance.GetProjectGuidance;

public sealed record GuidancePromptOverview(
    string Id, int Version, string Stage, int Order, string Question, string WhyThisMatters,
    string LearningContent, string TriggerExplanation, IReadOnlyList<string> RelatedFactKinds,
    IReadOnlyList<string> Examples, IReadOnlyList<GuidanceAnswerOverview> AnswerMappings,
    string PrimaryRepairPath);

public sealed record GuidanceAnswerOverview(
    string Key, string Label, string Kind, string ResultingChange, bool RequiresRationale, string? RepairPath);

public sealed record GuidanceStageOverview(
    string Id, string Name, string Status, int ApplicablePromptCount, string Explanation);

public sealed record ProjectGuidanceOverview(
    string ProjectId, string ProjectName, long Revision, string RegistryVersion,
    IReadOnlyList<GuidanceStageOverview> Stages, IReadOnlyList<GuidancePromptOverview> Prompts);

public abstract record GetProjectGuidanceResult
{
    private GetProjectGuidanceResult() { }
    public sealed record Found(ProjectGuidanceOverview Guidance) : GetProjectGuidanceResult;
    public sealed record Invalid(string Code, string Message) : GetProjectGuidanceResult;
    public sealed record NotFound : GetProjectGuidanceResult;
}

public sealed class GetProjectGuidanceHandler(
    GetProjectModelHandler models,
    PromptRegistry registry,
    ITraceabilityStore traceability)
{
    public async ValueTask<GetProjectGuidanceResult> HandleAsync(string projectId, CancellationToken cancellationToken = default)
    {
        var result = await models.HandleAsync(projectId, cancellationToken);
        if (result is GetProjectModelResult.Invalid invalid) return new GetProjectGuidanceResult.Invalid(invalid.Error.Code, invalid.Error.Message);
        if (result is GetProjectModelResult.NotFound) return new GetProjectGuidanceResult.NotFound();
        var model = ((GetProjectModelResult.Found)result).Model;
        var parsedProjectId = (SemanticResult<ProjectId>.Accepted)ProjectId.Parse(model.Project.Id);
        var traceabilitySnapshot = await traceability.LoadTraceabilityAsync(parsedProjectId.Value, cancellationToken);
        var facts = Facts(model, traceabilitySnapshot.Evidence.Length > 0);
        var applicable = registry.Applicable(facts);
        var prompts = applicable.Select(prompt => new GuidancePromptOverview(
            prompt.Id, prompt.Version, prompt.Stage.ToString(), prompt.Order, prompt.Question,
            prompt.WhyThisMatters, prompt.LearningContent, prompt.TriggerExplanation,
            prompt.RelatedFactKinds, prompt.Examples,
            prompt.AnswerMappings.Select(answer => new GuidanceAnswerOverview(
                answer.Key, answer.Label, AnswerKind(answer.Kind), answer.ResultingChange,
                answer.RequiresRationale, Route(answer.RepairCommand, projectId))).ToArray(),
            Route(prompt.PrimaryRepairCommand, projectId)!)).ToArray();
        var stages = Enum.GetValues<GuidanceStage>().Select(stage => Stage(stage, facts,
            prompts.Count(prompt => prompt.Stage == stage.ToString()))).ToArray();
        return new GetProjectGuidanceResult.Found(new(
            model.Project.Id, model.Project.Name, model.Project.Revision, "builtin/1", stages, prompts));
    }

    private static Dictionary<GuidanceFact, bool> Facts(ProjectModelOverview model, bool hasEvidenceArtifacts) =>
        new Dictionary<GuidanceFact, bool>
        {
            [GuidanceFact.HasActors] = model.Actors.Count > 0,
            [GuidanceFact.HasOutcomes] = model.Outcomes.Count > 0,
            [GuidanceFact.HasNarratives] = model.Narratives.Count > 0,
            [GuidanceFact.HasStateLogic] = model.StateLogic.Count > 0,
            [GuidanceFact.HasPaths] = model.Paths.Count > 0,
            [GuidanceFact.HasEvidenceArtifacts] = hasEvidenceArtifacts,
        };

    private static GuidanceStageOverview Stage(GuidanceStage stage, Dictionary<GuidanceFact, bool> facts, int prompts)
    {
        var established = stage switch
        {
            GuidanceStage.Frame => facts[GuidanceFact.HasOutcomes],
            GuidanceStage.Participants => facts[GuidanceFact.HasActors],
            GuidanceStage.Behavior => facts[GuidanceFact.HasNarratives],
            GuidanceStage.State => facts[GuidanceFact.HasStateLogic],
            GuidanceStage.Recovery => facts[GuidanceFact.HasPaths],
            GuidanceStage.Evidence => facts[GuidanceFact.HasEvidenceArtifacts],
            _ => false,
        };
        var status = prompts > 0 ? "Prompted" : established ? "Established" : "Waiting";
        var explanation = status switch
        {
            "Prompted" => $"{prompts} deterministic prompt{(prompts == 1 ? " is" : "s are")} applicable to current facts.",
            "Established" => "Current canonical facts establish this stage.",
            _ => "A prerequisite stage must be established before guidance becomes applicable.",
        };
        return new(stage.ToString().ToLowerInvariant(), stage.ToString(), status, prompts, explanation);
    }

    private static string? Route(string? template, string projectId) => template?.Replace("{projectId}", projectId, StringComparison.Ordinal);
    private static string AnswerKind(GuidanceAnswerKind kind) => kind == GuidanceAnswerKind.NotApplicable ? "Not applicable" : kind.ToString();
}
