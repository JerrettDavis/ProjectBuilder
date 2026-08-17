using System.Collections.Immutable;
using ProjectBuilder.Domain.Modeling.Elements;
using ProjectBuilder.Domain.Modeling.Primitives;
using ProjectBuilder.Domain.Projects;

namespace ProjectBuilder.Domain.Modeling.Transitions;

public sealed record SemanticResultDraft(ElementName Name, SemanticResultKind Kind, LogicStatement Meaning);
public sealed record StateLogicIds(ElementId StateId, ElementId FactId, ElementId RuleId,
    ElementId InvariantId, ElementId TransitionId, ImmutableArray<ElementId> ResultIds);
public sealed record StateLogicDraft(
    ElementName StateName, StateCategory StateCategory, ImmutableArray<LogicTerm> StateStructure,
    ImmutableArray<LogicTerm> StateValues, ElementId OwnerId,
    ElementName FactName, LogicTerm FactValueType, LogicStatement FactAuthority, FactMutability FactMutability,
    ElementName RuleName, RuleKind RuleKind, LogicStatement RuleStatement, ElementId RuleAuthorityOwnerId,
    ElementName InvariantName, LogicStatement InvariantStatement, LogicStatement FalsifyingExample,
    ImmutableArray<LogicTerm> ProofExpectation,
    ImmutableArray<SemanticResultDraft> Results,
    ElementName TransitionName, LogicStatement SourcePredicate, LogicStatement Trigger, LogicStatement TargetPredicate);

public sealed record StateLogicDefinitionSet(StateDefinition State, FactDefinition Fact, RuleDefinition Rule,
    InvariantDefinition Invariant, ImmutableArray<SemanticResultDefinition> Results, TransitionDefinition Transition)
{
    public ImmutableArray<ModelElement> Elements => [State, Fact, Rule, Invariant, .. Results, Transition];
}

public abstract record DefineStateLogicTransitionResult
{
    private DefineStateLogicTransitionResult() { }
    public sealed record Accepted(ProjectDefinition Project, StateLogicDefinitionSet Definitions, ProjectModelChangeSet ChangeSet)
        : DefineStateLogicTransitionResult;
    public sealed record Conflict(Revision Expected, Revision Actual, ImmutableArray<SemanticConflict> Conflicts) : DefineStateLogicTransitionResult;
    public sealed record Invalid(IReadOnlyList<SemanticError> Errors) : DefineStateLogicTransitionResult;
}

public static class StateLogicValidation
{
    public static IReadOnlyList<SemanticError> Validate(
        StateDefinition state, IEnumerable<FactDefinition> facts, IEnumerable<RuleDefinition> rules,
        IEnumerable<InvariantDefinition> invariants, IEnumerable<SemanticResultDefinition> results,
        TransitionDefinition transition)
    {
        var errors = new List<SemanticError>();
        var factArray = facts.ToArray(); var ruleArray = rules.ToArray(); var invariantArray = invariants.ToArray(); var resultArray = results.ToArray();
        if (transition.ParentId != state.Id) errors.Add(new("PB-STATE-002", "A transition must belong to its explicit state definition."));
        if (transition.ChangedFactIds.IsDefaultOrEmpty || transition.ChangedFactIds.Any(id => factArray.All(x => x.Id != id)))
            errors.Add(new("PB-STATE-001", "A transition may change only facts defined in its state scope."));
        if (factArray.Any(x => x.ParentId != state.Id))
            errors.Add(new(state.Category == StateCategory.Domain ? "PB-STATE-005" : "PB-STATE-001", "A fact from another state category cannot supply transition truth implicitly."));
        if (transition.RuleIds.Any(id => ruleArray.All(x => x.Id != id)) || ruleArray.Any(x => x.ParentId != state.Id))
            errors.Add(new("PB-STATE-006", "Every evaluated rule must be explicit and owned by the transition state scope."));
        if (transition.InvariantIds.Any(id => invariantArray.All(x => x.Id != id)) || invariantArray.Any(x => !x.ScopeIds.Contains(state.Id)))
            errors.Add(new("PB-STATE-003", "Every invariant must explicitly include the owning state scope."));
        if (transition.ResultIds.IsDefaultOrEmpty || transition.ResultIds.Any(id => resultArray.All(x => x.Id != id)))
            errors.Add(new("PB-STATE-010", "A transition requires explicit typed semantic results."));
        if (resultArray.Select(x => x.ResultKind).Distinct().Count() != resultArray.Length)
            errors.Add(new("PB-STATE-010", "Semantic result kinds must be unique within a transition."));
        return errors;
    }
}

