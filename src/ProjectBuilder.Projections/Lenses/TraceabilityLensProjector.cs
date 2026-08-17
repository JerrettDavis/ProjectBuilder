using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ProjectBuilder.Contracts.Projects;

namespace ProjectBuilder.Projections.Lenses;

public static class TraceabilityLensProjector
{
    public const string ContractVersion = "traceability/1";

    public static TraceabilityProjectionResponse Project(ProjectModelResponse model,
        TraceabilityResponse traceability, string view = "outcomes")
    {
        if (view is not ("outcomes" or "debt" or "impact")) throw new ArgumentException("View must be outcomes, debt, or impact.", nameof(view));
        var nodes = new List<TraceNodeResponse>(); var edges = new List<TraceEdgeResponse>();
        foreach (var outcome in model.Outcomes.OrderBy(item => item.Name, StringComparer.Ordinal).ThenBy(item => item.Id, StringComparer.Ordinal))
            nodes.Add(Node($"outcome:{outcome.Id}", outcome.Id, "semantic-element", "outcome", outcome.Name,
                outcome.Statement, outcome.KnowledgeStatus, "outcomes", 100 + nodes.Count, [outcome.BeneficiaryName]));
        foreach (var claim in traceability.Claims.OrderBy(item => item.Statement, StringComparer.Ordinal).ThenBy(item => item.Id, StringComparer.Ordinal))
        {
            nodes.Add(Node($"claim:{claim.Id}", claim.Id, "semantic-claim", "claim", ClaimTitle(claim.Statement),
                claim.Statement, claim.Status, "claims", 300 + nodes.Count, [claim.Kind, claim.OwnerName]));
            foreach (var elementId in claim.ElementIds.Where(id => nodes.Any(node => node.SemanticReference == id)))
                edges.Add(new($"supports:{elementId}:{claim.Id}", "asserts", $"outcome:{elementId}", $"claim:{claim.Id}", "requires proof", "solid", "claim-explicit-reference"));
        }
        foreach (var evidence in traceability.Evidence.OrderBy(item => item.ProducedAt, StringComparer.Ordinal).ThenBy(item => item.Id, StringComparer.Ordinal))
        {
            var stale = IsStale(evidence, traceability.Claims, model.ChangeSets);
            var status = stale ? "stale" : evidence.Status;
            nodes.Add(Node($"evidence:{evidence.Id}", evidence.Id, "semantic-evidence", "evidence", evidence.Producer,
                evidence.Summary, status, "evidence", 500 + nodes.Count, [evidence.Kind, $"r{evidence.ModelRevision}"]));
            if (nodes.Any(node => node.Id == $"claim:{evidence.ClaimId}"))
                edges.Add(new($"proves:{evidence.ClaimId}:{evidence.Id}", "proves", $"claim:{evidence.ClaimId}", $"evidence:{evidence.Id}", status, status == "stale" ? "warning" : "double", "evidence-explicit-reference"));
        }
        var outcomeTraces = model.Outcomes.OrderBy(item => item.Name, StringComparer.Ordinal).Select(outcome =>
        {
            var claims = traceability.Claims.Where(claim => claim.ElementIds.Contains(outcome.Id, StringComparer.Ordinal)).ToArray();
            var evidence = traceability.Evidence.Where(item => claims.Any(claim => claim.Id == item.ClaimId)).ToArray();
            var current = evidence.Where(item => !IsStale(item, traceability.Claims, model.ChangeSets)).ToArray();
            var status = claims.Length == 0 ? "unsupported" : evidence.Length == 0 ? "unproven" : current.Length == 0 ? "stale" : current.Any(item => item.Status == "failed") ? "failing" : "supported";
            return new OutcomeTraceResponse(outcome.Id, outcome.Name, status, claims.Select(item => item.Id).Order().ToArray(), evidence.Select(item => item.Id).Order().ToArray(),
                status switch { "unsupported" => "No first-class claim references this outcome.", "unproven" => "A claim exists but has no evidence record.", "stale" => "Linked evidence predates a changed definition.", "failing" => "At least one current evidence item failed.", _ => "Current attributable evidence supports the linked claim." });
        }).ToArray();
        var missing = outcomeTraces.Where(item => item.Status != "supported").Select(item => new MissingTraceResponse(
            item.Status == "unsupported" ? "PB-EVID-001" : item.Status == "stale" ? "PB-EVID-003" : "PB-EVID-004",
            item.Status is "failing" or "stale" ? "Error" : "Warning", item.OutcomeId, item.OutcomeName,
            item.Status, $"/projects/{model.Project.Id}/evidence/new?scope={item.OutcomeId}", item.Explanation)).ToArray();
        var impact = traceability.Claims.Select(claim =>
        {
            var latest = LatestRevision(claim.ElementIds, model.ChangeSets);
            var evidence = traceability.Evidence.Where(item => item.ClaimId == claim.Id).ToArray();
            var stale = evidence.Any(item => item.ModelRevision < latest);
            var name = model.Outcomes.FirstOrDefault(item => claim.ElementIds.Contains(item.Id))?.Name ?? ClaimTitle(claim.Statement);
            return new ImpactTraceResponse(claim.ElementIds[0], name, latest, [claim.Id], evidence.Select(item => item.Id).ToArray(),
                stale ? "review-required" : "current", stale ? "A linked definition changed after evidence was produced." : "No linked semantic change is newer than the evidence baseline.");
        }).OrderByDescending(item => item.ChangedAtRevision).ThenBy(item => item.ScopeId, StringComparer.Ordinal).ToArray();
        var diagnostics = new List<LensDiagnosticResponse>();
        if (traceability.Claims.Count == 0) diagnostics.Add(new("traceability.claims.missing", "Warning", "No first-class claims are recorded; test counts are not treated as evidence.", model.Project.Id));
        if (traceability.Evidence.Any(item => traceability.Claims.All(claim => claim.Id != item.ClaimId))) diagnostics.Add(new("PB-EVID-002", "Error", "Evidence exists without a covered claim.", null));
        var orderedNodes = nodes.OrderBy(item => item.Order).ThenBy(item => item.Id, StringComparer.Ordinal).ToArray();
        var orderedEdges = edges.OrderBy(item => item.Id, StringComparer.Ordinal).ToArray();
        var accessibility = orderedNodes.Select((node, index) => new LensAccessibilityItemResponse(node.Id, node.SemanticReference,
            node.Kind, $"{node.Kind}: {node.Title}", node.Status, index + 1, orderedNodes.Length,
            orderedEdges.Count(edge => edge.TargetNodeId == node.Id), orderedEdges.Count(edge => edge.SourceNodeId == node.Id))).ToArray();
        var scope = new TraceabilityScopeResponse(model.Project.Id, model.Project.Name, model.Project.Revision, "discovery");
        var orderedDiagnostics = diagnostics.OrderBy(item => item.Code, StringComparer.Ordinal).ToArray();
        var hash = Hash(scope, view, orderedNodes, orderedEdges, outcomeTraces, missing, impact, orderedDiagnostics, accessibility);
        var result = new TraceabilityProjectionResponse($"traceability:{model.Project.Id}:r{model.Project.Revision}:{hash[..12]}",
            ContractVersion, scope, view, hash, orderedNodes, orderedEdges, outcomeTraces, missing, impact, orderedDiagnostics, accessibility);
        TraceabilityProjectionValidator.EnsureValid(result); return result;
    }

