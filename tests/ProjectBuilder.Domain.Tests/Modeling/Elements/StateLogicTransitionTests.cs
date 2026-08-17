using System.Collections.Immutable;
using ProjectBuilder.Domain.Modeling.Elements;
using ProjectBuilder.Domain.Modeling.Primitives;
using ProjectBuilder.Domain.Modeling.Transitions;
using ProjectBuilder.Domain.Projects;

namespace ProjectBuilder.Domain.Tests.Modeling.Elements;

public sealed class StateLogicTransitionTests
{
    [Test]
    public void Project_creation_state_packet_advances_once_and_uses_typed_results()
    {
        var project = Project(5);
        var result = StateLogicTransition.Define(project, RevisionValue(5), Ids(), ProjectDraft(), 30,
            ChangeSet(30), Reason(), Now, "modeler");

        Assert.That(result, Is.TypeOf<DefineStateLogicTransitionResult.Accepted>());
        var accepted = (DefineStateLogicTransitionResult.Accepted)result;
        Assert.Multiple(() =>
        {
            Assert.That(accepted.Project.Revision.Value, Is.EqualTo(6));
            Assert.That(accepted.Definitions.State.Category, Is.EqualTo(StateCategory.Domain));
            Assert.That(accepted.Definitions.Transition.ChangedFactIds, Is.EqualTo(new[] { accepted.Definitions.Fact.Id }));
            Assert.That(accepted.Definitions.Results.Select(x => x.ResultKind), Is.EqualTo(new[] { SemanticResultKind.Success, SemanticResultKind.Invalid, SemanticResultKind.Denied, SemanticResultKind.Conflict }));
            Assert.That(accepted.ChangeSet.ChangeKind, Is.EqualTo("state-logic.defined"));
            Assert.That(accepted.ChangeSet.Operations.Length, Is.EqualTo(9));
        });
    }

    [Test]
    public void Pos_item_add_state_and_invariant_are_explicit_and_valid()
    {
        var project = Project(5);
        var result = StateLogicTransition.Define(project, RevisionValue(5), Ids(3), PosDraft(), 30,
            ChangeSet(31), Reason(), Now, "modeler");
        var accepted = (DefineStateLogicTransitionResult.Accepted)result;

        Assert.Multiple(() =>
        {
            Assert.That(accepted.Definitions.State.Values.Select(x => x.Value), Does.Contain("Active"));
            Assert.That(accepted.Definitions.Invariant.Statement.Value, Does.Contain("positive quantity"));
            Assert.That(accepted.Definitions.Transition.SourcePredicate.Value, Does.Contain("Active"));
            Assert.That(accepted.Definitions.Transition.TargetPredicate.Value, Does.Contain("priced line"));
        });
    }

    [Test]
    public void Presentation_fact_cannot_supply_domain_transition_truth()
    {
        var projectId = ProjectIdValue(1); var stateId = Element(1); var otherStateId = Element(2);
        var domainState = new StateDefinition(stateId, projectId, Name("Transaction"), StateCategory.Domain,
            [Term("Status")], [Term("Active")], Element(90), 0, Now, "modeler");
        var presentationFact = new FactDefinition(Element(3), projectId, otherStateId, Name("Selected tab"),
            Term("string"), Statement("The interface owns selection."), FactMutability.Transitioned,
            [KnowledgeStatus.Known], 1, Now, "modeler");
        var rule = new RuleDefinition(Element(4), projectId, stateId, Name("Item eligibility"), RuleKind.Eligibility,
            Statement("The item is sellable."), Element(90), 2, Now, "modeler");
        var invariant = new InvariantDefinition(Element(5), projectId, stateId, Name("Positive quantity"),
            Statement("Every active line has positive quantity."), [stateId], Statement("An active line has zero quantity."),
            [Term("Property test")], 3, Now, "modeler");
        var success = new SemanticResultDefinition(Element(6), projectId, stateId, Name("Item added"),
            SemanticResultKind.Success, Statement("The priced line is added."), 4, Now, "modeler");
        var transition = new TransitionDefinition(Element(7), projectId, stateId, Name("Add item"),
            Statement("Transaction is Active."), Statement("Priced product resolved."),
            Statement("Transaction contains the priced line."), [presentationFact.Id], [rule.Id], [invariant.Id],
            [success.Id], 5, Now, "modeler");

        var errors = StateLogicValidation.Validate(domainState, [presentationFact], [rule], [invariant], [success], transition);
        Assert.That(errors.Select(x => x.Code), Does.Contain("PB-STATE-005"));
    }