public static class StateLogicTransition
{
    public static DefineStateLogicTransitionResult Define(ProjectDefinition project, Revision expectedRevision,
        StateLogicIds ids, StateLogicDraft draft, int firstOrder, ChangeSetId changeSetId,
        ChangeReason reason, UtcTimestamp occurredAt, string createdBy)
    {
        if (project.Revision != expectedRevision)
            return new DefineStateLogicTransitionResult.Conflict(
                expectedRevision, project.Revision,
                ProjectChangeSetTransition.RevisionConflicts(expectedRevision, project.Revision));
        if (ids.ResultIds.Length != draft.Results.Length || ids.ResultIds.IsDefaultOrEmpty)
            return new DefineStateLogicTransitionResult.Invalid([new("PB-STATE-010", "Every semantic result requires one stable identifier.")]);

        var state = new StateDefinition(ids.StateId, project.Id, draft.StateName, draft.StateCategory,
            draft.StateStructure, draft.StateValues, draft.OwnerId, firstOrder, occurredAt, createdBy);
        var fact = new FactDefinition(ids.FactId, project.Id, state.Id, draft.FactName, draft.FactValueType,
            draft.FactAuthority, draft.FactMutability, [KnowledgeStatus.Known, KnowledgeStatus.Unknown, KnowledgeStatus.Assumed],
            firstOrder + 1, occurredAt, createdBy);
        var rule = new RuleDefinition(ids.RuleId, project.Id, state.Id, draft.RuleName, draft.RuleKind,
            draft.RuleStatement, draft.RuleAuthorityOwnerId, firstOrder + 2, occurredAt, createdBy);
        var invariant = new InvariantDefinition(ids.InvariantId, project.Id, state.Id, draft.InvariantName,
            draft.InvariantStatement, [state.Id], draft.FalsifyingExample, draft.ProofExpectation,
            firstOrder + 3, occurredAt, createdBy);
        var results = draft.Results.Select((result, index) => new SemanticResultDefinition(ids.ResultIds[index],
            project.Id, state.Id, result.Name, result.Kind, result.Meaning, firstOrder + 4 + index,
            occurredAt, createdBy)).ToImmutableArray();
        var transition = new TransitionDefinition(ids.TransitionId, project.Id, state.Id, draft.TransitionName,
            draft.SourcePredicate, draft.Trigger, draft.TargetPredicate, [fact.Id], [rule.Id], [invariant.Id],
            results.Select(x => x.Id).ToImmutableArray(), firstOrder + 4 + results.Length, occurredAt, createdBy);
        var validation = StateLogicValidation.Validate(state, [fact], [rule], [invariant], results, transition);
        if (validation.Count > 0) return new DefineStateLogicTransitionResult.Invalid(validation);

        var definitions = new StateLogicDefinitionSet(state, fact, rule, invariant, results, transition);
        var committed = ProjectChangeSetTransition.Commit(
            project,
            expectedRevision,
            new(changeSetId, state.Id, "state-logic.defined", reason,
                ProjectChangeSetTransition.AddedElements(definitions.Elements)),
            occurredAt,
            createdBy);
        var accepted = (ProjectChangeSetTransitionResult.Accepted)committed;
        return new DefineStateLogicTransitionResult.Accepted(accepted.Project, definitions, accepted.ChangeSet);
    }
}
