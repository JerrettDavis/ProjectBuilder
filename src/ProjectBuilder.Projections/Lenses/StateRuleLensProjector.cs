using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ProjectBuilder.Contracts.Projects;

namespace ProjectBuilder.Projections.Lenses;

public static class StateRuleLensProjector
{
    public const string ContractVersion = "state-rule/1";

    public static StateRuleProjectionResponse Project(ProjectModelResponse model, string stateId)
    {
        ArgumentNullException.ThrowIfNull(model);
        if (string.IsNullOrWhiteSpace(stateId)) throw new ArgumentException("A state identifier is required.", nameof(stateId));
        var state = model.StateLogic.SingleOrDefault(item => item.StateId == stateId) ??
            throw new ArgumentException($"State '{stateId}' is not available in this project.", nameof(stateId));
        var changedFactIds = state.ChangedFactIds ?? [];
        var ruleIds = state.RuleIds ?? [];
        var invariantIds = state.InvariantIds ?? [];
        var resultIds = state.ResultIds ?? [];
        var allowedKnowledge = state.FactAllowedKnowledge ?? [];
        var ruleAuthorityName = string.IsNullOrWhiteSpace(state.RuleAuthorityOwnerName)
            ? state.OwnerName
            : state.RuleAuthorityOwnerName;
        var linkedEffects = model.Paths.Where(path => path.SourceTransitionId == state.TransitionId)
            .OrderBy(path => path.EffectId, StringComparer.Ordinal).ToArray();

        var nodes = new List<StateRuleNodeResponse>
        {
            Node($"derived:{state.TransitionId}:source", $"{state.TransitionId}:source", "derived-explicit-field",
                "state-predicate", "Before", state.SourcePredicate, "known", "state", 100, "state",
                [state.StateCategory], Section("source", "Source predicate", Field("predicate", "Predicate", state.SourcePredicate))),
            Node($"node:{state.TransitionId}", state.TransitionId, "semantic-element", "transition", state.TransitionName,
                state.Trigger, "known", "transition", 200, "transition", ["Explicit transition"],
                Section("transition", "Transition", Field("trigger", "Trigger", state.Trigger),
                    Field("source", "Source", state.SourcePredicate), Field("target", "Target", state.TargetPredicate))),
            Node($"derived:{state.TransitionId}:target", $"{state.TransitionId}:target", "derived-explicit-field",
                "state-predicate", "After", state.TargetPredicate, "known", "state", 300, "state",
                ["Postcondition"], Section("target", "Target predicate", Field("predicate", "Predicate", state.TargetPredicate))),
            Node($"node:{state.FactId}", state.FactId, "semantic-element", "fact", state.FactName,
                state.FactAuthority, allowedKnowledge.Contains("unknown", StringComparer.OrdinalIgnoreCase) ? "unknown-capable" : "known",
                "logic", 400, "fact", [state.FactValueType, state.FactMutability],
                Section("fact", "Owned fact", Field("type", "Value type", state.FactValueType),
                    Field("authority", "Authority", state.FactAuthority),
                    Field("knowledge", "Permitted knowledge", Join(allowedKnowledge)))),
            Node($"node:{state.RuleId}", state.RuleId, "semantic-element", "rule", state.RuleName,
                state.RuleStatement, "known", "logic", 500, "rule", [state.RuleKind],
                Section("rule", "Governing rule", Field("kind", "Rule kind", state.RuleKind),
                    Field("statement", "Statement", state.RuleStatement), Field("authority", "Authority", ruleAuthorityName))),
            Node($"node:{state.InvariantId}", state.InvariantId, "semantic-element", "invariant", state.InvariantName,
                state.InvariantStatement, "known", "assurance", 600, "invariant", ["Must always hold"],
                Section("invariant", "Invariant", Field("statement", "Statement", state.InvariantStatement),
                    Field("falsifier", "Falsifying example", state.FalsifyingExample),
                    Field("proof", "Expected proof", Join(state.ProofExpectation)))),
        };

        foreach (var result in state.Results)
            nodes.Add(Node($"node:{result.Id}", result.Id, "semantic-element", "result", result.Name, result.Meaning,
                "known", "result", 700 + nodes.Count, "result", [result.Kind],
                Section("result", "Semantic result", Field("kind", "Result kind", result.Kind), Field("meaning", "Meaning", result.Meaning))));
        foreach (var path in linkedEffects)
            nodes.Add(Node($"node:{path.EffectId}", path.EffectId, "semantic-element", "effect", path.EffectName,
                path.EffectStatement, "known", "effect", 900 + nodes.Count, "effect", [path.EffectKind, path.BranchClassification],
                Section("effect", "Requested effect", Field("kind", "Effect kind", path.EffectKind),
                    Field("statement", "Effect", path.EffectStatement), Field("path", "Path", path.BranchName))));

        var edges = new List<StateRuleEdgeResponse>
        {
            Edge("flow:source-transition", "transitionsVia", nodes[0].Id, nodes[1].Id, state.Trigger, "solid", "derived-transition-field"),
            Edge("flow:transition-target", "producesState", nodes[1].Id, nodes[2].Id, "Target predicate", "solid", "derived-transition-field"),
        };
        if (changedFactIds.Contains(state.FactId, StringComparer.Ordinal))
            edges.Add(Edge($"edge:{state.TransitionId}:changes:{state.FactId}", "changesFact", nodes[1].Id,
                $"node:{state.FactId}", "Changes", "double", "semantic-transition-reference"));
        if (ruleIds.Contains(state.RuleId, StringComparer.Ordinal))
            edges.Add(Edge($"edge:{state.TransitionId}:evaluates:{state.RuleId}", "evaluatesRule", $"node:{state.RuleId}",
                nodes[1].Id, "Governs", "dashed", "semantic-transition-reference"));
        if (invariantIds.Contains(state.InvariantId, StringComparer.Ordinal))
            edges.Add(Edge($"edge:{state.TransitionId}:checks:{state.InvariantId}", "checksInvariant", nodes[1].Id,
                $"node:{state.InvariantId}", "Must preserve", "dotted", "semantic-transition-reference"));
        foreach (var result in state.Results.Where(result => resultIds.Contains(result.Id, StringComparer.Ordinal)))
            edges.Add(Edge($"edge:{state.TransitionId}:returns:{result.Id}", "returnsResult", nodes[1].Id,
                $"node:{result.Id}", result.Kind, "branch", "semantic-transition-reference"));
        foreach (var path in linkedEffects)
            edges.Add(Edge($"edge:{state.TransitionId}:effect:{path.EffectId}", "requestsEffect", nodes[1].Id,
                $"node:{path.EffectId}", path.BranchClassification, "effect", "semantic-path-reference"));

        var diagnostics = new List<LensDiagnosticResponse>
        {
            new("state-rule.events.unmodeled", "Info",
                "No typed EventDefinition exists in the current canonical model; this lens does not infer events from trigger text.", state.TransitionId),
        };
        if (linkedEffects.Length == 0)
            diagnostics.Add(new("state-rule.effects.unmodeled", "Info",
                "No path explicitly references this transition with a requested effect.", state.TransitionId));
        if (state.Values.Count == 0)
            diagnostics.Add(new("state-rule.values.unknown", "Warning",
                "The state has no explicit value catalog; source and target predicates remain authored statements only.", state.StateId));

        var orderedNodes = nodes.OrderBy(node => node.Order).ThenBy(node => node.Id, StringComparer.Ordinal).ToArray();
        var orderedEdges = edges.OrderBy(edge => edge.Id, StringComparer.Ordinal).ToArray();
        var transitionRows = new[] { new StateTransitionRowResponse(state.TransitionId, state.TransitionName,
            state.SourcePredicate, state.Trigger, [state.RuleName], [state.FactName], state.TargetPredicate,
            state.Results.ToArray(), linkedEffects.Select(path => path.EffectName).ToArray()) };
        var ruleRows = new[] { new StateRuleRowResponse(state.RuleId, state.RuleName, state.RuleKind,
            state.RuleStatement, ruleAuthorityName, [state.TransitionName], [state.FactName]) };
        var invariantRows = new[] { new StateInvariantResponse(state.InvariantId, state.InvariantName,
            state.InvariantStatement, state.FalsifyingExample, state.InvariantScopeIds ?? [], state.ProofExpectation,
            invariantIds.Contains(state.InvariantId, StringComparer.Ordinal) ? [state.TransitionName] : []) };
        var accessibility = orderedNodes.Select((node, index) => new LensAccessibilityItemResponse(node.Id,
            node.SemanticReference, node.Kind, $"{node.Kind}: {node.Title}", node.KnowledgeStatus, index + 1,
            orderedNodes.Length, orderedEdges.Count(edge => edge.TargetNodeId == node.Id),
            orderedEdges.Count(edge => edge.SourceNodeId == node.Id))).ToArray();
        var scope = new StateRuleScopeResponse(model.Project.Id, model.Project.Revision, state.StateId,
            state.StateName, state.StateCategory, state.OwnerId, state.OwnerName, state.Structure, state.Values);
        var orderedDiagnostics = diagnostics.OrderBy(item => item.Code, StringComparer.Ordinal).ToArray();
        string[] representations = ["graph", "transition-matrix", "rule-matrix", "invariant-panel"];
        var hash = Hash(scope, representations, orderedNodes, orderedEdges, transitionRows, ruleRows, invariantRows,
            orderedDiagnostics, accessibility);
        var projection = new StateRuleProjectionResponse($"state-rule:{stateId}:r{model.Project.Revision}:{hash[..12]}",
            ContractVersion, scope, hash, representations, orderedNodes, orderedEdges, transitionRows, ruleRows,
            invariantRows, orderedDiagnostics, accessibility);
        StateRuleProjectionValidator.EnsureValid(projection);
        return projection;
    }

