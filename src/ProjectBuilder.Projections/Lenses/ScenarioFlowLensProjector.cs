using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ProjectBuilder.Contracts.Projects;

namespace ProjectBuilder.Projections.Lenses;

public static class ScenarioFlowLensProjector
{
    public const string ContractVersion = "scenario-flow/1";

    public static ScenarioFlowProjectionResponse Project(ProjectModelResponse model, string scenarioId)
    {
        ArgumentNullException.ThrowIfNull(model);
        if (string.IsNullOrWhiteSpace(scenarioId)) throw new ArgumentException("A scenario identifier is required.", nameof(scenarioId));
        var narrative = model.Narratives.SingleOrDefault(item => item.ScenarioId == scenarioId) ??
            throw new ArgumentException($"Scenario '{scenarioId}' is not available in this project.", nameof(scenarioId));
        var paths = model.Paths.Where(path => path.ScenarioId == scenarioId).OrderBy(path => path.BranchPathId, StringComparer.Ordinal).ToArray();
        var diagnostics = new List<LensDiagnosticResponse>();
        if (paths.Length == 0) diagnostics.Add(new("scenario-flow.paths.missing", "Info",
            "Only the authored primary scenario is visible. No alternate, exceptional, degraded, or recovery path is modeled.", scenarioId));
        if (!paths.Any(path => path.EffectKind.Equals("externalInteraction", StringComparison.OrdinalIgnoreCase)))
            diagnostics.Add(new("scenario-flow.boundary.unmodeled", "Info",
                "No explicit external-interaction effect identifies a boundary crossing in this scenario scope.", scenarioId));

        var lanes = Lanes(model, narrative);
        var nodes = new List<ScenarioFlowNodeResponse>();
        var edges = new List<ScenarioFlowEdgeResponse>();
        var projectedPaths = new List<ScenarioFlowPathResponse>();
        var playback = new List<ScenarioFlowPlaybackStepResponse>();
        ProjectPrimary(narrative, lanes, nodes, edges, projectedPaths, playback);
        foreach (var path in paths) ProjectPath(path, lanes, nodes, edges, projectedPaths, playback);
        var overlays = Overlays(model, narrative, paths, projectedPaths);

        var orderedNodes = nodes.DistinctBy(node => node.Id).OrderBy(node => node.Order).ThenBy(node => node.Id, StringComparer.Ordinal).ToArray();
        var orderedEdges = edges.OrderBy(edge => edge.PathId, StringComparer.Ordinal).ThenBy(edge => edge.Id, StringComparer.Ordinal).ToArray();
        var orderedPaths = projectedPaths.OrderBy(path => PathOrder(path.Classification)).ThenBy(path => path.Id, StringComparer.Ordinal).ToArray();
        var orderedPlayback = playback.OrderBy(step => PathOrder(orderedPaths.Single(path => path.Id == step.PathId).Classification))
            .ThenBy(step => step.PathId, StringComparer.Ordinal).ThenBy(step => step.Position).ToArray();
        var accessibility = orderedNodes.Select((node, index) => new LensAccessibilityItemResponse(
            node.Id, node.SemanticReference, node.Kind, $"{node.Kind}: {node.Title}", node.Status,
            index + 1, orderedNodes.Length, orderedEdges.Count(edge => edge.TargetNodeId == node.Id),
            orderedEdges.Count(edge => edge.SourceNodeId == node.Id))).ToArray();
        var scope = new ScenarioFlowScopeResponse(model.Project.Id, model.Project.Revision, scenarioId,
            narrative.ScenarioName, narrative.Classification, narrative.EpisodeName, narrative.SceneName, narrative.OutcomeName);
        var orderedDiagnostics = diagnostics.OrderBy(item => item.Code, StringComparer.Ordinal).ToArray();
        var hash = Hash(scope, lanes, orderedNodes, orderedEdges, orderedPaths, orderedPlayback, overlays, orderedDiagnostics, accessibility);
        var projection = new ScenarioFlowProjectionResponse($"scenario-flow:{scenarioId}:r{model.Project.Revision}:{hash[..12]}",
            ContractVersion, scope, hash, lanes, orderedNodes, orderedEdges, orderedPaths, orderedPlayback, overlays,
            orderedDiagnostics, accessibility);
        ScenarioFlowProjectionValidator.EnsureValid(projection);
        return projection;
    }

