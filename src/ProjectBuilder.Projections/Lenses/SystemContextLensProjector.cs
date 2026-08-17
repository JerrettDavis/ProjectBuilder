using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ProjectBuilder.Contracts.Projects;

namespace ProjectBuilder.Projections.Lenses;

public static class SystemContextLensProjector
{
    public const string ContractVersion = "system-context/1";

    public static SystemContextProjectionResponse Project(ProjectModelResponse model, string ownedSystemId, string overlay = "ownership")
    {
        ArgumentNullException.ThrowIfNull(model);
        if (overlay is not ("ownership" or "trust" or "none")) throw new ArgumentException("Overlay must be ownership, trust, or none.", nameof(overlay));
        var context = (model.SystemContexts ?? []).SingleOrDefault(item => item.OwnedSystemId == ownedSystemId)
            ?? throw new ArgumentException($"System '{ownedSystemId}' is not available in this project.", nameof(ownedSystemId));
        var nodes = new List<SystemContextNodeResponse>();
        foreach (var actor in context.ParticipantIds.Zip(context.ParticipantNames).Where(item => item.First != context.ExternalSystemId))
            nodes.Add(Node($"actor:{actor.First}", actor.First, "semantic-reference", "actor", actor.Second,
                "Participant at this interface", "people", 100 + nodes.Count, overlay == "ownership" ? ["participant"] : []));
        nodes.Add(Node($"system:{context.OwnedSystemId}", context.OwnedSystemId, "semantic-element", "owned-system",
            context.OwnedSystemName, context.OwnedSystemPurpose, "owned", 300,
            overlay == "ownership" ? [$"owned by {context.OwnedSystemOwnerName}"] : []));
        nodes.Add(Node($"interface:{context.InterfaceId}", context.InterfaceId, "semantic-element", "interface",
            context.InterfaceName, context.InterfaceDescription, "boundary", 400, [context.InterfaceKind]));
        nodes.Add(Node($"contract:{context.ContractId}", context.ContractId, "semantic-element", "contract",
            context.ContractName, context.ContractDescription, "boundary", 500,
            overlay == "ownership" ? [context.ContractKind, $"owned by {context.ContractOwnerName}"] : [context.ContractKind]));
        nodes.Add(Node($"system:{context.ExternalSystemId}", context.ExternalSystemId, "semantic-element", "external-system",
            context.ExternalSystemName, context.ExternalSystemPurpose, "external", 600,
            overlay == "trust" ? [context.ExternalKnowledgeStatus, "outside trust boundary"] : [context.ExternalKnowledgeStatus]));
        if (!string.IsNullOrWhiteSpace(context.CrossingEffectId))
            nodes.Add(Node($"effect:{context.CrossingEffectId}", context.CrossingEffectId!, "semantic-reference", "effect",
                context.CrossingEffectName ?? "Boundary effect", "Explicit effect referenced by the boundary", "boundary", 550, ["requested effect"]));

        var connections = new List<SystemContextConnectionResponse>();
        foreach (var actor in context.ParticipantIds.Where(id => id != context.ExternalSystemId))
            connections.Add(new($"uses:{actor}:{context.InterfaceId}", "usesInterface", $"actor:{actor}", $"interface:{context.InterfaceId}", "uses", "solid", "semantic-interface-reference"));
        connections.Add(new($"exposes:{context.OwnedSystemId}:{context.InterfaceId}", "exposes", $"system:{context.OwnedSystemId}", $"interface:{context.InterfaceId}", "exposes", "solid", "semantic-containment"));
        connections.Add(new($"governs:{context.ContractId}:{context.InterfaceId}", "governs", $"contract:{context.ContractId}", $"interface:{context.InterfaceId}", "governs", "dashed", "semantic-contract-reference"));
        connections.Add(new($"crosses:{context.InterfaceId}:{context.ExternalSystemId}", "crossesBoundary", $"interface:{context.InterfaceId}", $"system:{context.ExternalSystemId}", "crosses", "double", "semantic-boundary-reference"));
        if (!string.IsNullOrWhiteSpace(context.CrossingEffectId))
            connections.Add(new($"effect:{context.CrossingEffectId}:{context.BoundaryId}", "requestsEffect", $"effect:{context.CrossingEffectId}", $"contract:{context.ContractId}", "crossing effect", "dotted", "semantic-boundary-effect-reference"));
        var dataFlows = new[]
        {
            new SystemDataFlowResponse($"request:{context.ContractId}", "outbound", $"system:{context.OwnedSystemId}", $"system:{context.ExternalSystemId}", context.RequestData, context.DataClassification, context.ContractId, "contract-explicit-field"),
            new SystemDataFlowResponse($"response:{context.ContractId}", "inbound", $"system:{context.ExternalSystemId}", $"system:{context.OwnedSystemId}", context.ResponseData, context.DataClassification, context.ContractId, "contract-explicit-field"),
        };
        var boundary = new SystemBoundaryResponse(context.BoundaryId, context.BoundaryId, context.BoundaryName,
            context.BoundaryKinds, context.OwnedSystemId, context.ExternalSystemId, context.BoundaryOwnerNames,
            context.BoundaryKnowledgeStatus, context.CrossingEffectId, context.CrossingEffectName);
        var diagnostics = new List<LensDiagnosticResponse>();
        if (!context.BoundaryKinds.Contains("trust", StringComparer.OrdinalIgnoreCase)) diagnostics.Add(new("system-context.trust.unmodeled", "Warning", "Trust is not an explicit kind on this boundary.", context.BoundaryId));
        if (context.SchemaReference.Equals("Unknown", StringComparison.OrdinalIgnoreCase)) diagnostics.Add(new("system-context.schema.unknown", "Info", "The contract schema reference is explicitly Unknown.", context.ContractId));
        var orderedNodes = nodes.OrderBy(node => node.Order).ThenBy(node => node.Id, StringComparer.Ordinal).ToArray();
        var orderedConnections = connections.OrderBy(edge => edge.Id, StringComparer.Ordinal).ToArray();
        var accessibility = orderedNodes.Select((node, index) => new LensAccessibilityItemResponse(node.Id,
            node.SemanticReference, node.Kind, $"{node.Kind}: {node.Title}", "known", index + 1, orderedNodes.Length,
            orderedConnections.Count(edge => edge.TargetNodeId == node.Id), orderedConnections.Count(edge => edge.SourceNodeId == node.Id))).ToArray();
        var scope = new SystemContextScopeResponse(model.Project.Id, model.Project.Revision, context.OwnedSystemId, context.OwnedSystemName);
        var hash = Hash(scope, overlay, orderedNodes, orderedConnections, boundary, dataFlows, diagnostics, accessibility);
        var result = new SystemContextProjectionResponse($"system-context:{ownedSystemId}:r{model.Project.Revision}:{hash[..12]}",
            ContractVersion, scope, overlay, hash, orderedNodes, orderedConnections, boundary, dataFlows,
            diagnostics.OrderBy(item => item.Code, StringComparer.Ordinal).ToArray(), accessibility);
        SystemContextProjectionValidator.EnsureValid(result);
        return result;
    }