    private static StateRuleNodeResponse Node(string id, string reference, string origin, string kind, string title,
        string detail, string knowledge, string column, int order, string shape, IReadOnlyList<string> badges,
        params LensInspectorSectionResponse[] inspector) =>
        new(id, reference, origin, kind, title, detail, knowledge, column, order, shape, badges, inspector);
    private static StateRuleEdgeResponse Edge(string id, string kind, string source, string target, string label,
        string pattern, string origin) => new(id, kind, source, target, label, pattern, origin);
    private static LensInspectorSectionResponse Section(string id, string label, params LensInspectorFieldResponse[] fields) => new(id, label, fields);
    private static LensInspectorFieldResponse Field(string key, string label, string value) => new(key, label, value, "known");
    private static string Join(IEnumerable<string> values) => string.Join(" · ", values.DefaultIfEmpty("Unknown"));

    private static string Hash(StateRuleScopeResponse scope, IReadOnlyList<string> representations,
        IReadOnlyList<StateRuleNodeResponse> nodes, IReadOnlyList<StateRuleEdgeResponse> edges,
        IReadOnlyList<StateTransitionRowResponse> transitions, IReadOnlyList<StateRuleRowResponse> rules,
        IReadOnlyList<StateInvariantResponse> invariants, IReadOnlyList<LensDiagnosticResponse> diagnostics,
        IReadOnlyList<LensAccessibilityItemResponse> accessibility)
    {
        var json = JsonSerializer.Serialize(new
        {
            ContractVersion,
            Scope = scope,
            Representations = representations,
            Nodes = nodes,
            Edges = edges,
            Transitions = transitions,
            Rules = rules,
            Invariants = invariants,
            Diagnostics = diagnostics,
            Accessibility = accessibility
        });
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(json)));
    }
}

