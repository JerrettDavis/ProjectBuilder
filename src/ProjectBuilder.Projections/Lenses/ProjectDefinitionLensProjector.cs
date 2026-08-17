using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ProjectBuilder.Contracts.Projects;

namespace ProjectBuilder.Projections.Lenses;

public sealed record LensProjectionRequest(
    IReadOnlyList<string> Kinds,
    IReadOnlyList<string> Statuses,
    string? Text,
    IReadOnlyList<string>? Overlays = null)
{
    public static LensProjectionRequest All { get; } = new([], [], null);
}

public static class ProjectDefinitionLensProjector
{
    public const string ContractVersion = "lens/1";
    public const string Lens = "project-definition";

    public static LensProjectionResponse Project(ProjectModelResponse model, LensProjectionRequest request)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(request);

        var filter = Normalize(request);
        var diagnostics = new List<LensDiagnosticResponse>();
        var candidates = Nodes(model).ToArray();
        var visible = candidates.Where(node => IsVisible(node, filter)).OrderBy(node => node.Order)
            .ThenBy(node => node.Kind, StringComparer.Ordinal).ThenBy(node => node.SemanticId, StringComparer.Ordinal).ToArray();
        var candidateIds = candidates.Select(node => node.Id).ToHashSet(StringComparer.Ordinal);
        var visibleIds = visible.Select(node => node.Id).ToHashSet(StringComparer.Ordinal);
        var edges = new List<LensEdgeResponse>();
        var suppressed = 0;

        foreach (var relation in model.Relations.OrderBy(item => item.Kind, StringComparer.Ordinal).ThenBy(item => item.Id, StringComparer.Ordinal))
        {
            var source = NodeId(relation.SourceElementId);
            var target = NodeId(relation.TargetElementId);
            if (!candidateIds.Contains(source) || !candidateIds.Contains(target))
            {
                diagnostics.Add(new("lens.edge.endpoint-missing", "Warning",
                    $"Relation '{relation.DisplayName}' references an endpoint outside the supported project-definition lens.", relation.Id));
                continue;
            }
            if (!visibleIds.Contains(source) || !visibleIds.Contains(target)) { suppressed++; continue; }
            edges.Add(new($"edge:{relation.Id}", relation.Id, relation.Kind, source, PortId(source, "out"),
                target, PortId(target, "in"), relation.DisplayName, EdgePattern(relation.Kind)));
        }

        if (suppressed > 0)
            diagnostics.Add(new("lens.filter.edge-suppressed", "Info",
                $"{suppressed} relation(s) are hidden because one or both endpoints do not match the active filter.", null));
        if (visible.Length == 1)
            diagnostics.Add(new("lens.scope.no-definitions", "Info", "No model definitions match the active filter; the project context remains pinned.", model.Project.Id));