    private static ScenarioFlowLaneResponse[] Lanes(ProjectModelResponse model, NarrativeResponse narrative)
    {
        var initiator = model.Actors.Single(actor => actor.Id == narrative.InitiatorId);
        var receiver = model.Actors.Single(actor => actor.Id == narrative.ReceiverId);
        return new[]
        {
            new ScenarioFlowLaneResponse("lane:context", "Scenario context", narrative.ScenarioId, narrative.ScenarioName, "Context and results", 0),
            new ScenarioFlowLaneResponse($"lane:{initiator.Id}", initiator.Name, initiator.Id, initiator.Name, initiator.ContextualRole, 1),
            new ScenarioFlowLaneResponse($"lane:{receiver.Id}", receiver.Name, receiver.Id, receiver.Name, receiver.ContextualRole, 2),
        }.DistinctBy(lane => lane.Id).OrderBy(lane => lane.Order).ThenBy(lane => lane.Id, StringComparer.Ordinal).ToArray();
    }

    private static void ProjectPrimary(NarrativeResponse narrative, IReadOnlyList<ScenarioFlowLaneResponse> lanes,
        List<ScenarioFlowNodeResponse> nodes, List<ScenarioFlowEdgeResponse> edges,
        List<ScenarioFlowPathResponse> paths, List<ScenarioFlowPlaybackStepResponse> playback)
    {
        const string pathId = "path:primary";
        var initiatorLane = $"lane:{narrative.InitiatorId}";
        var receiverLane = $"lane:{narrative.ReceiverId}";
        var sequence = new List<ScenarioFlowNodeResponse>
        {
            Node($"derived:{narrative.ScenarioId}:facts", $"{narrative.ScenarioId}:facts", "derived-explicit-field", "facts",
                "Starting facts", Join(narrative.StartingFacts), "known", "lane:context", 100, "stack", ["Primary path"],
                Section("context", "Scenario context", Field("facts", "Facts", Join(narrative.StartingFacts)))),
            Node($"derived:{narrative.ScenarioId}:trigger", $"{narrative.ScenarioId}:trigger", "derived-explicit-field", "trigger",
                "Trigger", narrative.Trigger, "known", initiatorLane, 200, "event", [narrative.InitiatorName],
                Section("trigger", "Initiation", Field("actor", "Initiator", narrative.InitiatorName), Field("trigger", "Trigger", narrative.Trigger))),
            Node($"node:{narrative.IntentId}", narrative.IntentId, "semantic-element", "intent", "Intent", narrative.Intent,
                "known", initiatorLane, 300, "intent", ["Human intent"], Section("intent", "Requested meaning", Field("intent", "Intent", narrative.Intent))),
            Node($"node:{narrative.InteractionId}", narrative.InteractionId, "semantic-element", "interaction", narrative.InteractionName,
                $"{narrative.InitiatorName} → {narrative.ReceiverName}", "known", receiverLane, 400, "interaction",
                ["Directed interaction"], Section("interaction", "Responsibility crossing", Field("initiator", "Initiator", narrative.InitiatorName),
                    Field("receiver", "Receiver", narrative.ReceiverName), Field("setting", "Scene setting", narrative.Setting))),
            Node($"node:{narrative.StepId}", narrative.StepId, "semantic-element", "step", "Perform step", narrative.Step,
                "known", receiverLane, 500, "step", [narrative.SceneName], Section("step", "Behavior", Field("step", "Step", narrative.Step))),
            Node($"node:{narrative.ObservationId}", narrative.ObservationId, "semantic-element", "observation", "Observe result",
                narrative.Observation, "known", initiatorLane, 600, "observation", ["Participant-visible"],
                Section("observation", "Observation", Field("observation", "Visible result", narrative.Observation))),
        };
        sequence.Add(Node($"derived:{narrative.ScenarioId}:expected-outcome", $"{narrative.ScenarioId}:expected-outcome",
            "derived-explicit-field", "result", "Expected outcome", narrative.ExpectedOutcome, "known", "lane:context",
            700, "terminal", ["Outcome boundary", $"Declared results · {Join(narrative.SemanticResults)}"],
            Section("result", "Outcome boundary", Field("expected", "Expected outcome", narrative.ExpectedOutcome),
                Field("result-set", "Declared semantic results", Join(narrative.SemanticResults)))));
        AddSequence(pathId, "primary", sequence, nodes, edges, playback);
        paths.Add(new(pathId, narrative.ScenarioId, narrative.Classification, "Primary authored route", narrative.Trigger,
            sequence.Select(node => node.Id).ToArray(), narrative.ExpectedOutcome,
            "solid", "modeled"));
    }