    private static bool IsStale(EvidenceResponse evidence, IReadOnlyList<ClaimResponse> claims, IReadOnlyList<ChangeSetResponse> changes) =>
        claims.FirstOrDefault(claim => claim.Id == evidence.ClaimId) is { } claim && LatestRevision(claim.ElementIds, changes) > evidence.ModelRevision;
    private static long LatestRevision(IReadOnlyList<string> ids, IReadOnlyList<ChangeSetResponse> changes) => changes
        .Where(change => change.Operations.Any(operation => operation.ElementId is not null && ids.Contains(operation.ElementId, StringComparer.Ordinal)))
        .Select(change => change.ResultRevision).DefaultIfEmpty(1).Max();
    private static string ClaimTitle(string statement) => statement.Length <= 62 ? statement : statement[..59] + "…";
    private static TraceNodeResponse Node(string id, string semantic, string origin, string kind, string title,
        string detail, string status, string lane, int order, IReadOnlyList<string> badges) => new(id, semantic, origin,
            kind, title, detail, status, lane, order, badges, [new("trace", "Attribution", [new("origin", "Origin", origin, "known"), new("status", "Status", status, "known")])]);
    private static string Hash(params object[] values) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(values))));
}

public static class TraceabilityProjectionValidator
{
    public static void EnsureValid(TraceabilityProjectionResponse projection)
    {
        var ids = projection.Nodes.Select(node => node.Id).ToArray();
        if (ids.Distinct(StringComparer.Ordinal).Count() != ids.Length) throw new InvalidOperationException("Traceability node identifiers must be unique.");
        var known = ids.ToHashSet(StringComparer.Ordinal);
        foreach (var edge in projection.Edges) if (!known.Contains(edge.SourceNodeId) || !known.Contains(edge.TargetNodeId)) throw new InvalidOperationException($"Trace edge '{edge.Id}' has a missing endpoint.");
        if (projection.AccessibilityTree.Count != projection.Nodes.Count) throw new InvalidOperationException("Every trace node requires one accessibility item.");
        foreach (var trace in projection.OutcomeTraces)
            if (!known.Contains($"outcome:{trace.OutcomeId}")) throw new InvalidOperationException($"Outcome trace '{trace.OutcomeId}' has no node.");
    }
}
