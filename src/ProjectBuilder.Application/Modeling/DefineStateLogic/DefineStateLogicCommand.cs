namespace ProjectBuilder.Application.Modeling.DefineStateLogic;

public sealed record DefineStateLogicCommand(
    string ProjectId, string ExpectedRevision, string OperationId,
    string StateName, string StateCategory, string StateStructure, string StateValues, string OwnerId,
    string FactName, string FactValueType, string FactAuthority, string FactMutability,
    string RuleName, string RuleKind, string RuleStatement, string RuleAuthorityOwnerId,
    string InvariantName, string InvariantStatement, string FalsifyingExample, string ProofExpectation,
    string SemanticResults, string TransitionName, string SourcePredicate, string Trigger,
    string TargetPredicate, string Reason);
