using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ProjectBuilder.Contracts.Projects;

namespace ProjectBuilder.Projections.Lenses;

public static class StoryMapLensProjector
{
    public const string ContractVersion = "story-map/1";
    public const string Lens = "story-map";

    public static LensProjectionResponse Project(ProjectModelResponse model, LensProjectionRequest request)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(request);
        var filter = Normalize(request);
        var diagnostics = new List<LensDiagnosticResponse>();
        var nodes = Nodes(model, filter).OrderBy(node => node.Order).ThenBy(node => node.SemanticId, StringComparer.Ordinal).ToArray();
        var candidates = nodes.ToDictionary(node => node.Id, StringComparer.Ordinal);
        var visible = nodes.Where(node => Visible(node, filter)).ToArray();
        var visibleIds = visible.Select(node => node.Id).ToHashSet(StringComparer.Ordinal);
        var edges = Edges(model, candidates, diagnostics).Where(edge => visibleIds.Contains(edge.SourceNodeId) && visibleIds.Contains(edge.TargetNodeId))
            .OrderBy(edge => edge.Kind, StringComparer.Ordinal).ThenBy(edge => edge.Id, StringComparer.Ordinal).ToArray();
        var allEdgeCount = Edges(model, candidates, []).Count;
        if (allEdgeCount > edges.Length) diagnostics.Add(new("story-map.filter.edge-suppressed", "Info",
            $"{allEdgeCount - edges.Length} connector(s) are hidden because an endpoint does not match the active filter.", null));
        if ((model.Capabilities ?? []).Count == 0) diagnostics.Add(new("story-map.capability.missing", "Warning",
            "No modeled Capability connects outcomes to narrative behavior. Define an ability rather than inferring one from workflow names.", model.Project.Id));
        if (model.Narratives.Count == 0) diagnostics.Add(new("story-map.narrative.missing", "Info",
            "No Episode-to-Scene narrative packet is modeled yet.", model.Project.Id));
        var orderedDiagnostics = diagnostics.Distinct().OrderBy(item => item.Severity, StringComparer.Ordinal)
            .ThenBy(item => item.Code, StringComparer.Ordinal).ThenBy(item => item.SemanticId, StringComparer.Ordinal).ToArray();
        var accessibility = visible.Select((node, index) => new LensAccessibilityItemResponse(node.Id, node.SemanticId,
            node.Kind, $"{node.Kind}: {node.Title}", node.Status, index + 1, visible.Length,
            edges.Count(edge => edge.TargetNodeId == node.Id), edges.Count(edge => edge.SourceNodeId == node.Id))).ToArray();
        var scope = new LensScopeResponse(model.Project.Id, model.Project.Revision, model.Project.Id, "project", model.Project.Name);
        var hash = Hash(scope, filter, visible, edges, orderedDiagnostics, accessibility);
        var projection = new LensProjectionResponse($"story-map:{model.Project.Id}:r{model.Project.Revision}:{hash[..12]}",
            ContractVersion, Lens, scope, filter, hash, visible, edges, orderedDiagnostics, accessibility);
        LensProjectionValidator.EnsureValid(projection);
        return projection;
    }

    private static IEnumerable<LensNodeResponse> Nodes(ProjectModelResponse model, LensFilterResponse filter)
    {
        foreach (var outcome in model.Outcomes)
            yield return Node(outcome.Id, "outcome", outcome.Name, outcome.Statement, outcome.KnowledgeStatus, "Outcomes", 100,
                Overlay(filter, outcome.KnowledgeStatus, null, $"Benefits {outcome.BeneficiaryName}"),
                [Port("input", "Value received", ["benefitsFrom", "contributesTo"])],
                [Section("value", "Observable value", Field("beneficiary", "Beneficiary", outcome.BeneficiaryName),
                    Field("statement", "Outcome", outcome.Statement, outcome.KnowledgeStatus),
                    Field("signals", "Success signals", Join(outcome.SuccessSignals), outcome.KnowledgeStatus))]);
        foreach (var capability in model.Capabilities ?? [])
            yield return Node(capability.Id, "capability", capability.Name, capability.Ability, capability.KnowledgeStatus,
                "Capabilities", 200, Overlay(filter, capability.KnowledgeStatus, capability.Priority, "Ability, not workflow"),
                [Port("input", "Exercised by episodes", ["exercises"]), Port("output", "Contributes to outcomes", ["contributesTo"])],
                [Section("ability", "Capability", Field("ability", "Ability", capability.Ability, capability.KnowledgeStatus),
                    Field("priority", "Priority", capability.Priority))]);
        foreach (var narrative in model.Narratives)
        {
            yield return Node(narrative.EpisodeId, "episode", narrative.EpisodeName, narrative.End, "known", "Episodes", 300,
                Overlay(filter, "known", null, narrative.OutcomeName),
                [Port("input", "Participants and context", ["participatesIn"]), Port("output", "Exercises and contains", ["exercises", "contains"])],
                [Section("span", "Outcome-bearing span", Field("start", "Initiating situation", narrative.Start),
                    Field("end", "Completion boundary", narrative.End), Field("outcome", "Outcome", narrative.OutcomeName))]);
            yield return Node(narrative.ScenarioId, "scenario", narrative.ScenarioName, narrative.ExpectedOutcome, "known", "Scenarios", 400,
                Overlay(filter, "known", null, narrative.Classification),
                [Port("input", "Contained by episode", ["contains"]), Port("output", "Contains scenes", ["contains"])],
                [Section("path", "Concrete path", Field("classification", "Classification", narrative.Classification),
                    Field("facts", "Starting facts", Join(narrative.StartingFacts)), Field("trigger", "Trigger", narrative.Trigger))]);
            yield return Node(narrative.SceneId, "scene", narrative.SceneName, narrative.Responsibility, "known", "Scenes", 500,
                Overlay(filter, "known", null, narrative.Setting), [Port("input", "Contained by scenario", ["contains"])],
                [Section("scene", "Responsibility frame", Field("setting", "Setting", narrative.Setting),
                    Field("responsibility", "Responsibility", narrative.Responsibility))]);
        }
        var participantIds = model.Narratives.SelectMany(narrative => new[] { narrative.InitiatorId, narrative.ReceiverId })
            .Where(id => !string.IsNullOrWhiteSpace(id)).ToHashSet(StringComparer.Ordinal);
        foreach (var actor in model.Actors.Where(actor => participantIds.Contains(actor.Id) || model.Narratives.Count == 0))
            yield return Node(actor.Id, "actor", actor.Name, actor.ContextualRole, actor.KnowledgeStatus, "Participants", 600,
                Overlay(filter, actor.KnowledgeStatus, null, actor.ActorKind), [Port("output", "Participates and benefits", ["participatesIn", "benefitsFrom"])],
                [Section("participant", "Participant", Field("role", "Contextual role", actor.ContextualRole, actor.KnowledgeStatus),
                    Field("authority", "Authority", Join(actor.Authority), actor.KnowledgeStatus))]);
    }

    private static List<LensEdgeResponse> Edges(ProjectModelResponse model,
        IReadOnlyDictionary<string, LensNodeResponse> nodes, ICollection<LensDiagnosticResponse> diagnostics)
    {
        var edges = new List<LensEdgeResponse>();
        foreach (var relation in model.Relations.Where(relation => relation.Kind == "benefitsFrom"))
            Add(edges, nodes, diagnostics, $"edge:{relation.Id}", relation.Id, relation.Kind,
                relation.SourceElementId, relation.TargetElementId, relation.DisplayName, "solid-arrow");
        foreach (var capability in model.Capabilities ?? [])
            foreach (var outcomeId in capability.OutcomeIds)
                Add(edges, nodes, diagnostics, $"derived:capability-outcome:{capability.Id}:{outcomeId}",
                    $"capability:{capability.Id}:outcome:{outcomeId}", "contributesTo", capability.Id, outcomeId,
                    $"{capability.Name} contributes to {Name(nodes, outcomeId)}", "solid-arrow");
        foreach (var narrative in model.Narratives)
        {
            foreach (var capability in (model.Capabilities ?? []).Where(item => item.OutcomeIds.Contains(narrative.OutcomeId, StringComparer.Ordinal)))
                Add(edges, nodes, diagnostics, $"derived:episode-capability:{narrative.EpisodeId}:{capability.Id}",
                    $"episode:{narrative.EpisodeId}:capability:{capability.Id}", "exercises", narrative.EpisodeId, capability.Id,
                    $"{narrative.EpisodeName} exercises {capability.Name}", "dashed-arrow");
            Add(edges, nodes, diagnostics, $"derived:episode-scenario:{narrative.EpisodeId}:{narrative.ScenarioId}",
                $"containment:{narrative.EpisodeId}:{narrative.ScenarioId}", "contains", narrative.EpisodeId, narrative.ScenarioId,
                "Episode contains scenario", "solid-arrow");
            Add(edges, nodes, diagnostics, $"derived:scenario-scene:{narrative.ScenarioId}:{narrative.SceneId}",
                $"containment:{narrative.ScenarioId}:{narrative.SceneId}", "contains", narrative.ScenarioId, narrative.SceneId,
                "Scenario contains scene", "solid-arrow");
            foreach (var actorId in new[] { narrative.InitiatorId, narrative.ReceiverId }.Where(id => !string.IsNullOrWhiteSpace(id)).Distinct(StringComparer.Ordinal))
                Add(edges, nodes, diagnostics, $"derived:actor-episode:{actorId}:{narrative.EpisodeId}",
                    $"participation:{actorId}:{narrative.EpisodeId}", "participatesIn", actorId, narrative.EpisodeId,
                    $"{Name(nodes, actorId)} participates in {narrative.EpisodeName}", "dotted-arrow");
        }
        return edges;
    }

    private static void Add(List<LensEdgeResponse> edges, IReadOnlyDictionary<string, LensNodeResponse> nodes,
        ICollection<LensDiagnosticResponse> diagnostics, string id, string reference, string kind,
        string sourceId, string targetId, string label, string pattern)
    {
        var source = NodeId(sourceId); var target = NodeId(targetId);
        if (!nodes.TryGetValue(source, out var sourceNode) || !nodes.TryGetValue(target, out var targetNode))
        {
            diagnostics.Add(new("story-map.edge.endpoint-missing", "Warning", $"'{label}' cannot be shown because a typed endpoint is unavailable.", reference));
            return;
        }
        var sourcePort = sourceNode.Ports.SingleOrDefault(port => port.Direction == "output" && port.RelationKinds.Contains(kind, StringComparer.Ordinal));
        var targetPort = targetNode.Ports.SingleOrDefault(port => port.Direction == "input" && port.RelationKinds.Contains(kind, StringComparer.Ordinal));
        if (sourcePort is null || targetPort is null)
        {
            diagnostics.Add(new("story-map.edge.port-missing", "Error", $"'{label}' has no compatible directional port.", reference));
            return;
        }
        var origin = id.StartsWith("edge:", StringComparison.Ordinal) ? "semantic-relation"
            : reference.StartsWith("containment:", StringComparison.Ordinal) ? "semantic-containment"
            : "derived-explicit-reference";
        edges.Add(new(id, reference, kind, source, sourcePort.Id, target, targetPort.Id, label, pattern, origin));
    }

    private static LensNodeResponse Node(string semanticId, string kind, string title, string subtitle, string status,
        string group, int order, IReadOnlyList<string> badges, IReadOnlyList<LensPortResponse> ports,
        IReadOnlyList<LensInspectorSectionResponse> inspector) =>
        new(NodeId(semanticId), semanticId, kind, title, subtitle, status, group, order,
            ports.Select(port => port with { Id = $"{NodeId(semanticId)}:port:{port.Direction}:{port.RelationKinds[0]}" }).ToArray(),
            badges.Order(StringComparer.Ordinal).ToArray(), inspector);
    private static LensPortResponse Port(string direction, string label, IReadOnlyList<string> kinds) => new("", direction, label, kinds);
    private static LensInspectorSectionResponse Section(string id, string label, params LensInspectorFieldResponse[] fields) => new(id, label, fields);
    private static LensInspectorFieldResponse Field(string key, string label, string value, string status = "known") => new(key, label, value, status);
    private static string[] Overlay(LensFilterResponse filter, string status, string? priority, string context) =>
        new[] { filter.Overlays?.Contains("status", StringComparer.Ordinal) == true ? $"Status · {status}" : null,
            filter.Overlays?.Contains("priority", StringComparer.Ordinal) == true && priority is not null ? $"Priority · {priority}" : null, context }
            .Where(value => value is not null).Select(value => value!).ToArray();
    private static LensFilterResponse Normalize(LensProjectionRequest request) => new(
        Normalize(request.Kinds), Normalize(request.Statuses), string.IsNullOrWhiteSpace(request.Text) ? null : request.Text.Trim(),
        Normalize(request.Overlays ?? ["priority", "status"]));
    private static string[] Normalize(IEnumerable<string> values) => values.Where(value => !string.IsNullOrWhiteSpace(value))
        .Select(value => value.Trim().ToLowerInvariant()).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
    private static bool Visible(LensNodeResponse node, LensFilterResponse filter) =>
        (filter.Kinds.Count == 0 || filter.Kinds.Contains(node.Kind, StringComparer.Ordinal)) &&
        (filter.Statuses.Count == 0 || filter.Statuses.Contains(node.Status.ToLowerInvariant(), StringComparer.Ordinal)) &&
        (filter.Text is null || node.Title.Contains(filter.Text, StringComparison.OrdinalIgnoreCase) || node.Subtitle.Contains(filter.Text, StringComparison.OrdinalIgnoreCase));
    private static string NodeId(string semanticId) => $"node:{semanticId}";
    private static string Name(IReadOnlyDictionary<string, LensNodeResponse> nodes, string id) => nodes.GetValueOrDefault(NodeId(id))?.Title ?? id;
    private static string Join(IReadOnlyList<string> values) => values.Count == 0 ? "Unknown" : string.Join(" · ", values);
    private static string Hash(LensScopeResponse scope, LensFilterResponse filter, IReadOnlyList<LensNodeResponse> nodes,
        IReadOnlyList<LensEdgeResponse> edges, IReadOnlyList<LensDiagnosticResponse> diagnostics,
        IReadOnlyList<LensAccessibilityItemResponse> accessibility)
    {
        var json = JsonSerializer.Serialize(new
        {
            ContractVersion,
            Lens,
            Scope = scope,
            Filter = filter,
            Nodes = nodes,
            Edges = edges,
            Diagnostics = diagnostics,
            Accessibility = accessibility
        });
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(json)));
    }
}