        var orderedDiagnostics = diagnostics.OrderBy(item => Severity(item.Severity)).ThenBy(item => item.Code, StringComparer.Ordinal)
            .ThenBy(item => item.SemanticId, StringComparer.Ordinal).ToArray();
        var accessibility = Accessibility(visible, edges);
        var contentHash = Hash(model, filter, visible, edges, orderedDiagnostics, accessibility);
        var projection = new LensProjectionResponse($"lens:{model.Project.Id}:r{model.Project.Revision}:{contentHash[..12]}", ContractVersion, Lens,
            new(model.Project.Id, model.Project.Revision, model.Project.Id, "project", model.Project.Name), filter,
            contentHash, visible, edges, orderedDiagnostics, accessibility);
        LensProjectionValidator.EnsureValid(projection);
        return projection;
    }

    private static IEnumerable<LensNodeResponse> Nodes(ProjectModelResponse model)
    {
        yield return Node(model.Project.Id, "project", model.Project.Name, model.Project.Purpose, "defined", "Purpose", 0,
            ["Scope root", $"Revision {model.Project.Revision}"],
            [Section("identity", "Project", Field("purpose", "Purpose", model.Project.Purpose), Field("outcome", "Intended outcome", model.Project.IntendedOutcome))]);
        foreach (var actor in model.Actors)
            yield return Node(actor.Id, "actor", actor.Name, actor.ContextualRole, actor.KnowledgeStatus, "Participants", 100,
                [actor.ActorKind], [Section("identity", "Participant", Field("role", "Contextual role", actor.ContextualRole, actor.KnowledgeStatus), Field("authority", "Authority", Join(actor.Authority), actor.KnowledgeStatus))]);
        foreach (var outcome in model.Outcomes)
            yield return Node(outcome.Id, "outcome", outcome.Name, outcome.Statement, outcome.KnowledgeStatus, "Outcomes", 200,
                [$"Benefits {outcome.BeneficiaryName}"], [Section("definition", "Observable value", Field("statement", "Outcome", outcome.Statement, outcome.KnowledgeStatus), Field("signals", "Success signals", Join(outcome.SuccessSignals), outcome.KnowledgeStatus))]);
    }

    private static LensNodeResponse Node(
        string semanticId, string kind, string title, string subtitle, string status, string group, int order,
        IReadOnlyList<string> badges, IReadOnlyList<LensInspectorSectionResponse> inspector)
    {
        var id = NodeId(semanticId);
        var ports = kind switch
        {
            "actor" => new[] { new LensPortResponse(PortId(id, "out"), "output", "Benefits from", ["benefitsFrom"]) },
            "outcome" => new[] { new LensPortResponse(PortId(id, "in"), "input", "Benefits", ["benefitsFrom"]) },
            _ => [],
        };
        return new(id, semanticId, kind, title, subtitle, status, group, order, ports,
            badges.Order(StringComparer.Ordinal).ToArray(), inspector);
    }

    private static LensInspectorSectionResponse Section(string id, string label, params LensInspectorFieldResponse[] fields) =>
        new(id, label, fields.OrderBy(item => item.Key, StringComparer.Ordinal).ToArray());
    private static LensInspectorFieldResponse Field(string key, string label, string value, string status = "known") => new(key, label, value, status);
    private static string Join(IReadOnlyList<string> values) => values.Count == 0 ? "Unknown" : string.Join(" · ", values);
    private static string NodeId(string semanticId) => $"node:{semanticId}";
    private static string PortId(string nodeId, string direction) => $"{nodeId}:port:{direction}";
    private static string EdgePattern(string kind) => kind == "benefitsFrom" ? "solid-arrow" : "labeled-line";

    private static LensFilterResponse Normalize(LensProjectionRequest request) => new(
        request.Kinds.Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value.Trim().ToLowerInvariant()).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray(),
        request.Statuses.Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value.Trim().ToLowerInvariant()).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray(),
        string.IsNullOrWhiteSpace(request.Text) ? null : request.Text.Trim(),
        (request.Overlays ?? []).Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value.Trim().ToLowerInvariant())
            .Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray());

    private static bool IsVisible(LensNodeResponse node, LensFilterResponse filter)
    {
        if (node.Kind == "project") return true;
        if (filter.Kinds.Count > 0 && !filter.Kinds.Contains(node.Kind, StringComparer.Ordinal)) return false;
        if (filter.Statuses.Count > 0 && !filter.Statuses.Contains(node.Status.ToLowerInvariant(), StringComparer.Ordinal)) return false;
        return filter.Text is null || node.Title.Contains(filter.Text, StringComparison.OrdinalIgnoreCase) ||
            node.Subtitle.Contains(filter.Text, StringComparison.OrdinalIgnoreCase) || node.SemanticId.Equals(filter.Text, StringComparison.OrdinalIgnoreCase);
    }

    private static LensAccessibilityItemResponse[] Accessibility(LensNodeResponse[] nodes, IReadOnlyList<LensEdgeResponse> edges) =>
        nodes.Select((node, index) => new LensAccessibilityItemResponse(node.Id, node.SemanticId, node.Kind,
            $"{node.Kind}: {node.Title}", node.Status, index + 1, nodes.Length,
            edges.Count(edge => edge.TargetNodeId == node.Id), edges.Count(edge => edge.SourceNodeId == node.Id))).ToArray();

    private static string Hash(
        ProjectModelResponse model, LensFilterResponse filter, IReadOnlyList<LensNodeResponse> nodes,
        IReadOnlyList<LensEdgeResponse> edges, IReadOnlyList<LensDiagnosticResponse> diagnostics,
        IReadOnlyList<LensAccessibilityItemResponse> accessibility)
    {
        var canonical = JsonSerializer.Serialize(new
        {
            ContractVersion,
            Lens,
            Scope = new { model.Project.Id, model.Project.Revision, model.Project.Name },
            Filter = filter,
            Nodes = nodes,
            Edges = edges,
            Diagnostics = diagnostics,
            Accessibility = accessibility,
        });
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
    }

    private static int Severity(string severity) => severity switch
    {
        "Error" => 0,
        "Warning" => 1,
        "Info" => 2,
        _ => 3,
    };
}
