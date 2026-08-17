using ProjectBuilder.Application.Modeling;
using ProjectBuilder.Application.Modeling.GetProjectModel;
using ProjectBuilder.Application.Validation.GetProjectFindings;
using ProjectBuilder.Application.Validation.GetProjectRecommendations;

namespace ProjectBuilder.Application.Collaboration.GetProjectWorkshop;

public sealed record WorkshopParticipantOverview(string Id, string Name, string Role, string Contribution);
public sealed record WorkshopAgendaItemOverview(
    string Id, int Order, string Phase, string Title, string IntendedResult, int Minutes,
    string Status, string SourceLabel, string SourcePath);
public sealed record WorkshopFocusOverview(string Kind, string Code, string Title, string Severity, string Path);
public sealed record ProjectWorkshopOverview(
    string ProjectId, string ProjectName, string Purpose, string IntendedOutcome, long Revision,
    string ProfileId, string ProfileName, string BriefVersion, string PrimaryRecommendation,
    IReadOnlyList<WorkshopParticipantOverview> Participants,
    IReadOnlyList<WorkshopAgendaItemOverview> Agenda,
    IReadOnlyList<WorkshopFocusOverview> FocusItems);

public abstract record GetProjectWorkshopResult
{
    private GetProjectWorkshopResult() { }
    public sealed record Found(ProjectWorkshopOverview Workshop) : GetProjectWorkshopResult;
    public sealed record Invalid(string Code, string Message) : GetProjectWorkshopResult;
    public sealed record NotFound : GetProjectWorkshopResult;
}

public sealed class GetProjectWorkshopHandler(GetProjectModelHandler models)
{
    public const string BriefVersion = "workshop/1";

    public async ValueTask<GetProjectWorkshopResult> HandleAsync(
        string projectId, string profile = "discovery", CancellationToken cancellationToken = default)
    {
        if (profile is not ("discovery" or "implementation-ready"))
            return new GetProjectWorkshopResult.Invalid(
                "purpose-profile.invalid", "Purpose profile must be discovery or implementation-ready.");

        return await models.HandleAsync(projectId, cancellationToken) switch
        {
            GetProjectModelResult.Found found => new GetProjectWorkshopResult.Found(Evaluate(found.Model, profile)),
            GetProjectModelResult.Invalid invalid => new GetProjectWorkshopResult.Invalid(invalid.Error.Code, invalid.Error.Message),
            GetProjectModelResult.NotFound => new GetProjectWorkshopResult.NotFound(),
            _ => throw new InvalidOperationException("Unknown project model result."),
        };
    }

    public static ProjectWorkshopOverview Evaluate(ProjectModelOverview model, string profile = "discovery")
    {
        var findings = GetProjectFindingsHandler.Evaluate(model, profile);
        var recommendations = GetProjectRecommendationsHandler.Evaluate(model, profile);
        var primary = recommendations.Candidates.Single(item => item.Id == recommendations.PrimaryRecommendationId);
        var projectPath = $"/projects/{model.Project.Id}";
        var participants = model.Actors.OrderBy(item => item.Name, StringComparer.Ordinal)
            .ThenBy(item => item.Id, StringComparer.Ordinal)
            .Select(item => new WorkshopParticipantOverview(item.Id, item.Name, item.ContextualRole,
                item.Goals.Count == 0 ? "Contribution not yet modeled" : item.Goals[0]))
            .ToArray();
        var agenda = new[]
        {
            Item("workshop.align", 1, "Frame", "Align on the outcome", model.Project.IntendedOutcome, 8, "Ready", "Canonical purpose", projectPath),
            Item("workshop.voices", 2, "Context", "Hear the modeled participants", "Confirm who acts, benefits, decides, and supplies authority.", 10,
                participants.Length == 0 ? "Blocked" : "Ready", $"{participants.Length} modeled participant(s)", $"{projectPath}#actors-heading"),
            Item("workshop.behavior", 3, "Walkthrough", "Walk one ordinary scenario", "Trace starting facts, trigger, interaction, semantic result, and observation.", 15,
                model.Narratives.Count == 0 ? "Needs definition" : "Ready", $"{model.Narratives.Count} scenario packet(s)", $"{projectPath}#narratives-heading"),
            Item("workshop.tensions", 4, "Challenge", "Examine material tensions", "Make blockers, uncertainty, assumptions, and evidence debt discussable.", 15,
                findings.Findings.Count == 0 ? "Clear" : "Ready", $"{findings.Findings.Count} current finding(s)", $"{projectPath}/problems?profile={profile}"),
            Item("workshop.decide", 5, "Decide", "Choose the next coherent slice", primary.Title, 10, "Ready", primary.Id, $"{projectPath}/recommendations?profile={profile}"),
            Item("workshop.close", 6, "Close", "Confirm owners and unresolved questions", "Export the provisional record and identify what must become canonical next.", 7, "Ready", $"Revision {model.Project.Revision}", $"{projectPath}/history"),
        };
        var focus = findings.Findings.OrderBy(item => Severity(item.Severity)).ThenBy(item => item.Code, StringComparer.Ordinal)
            .Take(5).Select(item => new WorkshopFocusOverview("Finding", item.Code, item.Title, item.Severity, item.RepairPath)).ToList();
        focus.Insert(0, new("Recommendation", primary.Id, primary.Title, primary.Priority, primary.Path));
        return new(model.Project.Id, model.Project.Name, model.Project.Purpose, model.Project.IntendedOutcome,
            model.Project.Revision, findings.Profile.Id, findings.Profile.Name, BriefVersion, primary.Title,
            participants, agenda, focus);
    }

    private static WorkshopAgendaItemOverview Item(
        string id, int order, string phase, string title, string result, int minutes,
        string status, string source, string path) =>
        new(id, order, phase, title, result, minutes, status, source, path);

    private static int Severity(string severity) => severity switch
    {
        "Blocker" => 0,
        "Error" => 1,
        "Warning" => 2,
        "Info" => 3,
        _ => 4,
    };
}
