using ProjectBuilder.Contracts.Projects;

namespace ProjectBuilder.Projections.Lenses;

public static class LensProjectionValidator
{
    public static IReadOnlyList<string> Validate(LensProjectionResponse projection)
    {
        ArgumentNullException.ThrowIfNull(projection);
        var findings = new List<string>();
        FindDuplicates(projection.Nodes.Select(node => node.Id), "node", findings);
        FindDuplicates(projection.Nodes.Select(node => node.SemanticId), "semantic node", findings);
        FindDuplicates(projection.Edges.Select(edge => edge.Id), "edge", findings);
        FindDuplicates(projection.Nodes.SelectMany(node => node.Ports).Select(port => port.Id), "port", findings);

        var nodes = projection.Nodes.ToDictionary(node => node.Id, StringComparer.Ordinal);
        foreach (var edge in projection.Edges)
        {
            ValidateEndpoint(edge.Id, edge.SourceNodeId, edge.SourcePortId, "output", nodes, findings);
            ValidateEndpoint(edge.Id, edge.TargetNodeId, edge.TargetPortId, "input", nodes, findings);
        }

        var accessibleIds = projection.AccessibilityTree.Select(item => item.NodeId).ToArray();
        FindDuplicates(accessibleIds, "accessibility node", findings);
        foreach (var missing in nodes.Keys.Except(accessibleIds, StringComparer.Ordinal))
            findings.Add($"Node '{missing}' is absent from the accessibility tree.");
        foreach (var unknown in accessibleIds.Except(nodes.Keys, StringComparer.Ordinal))
            findings.Add($"Accessibility item '{unknown}' does not reference a node.");
        for (var index = 0; index < projection.AccessibilityTree.Count; index++)
        {
            var item = projection.AccessibilityTree[index];
            if (item.Position != index + 1 || item.SetSize != projection.Nodes.Count)
                findings.Add($"Accessibility item '{item.NodeId}' has an invalid position or set size.");
        }

        return findings.Order(StringComparer.Ordinal).ToArray();
    }

    public static void EnsureValid(LensProjectionResponse projection)
    {
        var findings = Validate(projection);
        if (findings.Count > 0)
            throw new InvalidOperationException($"Lens projection contract is invalid: {string.Join(" ", findings)}");
    }

    private static void ValidateEndpoint(
        string edgeId, string nodeId, string portId, string direction,
        Dictionary<string, LensNodeResponse> nodes, List<string> findings)
    {
        if (!nodes.TryGetValue(nodeId, out var node))
        {
            findings.Add($"Edge '{edgeId}' references missing node '{nodeId}'.");
            return;
        }
        var port = node.Ports.SingleOrDefault(candidate => candidate.Id == portId);
        if (port is null) findings.Add($"Edge '{edgeId}' references port '{portId}' outside node '{nodeId}'.");
        else if (port.Direction != direction) findings.Add($"Edge '{edgeId}' requires an {direction} port but '{portId}' is {port.Direction}.");
    }

    private static void FindDuplicates(IEnumerable<string> values, string kind, List<string> findings)
    {
        foreach (var value in values.GroupBy(value => value, StringComparer.Ordinal).Where(group => group.Count() > 1).Select(group => group.Key))
            findings.Add($"Duplicate {kind} identifier '{value}'.");
    }
}