    private static void ProjectPath(PathResponse path, IReadOnlyList<ScenarioFlowLaneResponse> lanes,
        List<ScenarioFlowNodeResponse> nodes, List<ScenarioFlowEdgeResponse> edges,
        List<ScenarioFlowPathResponse> paths, List<ScenarioFlowPlaybackStepResponse> playback)
    {
        var ownerLane = lanes.FirstOrDefault(lane => lane.ParticipantId == path.OwnerId)?.Id ?? "lane:context";
        var branchId = $"path:{path.BranchPathId}";
        var branch = new List<ScenarioFlowNodeResponse>
        {
            Node($"node:{path.BranchConditionId}", path.BranchConditionId, "semantic-element", "condition", path.BranchConditionName,
                path.BranchCondition, "known", ownerLane, 1000, "decision", [path.BranchConditionKind, path.BranchClassification],
                Section("condition", "Branch gate", Field("kind", "Condition kind", path.BranchConditionKind), Field("statement", "Predicate", path.BranchCondition))),
        };
        for (var index = 0; index < path.BranchSegments.Count; index++)
            branch.Add(Node($"derived:{path.BranchPathId}:segment:{index:D2}", $"{path.BranchPathId}:segment:{index:D2}",
                "derived-explicit-field", "segment", $"Branch step {index + 1}", path.BranchSegments[index], "known", ownerLane,
                1010 + index, "step", [$"{index + 1} of {path.BranchSegments.Count}"],
                Section("segment", "Ordered segment", Field("statement", "Behavior", path.BranchSegments[index]))));
        branch.Add(Node($"node:{path.EffectId}", path.EffectId, "semantic-element", "effect", path.EffectName, path.EffectStatement,
            "known", ownerLane, 1100, path.EffectKind.Equals("externalInteraction", StringComparison.OrdinalIgnoreCase) ? "boundary" : "effect",
            path.EffectKind.Equals("externalInteraction", StringComparison.OrdinalIgnoreCase) ? ["Explicit boundary effect", path.EffectKind] : [path.EffectKind],
            Section("effect", "Intended consequence", Field("kind", "Effect kind", path.EffectKind), Field("statement", "Effect", path.EffectStatement))));
        branch.Add(Node($"node:{path.TerminalResultId}", path.TerminalResultId, "semantic-element", "result", path.TerminalResultName,
            path.BranchObservation, "known", "lane:context", 1200, "terminal", [path.TerminalResultKind],
            Section("terminal", "Branch closure", Field("state", "Terminal state", path.BranchTerminalState),
                Field("observation", "Observation", path.BranchObservation))));
        AddSequence(branchId, path.BranchClassification, branch, nodes, edges, playback);
        paths.Add(new(branchId, path.BranchPathId, path.BranchClassification, path.BranchName, path.BranchCondition,
            branch.Select(node => node.Id).ToArray(), path.TerminalResultName, "dashed", "modeled"));

        var recoveryId = $"path:{path.RecoveryPathId}";
        var recovery = new List<ScenarioFlowNodeResponse>
        {
            Node($"node:{path.RecoveryConditionId}", path.RecoveryConditionId, "semantic-element", "condition", "Recovery entry",
                path.RecoveryCondition, "known", ownerLane, 1300, "decision", [path.RecoveryStrategy],
                Section("recovery-condition", "Recovery gate", Field("condition", "Entry condition", path.RecoveryCondition))),
        };
        for (var index = 0; index < path.RecoverySegments.Count; index++)
            recovery.Add(Node($"derived:{path.RecoveryPathId}:segment:{index:D2}", $"{path.RecoveryPathId}:segment:{index:D2}",
                "derived-explicit-field", "recovery-step", $"Recovery step {index + 1}", path.RecoverySegments[index], "known", ownerLane,
                1310 + index, "recovery", [$"{index + 1} of {path.RecoverySegments.Count}"],
                Section("recovery", "Ordered recovery", Field("statement", "Behavior", path.RecoverySegments[index]))));
        recovery.Add(Node($"node:{path.RecoveryResultId}", path.RecoveryResultId, "semantic-element", "result", path.RecoveryResultName,
            path.RecoveryObservation, "known", "lane:context", 1400, "terminal", ["Recovered", path.RecoveryStrategy],
            Section("recovery-result", "Recovery closure", Field("state", "Terminal state", path.RecoveryTerminalState),
                Field("observation", "Observation", path.RecoveryObservation), Field("retry", "Retry policy", Empty(path.RetryPolicy)))));
        AddSequence(recoveryId, "recovery", recovery, nodes, edges, playback);
        paths.Add(new(recoveryId, path.RecoveryPathId, "recovery", path.RecoveryName, path.RecoveryCondition,
            recovery.Select(node => node.Id).ToArray(), path.RecoveryResultName, "double", "modeled"));
        edges.Add(new($"edge:{path.BranchPathId}:recovers-via:{path.RecoveryPathId}", "recoversVia", branch[^1].Id,
            recovery[0].Id, "Recovery available", "double", "semantic-path-reference", recoveryId));
        if (path.EffectKind.Equals("externalInteraction", StringComparison.OrdinalIgnoreCase))
            edges.Add(new($"edge:{path.EffectId}:boundary", "crossesBoundary", branch[^3].Id, branch[^2].Id,
                "External interaction boundary", "boundary", "semantic-effect-classification", branchId));
    }