public static class StateRuleProjectionValidator
{
    public static void EnsureValid(StateRuleProjectionResponse projection)
    {
        ArgumentNullException.ThrowIfNull(projection);
        var errors = new List<string>();
        var nodeIds = projection.Nodes.Select(node => node.Id).ToArray();
        if (nodeIds.Distinct(StringComparer.Ordinal).Count() != nodeIds.Length)
            errors.Add("State and Rule node identifiers must be unique.");
        var known = nodeIds.ToHashSet(StringComparer.Ordinal);
        foreach (var edge in projection.Edges)
        {
            if (!known.Contains(edge.SourceNodeId)) errors.Add($"Edge '{edge.Id}' has a missing source.");
            if (!known.Contains(edge.TargetNodeId)) errors.Add($"Edge '{edge.Id}' has a missing target.");
        }
        if (projection.Transitions.Count == 0) errors.Add("A State and Rule projection requires a transition row.");
        if (projection.Rules.Count == 0) errors.Add("A State and Rule projection requires a rule row.");
        if (projection.Invariants.Count == 0) errors.Add("A State and Rule projection requires an invariant panel.");
        if (projection.AccessibilityTree.Count != projection.Nodes.Count)
            errors.Add("Every State and Rule node requires one accessibility item.");
        if (errors.Count > 0) throw new InvalidOperationException(string.Join(Environment.NewLine, errors.Order(StringComparer.Ordinal)));
    }
}