    private static StateLogicDraft ProjectDraft() => new(
        Name("Project definition state"), StateCategory.Domain, [Term("DefinitionStatus"), Term("Revision")],
        [Term("Unmodeled"), Term("Defined")], Element(90),
        Name("Project purpose recorded"), Term("boolean"), Statement("The Project aggregate owns accepted purpose truth."), FactMutability.Transitioned,
        Name("Project definition validity"), RuleKind.Validation, Statement("Name, purpose, intended outcome, and reason must be valid."), Element(90),
        Name("Project revision advances once"), Statement("An accepted creation advances the project to revision 1 exactly once."),
        Statement("One accepted creation produces more than one revision or no revision."), [Term("Transition example and idempotency property")],
        [Result("Created", SemanticResultKind.Success), Result("Invalid", SemanticResultKind.Invalid), Result("Denied", SemanticResultKind.Denied), Result("Conflict", SemanticResultKind.Conflict)],
        Name("Create project definition"), Statement("No project definition exists."), Statement("Authorized create-project intent is accepted."),
        Statement("A purpose-led project exists at revision 1."));

    private static StateLogicDraft PosDraft() => new(
        Name("Transaction state"), StateCategory.Domain, [Term("Status"), Term("Lines"), Term("Total")],
        [Term("Active"), Term("Completed")], Element(90),
        Name("Priced line present"), Term("boolean"), Statement("The Transaction aggregate owns merchandise-line truth."), FactMutability.Transitioned,
        Name("Sale eligibility"), RuleKind.Eligibility, Statement("Only a sellable product with an applicable price can be added."), Element(90),
        Name("Active line quantity"), Statement("Every active merchandise line has positive quantity."),
        Statement("An active merchandise line has zero quantity."), [Term("Property test across valid quantities")],
        [Result("ItemAdded", SemanticResultKind.Success), Result("SaleProhibited", SemanticResultKind.Denied), Result("Conflict", SemanticResultKind.Conflict)],
        Name("Add priced product line"), Statement("Transaction is Active and has no line for this attempt."),
        Statement("A priced product is resolved for an authorized operator."),
        Statement("Transaction remains Active and contains exactly one priced line for the attempt."));

    private static SemanticResultDraft Result(string name, SemanticResultKind kind) => new(Name(name), kind, Statement($"The semantic result is {name}."));
    private static StateLogicIds Ids(int resultCount = 4) => new(Element(10), Element(11), Element(12), Element(13), Element(14),
        Enumerable.Range(15, resultCount).Select(Element).ToImmutableArray());
    private static ProjectDefinition Project(long revision)
    {
        var created = ProjectDefinition.Create(ProjectIdValue(1), Accepted(WorkspaceId.Parse("0198ad00-0000-7000-8000-000000000700")), Name("Project Builder"),
            Accepted(ProjectPurpose.Create("Model system meaning.")), Accepted(IntendedOutcome.Create("A modeler can build a trustworthy definition.")),
            ChangeSet(1), Reason(), Now, "modeler");
        return ProjectDefinition.Restore(created.Id, created.WorkspaceId, created.Name, created.Purpose, created.IntendedOutcome, RevisionValue(revision), created.Creation);
    }
    private static readonly UtcTimestamp Now = UtcTimestamp.Create(new DateTimeOffset(2026, 8, 16, 0, 0, 0, TimeSpan.Zero));
    private static ElementName Name(string value) => Accepted(ElementName.Create(value));
    private static LogicStatement Statement(string value) => Accepted(LogicStatement.Create(value));
    private static LogicTerm Term(string value) => Accepted(LogicTerm.Create(value));
    private static ChangeReason Reason() => Accepted(ChangeReason.Create("Define explicit state and logic."));
    private static Revision RevisionValue(long value) => Accepted(Revision.Create(value));
    private static ProjectId ProjectIdValue(int seed) => Accepted(ProjectId.Parse($"0198ad00-0000-7000-8000-{seed:X12}"));
    private static ElementId Element(int seed) => Accepted(ElementId.Parse($"0198ad00-0000-7000-8500-{seed:X12}"));
    private static ChangeSetId ChangeSet(int seed) => Accepted(ChangeSetId.Parse($"0198ad00-0000-7000-9500-{seed:X12}"));
    private static T Accepted<T>(SemanticResult<T> result) where T : notnull => ((SemanticResult<T>.Accepted)result).Value;
}