    private static void AddSequence(string pathId, string phase, List<ScenarioFlowNodeResponse> sequence,
        List<ScenarioFlowNodeResponse> nodes, List<ScenarioFlowEdgeResponse> edges,
        List<ScenarioFlowPlaybackStepResponse> playback)
    {
        foreach (var node in sequence) nodes.Add(node);
        for (var index = 1; index < sequence.Count; index++)
            edges.Add(new($"edge:{pathId}:{index - 1:D2}-{index:D2}", "next", sequence[index - 1].Id, sequence[index].Id,
                "Then", phase.Equals("primary", StringComparison.OrdinalIgnoreCase) ? "solid" : "dashed", "derived-order", pathId));
        for (var index = 0; index < sequence.Count; index++)
            playback.Add(new(index + 1, sequence.Count, sequence[index].Id, phase,
                sequence[index].Detail, sequence[index].LaneId, pathId,
                sequence[index].Kind == "condition", sequence[index].Kind == "result"));
    }

    private static ScenarioFlowOverlayResponse[] Overlays(
        ProjectModelResponse model, NarrativeResponse narrative, IReadOnlyList<PathResponse> paths,
        IReadOnlyList<ScenarioFlowPathResponse> projectedPaths)
    {
        var result = new List<ScenarioFlowOverlayResponse>
        {
            new("path:primary", "", "Primary authored route", Join(narrative.StartingFacts), narrative.ExpectedOutcome,
                [], narrative.Observation, "", "Not linked", "No invariant is explicitly linked to the primary narrative route.",
                "", "Playback proceeds because this route has no linked transition invariant."),
        };
        foreach (var path in paths)
        {
            var state = model.StateLogic.SingleOrDefault(item => item.TransitionId == path.SourceTransitionId);
            if (state is null) continue;
            var invariantId = state.InvariantId;
            var changedFacts = state.ChangedFactIds?.Contains(state.FactId, StringComparer.Ordinal) == true ? new[] { state.FactName } : [];
            var branchId = $"path:{path.BranchPathId}";
            var recoveryId = $"path:{path.RecoveryPathId}";
            result.Add(new(branchId, state.TransitionId, state.TransitionName, state.SourcePredicate,
                path.BranchTerminalState, changedFacts, path.BranchObservation, invariantId, state.InvariantName,
                state.InvariantStatement, projectedPaths.Single(item => item.Id == branchId).NodeIds[0],
                $"Review {state.InvariantName} before following the {path.BranchClassification} route."));
            result.Add(new(recoveryId, state.TransitionId, state.TransitionName, path.BranchTerminalState,
                path.RecoveryTerminalState, changedFacts, path.RecoveryObservation, invariantId, state.InvariantName,
                state.InvariantStatement, projectedPaths.Single(item => item.Id == recoveryId).NodeIds[0],
                $"Review {state.InvariantName} before following the recovery route."));
        }
        return result.OrderBy(item => PathOrder(projectedPaths.Single(path => path.Id == item.PathId).Classification))
            .ThenBy(item => item.PathId, StringComparer.Ordinal).ToArray();
    }

