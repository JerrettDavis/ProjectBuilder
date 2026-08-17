using ProjectBuilder.Domain.Modeling.Elements;
using ProjectBuilder.Domain.Modeling.Primitives;
using ProjectBuilder.Domain.Modeling.Transitions;
using ProjectBuilder.Domain.Projects;

namespace ProjectBuilder.Domain.Tests.Modeling.Elements;

public sealed class PathTransitionTests
{
    [Test]
    public void Exceptional_branch_and_owned_recovery_advance_one_revision()
    {
        var project = Project(6);
        var invalid = Result(70, "Invalid", SemanticResultKind.Invalid);
        var created = Result(71, "Created", SemanticResultKind.Success);

        var result = PathTransition.Define(project, RevisionValue(6), Ids(), Draft(), invalid, created,
            40, ChangeSet(40), Reason(), Now, "modeler");

        Assert.That(result, Is.TypeOf<DefinePathTransitionResult.Accepted>());
        var accepted = (DefinePathTransitionResult.Accepted)result;
        Assert.Multiple(() =>
        {
            Assert.That(accepted.Project.Revision.Value, Is.EqualTo(7));
            Assert.That(accepted.Definitions.Branch.Classification, Is.EqualTo(PathClassification.Exceptional));
            Assert.That(accepted.Definitions.Branch.RecoveryPathId, Is.EqualTo(accepted.Definitions.Recovery.Id));
            Assert.That(accepted.Definitions.Recovery.RecoversFromPathId, Is.EqualTo(accepted.Definitions.Branch.Id));
            Assert.That(accepted.Definitions.Recovery.RecoveryStrategy, Is.EqualTo(RecoveryStrategy.CorrectAndRetry));
            Assert.That(accepted.ChangeSet.ChangeKind, Is.EqualTo("path.defined"));
            Assert.That(accepted.ChangeSet.Operations.Length, Is.EqualTo(5));
        });
    }

    [Test]
    public void Pos_non_happy_catalog_closes_unknown_unavailable_prohibited_duplicate_and_cancellation_paths()
    {
        var projectId = ProjectIdValue(1);
        var scenarioId = Element(50);
        var transitionId = Element(51);
        var ownerId = Element(52);
        var cases = new[]
        {
            new PosCase("Unknown item", PathClassification.Exceptional, SemanticResultKind.Invalid, "UnknownProduct", "Transaction remains Active v12."),
            new PosCase("Price book unavailable", PathClassification.Exceptional, SemanticResultKind.Unavailable, "DependencyUnavailable", "Transaction remains Active v12."),
            new PosCase("Sale prohibited", PathClassification.Exceptional, SemanticResultKind.Denied, "SaleProhibited", "Transaction remains Active v12."),
            new PosCase("Duplicate device delivery", PathClassification.Alternate, SemanticResultKind.Duplicate, "DuplicateIgnored", "Transaction remains Active v13 with one line."),
            new PosCase("Clerk cancellation", PathClassification.Cancellation, SemanticResultKind.Cancelled, "Cancelled", "Transaction remains Active v12."),
        };
        var paths = new List<PathDefinition>();
        var conditions = new List<ConditionDefinition>();
        var results = new List<SemanticResultDefinition>();
        var next = 100;
        foreach (var item in cases)
        {
            var pathId = Element(next++); var conditionId = Element(next++); var resultId = Element(next++);
            paths.Add(new(pathId, projectId, scenarioId, Name(item.Name), item.Classification, transitionId,
                [conditionId], [Term("Evaluate the explicit branch condition"), Term("Present the typed result")],
                resultId, null, Statement(item.TerminalState), Statement($"The clerk observes {item.ResultName}."),
                ownerId, null, null, null, null, null, null, null, next, Now, "modeler"));
            conditions.Add(new(conditionId, projectId, pathId, Name($"{item.Name} condition"),
                item.Classification == PathClassification.Cancellation ? ConditionKind.Cancellation : ConditionKind.Branch,
                Statement($"The POS detects {item.Name.ToLowerInvariant()}."), [], [], next + 1, Now, "modeler"));
            results.Add(new(resultId, projectId, Element(60), Name(item.ResultName), item.ResultKind,
                Statement($"The result is {item.ResultName}."), next + 2, Now, "modeler"));
            next += 3;
        }

        var errors = PathValidation.Validate(paths, conditions, [], results);

        Assert.Multiple(() =>
        {
            Assert.That(errors, Is.Empty);
            Assert.That(paths, Has.Count.EqualTo(5));
            Assert.That(paths.All(path => path.TerminalState.Value.Contains("Transaction remains", StringComparison.Ordinal)), Is.True);
            Assert.That(results.Select(result => result.ResultKind),
                Is.EquivalentTo(new[] { SemanticResultKind.Invalid, SemanticResultKind.Unavailable,
                    SemanticResultKind.Denied, SemanticResultKind.Duplicate, SemanticResultKind.Cancelled }));
        });
    }

    [Test]
    public void External_effect_without_failure_path_is_rejected()
    {
        var definition = Definitions();
        var effect = new EffectDefinition(Element(90), ProjectIdValue(1), definition.Path.Id,
            Name("Resolve price"), EffectKind.ExternalInteraction, Statement("Ask the price authority."),
            null, 9, Now, "modeler");

        var errors = PathValidation.Validate([definition.Path], [definition.Condition], [effect], [definition.Result]);

        Assert.That(errors.Select(error => error.Code), Does.Contain("PB-PATH-002"));
    }