    private static SystemContextNodeResponse Node(string id, string semantic, string origin, string kind,
        string title, string detail, string zone, int order, IReadOnlyList<string> badges) =>
        new(id, semantic, origin, kind, title, detail, zone, order, badges,
            [new("identity", "Definition", [new("kind", "Kind", kind, "known"), new("detail", "Purpose", detail, "known")])]);

    private static string Hash(params object[] parts) => Convert.ToHexStringLower(SHA256.HashData(
        Encoding.UTF8.GetBytes(JsonSerializer.Serialize(parts))));
}

public static class SystemContextProjectionValidator
{
    public static void EnsureValid(SystemContextProjectionResponse projection)
    {
        var ids = projection.Nodes.Select(node => node.Id).ToArray();
        if (ids.Distinct(StringComparer.Ordinal).Count() != ids.Length) throw new InvalidOperationException("System context node identifiers must be unique.");
        var known = ids.ToHashSet(StringComparer.Ordinal);
        foreach (var edge in projection.Connections)
            if (!known.Contains(edge.SourceNodeId) || !known.Contains(edge.TargetNodeId)) throw new InvalidOperationException($"Connection '{edge.Id}' has a missing endpoint.");
        foreach (var flow in projection.DataFlows)
            if (!known.Contains(flow.SourceNodeId) || !known.Contains(flow.TargetNodeId)) throw new InvalidOperationException($"Data flow '{flow.Id}' has a missing endpoint.");
        if (projection.AccessibilityTree.Count != projection.Nodes.Count) throw new InvalidOperationException("Every system-context node requires one accessibility item.");
    }
}