    private static ScenarioFlowNodeResponse Node(string id, string reference, string origin, string kind, string title,
        string detail, string status, string lane, int order, string shape, IReadOnlyList<string> badges,
        params LensInspectorSectionResponse[] inspector) =>
        new(id, reference, origin, kind, title, detail, status, lane, order, shape, badges, inspector);
    private static LensInspectorSectionResponse Section(string id, string label, params LensInspectorFieldResponse[] fields) => new(id, label, fields);
    private static LensInspectorFieldResponse Field(string key, string label, string value) => new(key, label, value, "known");
    private static string Join(IReadOnlyList<string> values) => values.Count == 0 ? "Unknown" : string.Join(" · ", values);
    private static string Empty(string value) => string.IsNullOrWhiteSpace(value) ? "Unknown" : value;
    private static int PathOrder(string classification) => classification.ToLowerInvariant() switch
    {
        "happy" => 0,
        "alternate" => 1,
        "exceptional" => 2,
        "degraded" => 3,
        "cancellation" => 4,
        "compensation" => 5,
        "recovery" => 6,
        _ => 7,
    };

    private static string Hash(ScenarioFlowScopeResponse scope, IReadOnlyList<ScenarioFlowLaneResponse> lanes,
        IReadOnlyList<ScenarioFlowNodeResponse> nodes, IReadOnlyList<ScenarioFlowEdgeResponse> edges,
        IReadOnlyList<ScenarioFlowPathResponse> paths, IReadOnlyList<ScenarioFlowPlaybackStepResponse> playback,
        IReadOnlyList<ScenarioFlowOverlayResponse> overlays,
        IReadOnlyList<LensDiagnosticResponse> diagnostics, IReadOnlyList<LensAccessibilityItemResponse> accessibility)
    {
        var json = JsonSerializer.Serialize(new
        {
            ContractVersion,
            Scope = scope,
            Lanes = lanes,
            Nodes = nodes,
            Edges = edges,
            Paths = paths,
            Playback = playback,
            Overlays = overlays,
            Diagnostics = diagnostics,
            Accessibility = accessibility
        });
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(json)));
    }
}

public static class ScenarioFlowProjectionValidator
{
    public static void EnsureValid(ScenarioFlowProjectionResponse projection)
    {
        ArgumentNullException.ThrowIfNull(projection);
        var errors = new List<string>();
        var nodeIds = projection.Nodes.Select(node => node.Id).ToArray();
        if (nodeIds.Distinct(StringComparer.Ordinal).Count() != nodeIds.Length) errors.Add("Scenario Flow node identifiers must be unique.");
        var known = nodeIds.ToHashSet(StringComparer.Ordinal);
        foreach (var edge in projection.Edges)
        {
            if (!known.Contains(edge.SourceNodeId)) errors.Add($"Edge '{edge.Id}' has a missing source.");
            if (!known.Contains(edge.TargetNodeId)) errors.Add($"Edge '{edge.Id}' has a missing target.");
        }
        foreach (var path in projection.Paths)
            foreach (var nodeId in path.NodeIds.Where(nodeId => !known.Contains(nodeId))) errors.Add($"Path '{path.Id}' has a missing node '{nodeId}'.");
        foreach (var step in projection.Playback.Where(step => !known.Contains(step.NodeId))) errors.Add($"Playback step '{step.Position}' has a missing node.");
        if (projection.AccessibilityTree.Count != projection.Nodes.Count) errors.Add("Every Scenario Flow node requires one accessibility item.");
        if (errors.Count > 0) throw new InvalidOperationException(string.Join(Environment.NewLine, errors.Order(StringComparer.Ordinal)));
    }
}