    [Test]
    public void Retry_without_idempotency_analysis_is_rejected()
    {
        var definition = Definitions(recoveryStrategy: RecoveryStrategy.Retry);

        var errors = PathValidation.Validate([definition.Path], [definition.Condition], [], [definition.Result]);

        Assert.That(errors.Select(error => error.Code), Does.Contain("PB-PATH-003"));
    }

    [Test]
    public void Degraded_path_without_exit_and_reconciliation_is_rejected()
    {
        var definition = Definitions(classification: PathClassification.Degraded);

        var errors = PathValidation.Validate([definition.Path], [definition.Condition], [], [definition.Result]);

        Assert.That(errors.Select(error => error.Code), Does.Contain("PB-PATH-007"));
    }

    private static (PathDefinition Path, ConditionDefinition Condition, SemanticResultDefinition Result) Definitions(
        PathClassification classification = PathClassification.Exceptional,
        RecoveryStrategy? recoveryStrategy = null)
    {
        var projectId = ProjectIdValue(1); var pathId = Element(80); var conditionId = Element(81);
        var result = Result(82, "Unavailable", SemanticResultKind.Unavailable);
        var path = new PathDefinition(pathId, projectId, Element(50), Name("Price unavailable"), classification,
            Element(51), [conditionId], [Term("Present unavailable result")], result.Id, null,
            Statement("Transaction remains Active."), Statement("The clerk sees that pricing is unavailable."),
            Element(52), null, null, recoveryStrategy, null, null, null, null, 1, Now, "modeler");
        var condition = new ConditionDefinition(conditionId, projectId, pathId, Name("Price lookup failed"),
            ConditionKind.Branch, Statement("No approved price authority is available."), [], [], 2, Now, "modeler");
        return (path, condition, result);
    }

    private static PathDraft Draft() => new(
        Element(20), Element(21), Element(70), Element(71), Element(22),
        Name("Invalid project definition"), PathClassification.Exceptional,
        Name("Definition is invalid"), ConditionKind.Branch,
        Statement("One or more purpose-led project fields fail semantic validation."), [], [],
        [Term("Validate submitted meaning"), Term("Return field-level findings")],
        Statement("No project definition exists and the revision is unchanged."),
        Statement("The modeler sees an error summary and preserved input."),
        Name("Present validation findings"), EffectKind.Observation,
        Statement("Present actionable validation findings without changing domain state."),
        Name("Correct and resubmit"), RecoveryStrategy.CorrectAndRetry,
        Name("Modeler chooses to correct"), Statement("The modeler retains authority and corrects the rejected meaning."),
        [Term("Correct invalid fields"), Term("Resubmit with a new operation identity")],
        Statement("The corrected definition is eligible for the Create Project transition."),
        Statement("The modeler can submit the corrected definition."),
        Statement("One corrected submission per new operation identity; stop after the modeler cancels."),
        Statement("A rejected operation never commits; a new operation identity represents the corrected intent."),
        Statement("Exit when the project is created or the modeler cancels."),
        Statement("No reconciliation is required because rejection produced no domain mutation."));

    private static PathIds Ids() => new(Element(30), Element(31), Element(32), Element(33), Element(34));
    private static SemanticResultDefinition Result(int seed, string name, SemanticResultKind kind) =>
        new(Element(seed), ProjectIdValue(1), Element(60), Name(name), kind,
            Statement($"The semantic result is {name}."), seed, Now, "modeler");
    private static ProjectDefinition Project(long revision)
    {
        var created = ProjectDefinition.Create(ProjectIdValue(1),
            Accepted(WorkspaceId.Parse("0198ad00-0000-7000-8000-000000000700")), Name("Project Builder"),
            Accepted(ProjectPurpose.Create("Model system meaning.")),
            Accepted(IntendedOutcome.Create("A modeler can build a trustworthy definition.")),
            ChangeSet(1), Reason(), Now, "modeler");
        return ProjectDefinition.Restore(created.Id, created.WorkspaceId, created.Name, created.Purpose,
            created.IntendedOutcome, RevisionValue(revision), created.Creation);
    }

    private sealed record PosCase(string Name, PathClassification Classification,
        SemanticResultKind ResultKind, string ResultName, string TerminalState);
    private static readonly UtcTimestamp Now = UtcTimestamp.Create(new DateTimeOffset(2026, 8, 16, 1, 0, 0, TimeSpan.Zero));
    private static ElementName Name(string value) => Accepted(ElementName.Create(value));
    private static LogicStatement Statement(string value) => Accepted(LogicStatement.Create(value));
    private static LogicTerm Term(string value) => Accepted(LogicTerm.Create(value));
    private static ChangeReason Reason() => Accepted(ChangeReason.Create("Define explicit branch and recovery behavior."));
    private static Revision RevisionValue(long value) => Accepted(Revision.Create(value));
    private static ProjectId ProjectIdValue(int seed) => Accepted(ProjectId.Parse($"0198ad00-0000-7000-8000-{seed:X12}"));
    private static ElementId Element(int seed) => Accepted(ElementId.Parse($"0198ad00-0000-7000-8600-{seed:X12}"));
    private static ChangeSetId ChangeSet(int seed) => Accepted(ChangeSetId.Parse($"0198ad00-0000-7000-9600-{seed:X12}"));
    private static T Accepted<T>(SemanticResult<T> result) where T : notnull => ((SemanticResult<T>.Accepted)result).Value;
}
