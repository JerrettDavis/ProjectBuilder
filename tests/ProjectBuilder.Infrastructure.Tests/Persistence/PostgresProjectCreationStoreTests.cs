using System.Collections.Immutable;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using ProjectBuilder.Application.Modeling;
using ProjectBuilder.Application.Portability;
using ProjectBuilder.Application.Projects.CreateProject;
using ProjectBuilder.Domain.Modeling.Elements;
using ProjectBuilder.Domain.Modeling.Primitives;
using ProjectBuilder.Domain.Modeling.Transitions;
using ProjectBuilder.Domain.Projects;
using ProjectBuilder.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace ProjectBuilder.Infrastructure.Tests.Persistence;

[Category("PostgreSQL")]
public sealed class PostgresProjectCreationStoreTests
{
    private static readonly bool[] OneAcceptedOneRejected = [true, false];
    private static readonly long[] ThreeRevisionHistory = [3, 2, 1];
    private static readonly int[] ThreeOperationCounts = [2, 1, 1];
    private static readonly string[] OutcomeOperationKinds = ["element.added", "relation.added"];
    private static readonly string[] ActorOutcomeKinds = ["actor", "outcome"];
    private static readonly int[] TwoOperationSequence = [0, 1];

    private PostgreSqlContainer database = null!;
    private DbContextOptions<FoundationDbContext> options = null!;
    private string backfilledOperationKind = string.Empty;
    private int backfilledOperationCount;

    [OneTimeSetUp]
    public async Task StartPostgreSql()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("PB_RUN_POSTGRES_TESTS"), "true", StringComparison.OrdinalIgnoreCase))
        {
            Assert.Ignore("Run through eng/verify to enable real PostgreSQL boundary tests.");
        }

        database = new PostgreSqlBuilder("postgres:18-alpine")
            .WithDatabase("projectbuilder_infrastructure")
            .WithUsername("projectbuilder")
            .WithPassword("local-test-only")
            .Build();
        await database.StartAsync();
        options = new DbContextOptionsBuilder<FoundationDbContext>()
            .UseNpgsql(database.GetConnectionString())
            .Options;
        await using var context = new FoundationDbContext(options);
        var migrator = context.Database.GetService<IMigrator>();
        await migrator.MigrateAsync("20260815223113_EnforceRelationCardinality");
        await context.Database.ExecuteSqlRawAsync("""
            INSERT INTO projects
                ("Id", "WorkspaceId", "Name", "NormalizedName", "Purpose", "IntendedOutcome", "CurrentRevision", "CreatedAt", "CreatedBy")
            VALUES
                ('0198ad00-0000-7000-8000-000000000099', '0198ad00-0000-7000-8000-000000000700',
                 'Migration fixture', 'MIGRATION FIXTURE', 'Prove operation backfill.', 'History remains reviewable.', 1,
                 '2026-08-16T02:00:00Z', 'migration-test');

            INSERT INTO project_change_sets
                ("Id", "ProjectId", "BaseRevision", "ResultRevision", "ElementId", "ChangeKind", "RequestFingerprint", "Reason", "OccurredAt", "CreatedBy")
            VALUES
                ('0198ad00-0000-7000-9000-000000000099', '0198ad00-0000-7000-8000-000000000099',
                 NULL, 1, NULL, 'project.created', repeat('a', 64), 'Create migration fixture.',
                 '2026-08-16T02:00:00Z', 'migration-test');
            """);
        await migrator.MigrateAsync();
        var migratedChange = await context.ProjectChangeSets.AsNoTracking().SingleAsync();
        var migratedOperation = await context.ProjectChangeOperations.AsNoTracking().SingleAsync();
        backfilledOperationKind = migratedOperation.Kind;
        backfilledOperationCount = migratedChange.OperationCount;
    }

    [OneTimeTearDown]
    public async Task StopPostgreSql() => await database.DisposeAsync();

    [SetUp]
    public async Task ResetDatabase()
    {
        await using var context = new FoundationDbContext(options);
        await context.ProjectCreations.ExecuteDeleteAsync();
        await context.Projects.ExecuteDeleteAsync();
    }

    [Test]
    public async Task Portable_import_is_atomic_byte_stable_and_refuses_a_stale_snapshot()
    {
        var codec = new ProjectBuilder.Infrastructure.Portability.JsonPortableProjectCodec();
        var accepted = (PortableProjectReadResult.Accepted)codec.Read(PortableFixture());
        await using var context = new FoundationDbContext(options);
        var store = new PostgresPortableProjectStore(context);
        var result = await store.ImportAsync(
            Guid.Parse("0198ad00-0000-7000-8000-000000000888"),
            Import(accepted, Guid.Parse("0198ad00-0000-7000-9000-000000000900")),
            CancellationToken.None);
        var retry = await store.ImportAsync(
            Guid.Parse("0198ad00-0000-7000-8000-000000000888"),
            Import(accepted, Guid.Parse("0198ad00-0000-7000-9000-000000000900")),
            CancellationToken.None);
        var exported = await store.ExportAsync(accepted.Document.Project.Id, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.TypeOf<PortableImportStoreResult.Imported>());
            Assert.That(retry, Is.EqualTo(result));
            Assert.That(((PortableExportStoreResult.Exported)exported).CanonicalJson, Is.EqualTo(accepted.CanonicalJson));
            Assert.That(context.Projects.Count(), Is.EqualTo(1));
            Assert.That(context.ModelElements.Count(), Is.EqualTo(2));
            Assert.That(context.ModelRelations.Count(), Is.EqualTo(1));
            Assert.That(context.ProjectChangeOperations.Count(), Is.EqualTo(4));
        });

        await context.Projects.ExecuteUpdateAsync(update => update.SetProperty(project => project.CurrentRevision, 2L));
        var stale = await store.ExportAsync(accepted.Document.Project.Id, CancellationToken.None);
        Assert.That(stale, Is.EqualTo(new PortableExportStoreResult.SnapshotStale(1, 2)));
    }

    [Test]
    public async Task Failed_portable_import_rolls_back_project_elements_relations_history_and_snapshot()
    {
        var codec = new ProjectBuilder.Infrastructure.Portability.JsonPortableProjectCodec();
        var accepted = (PortableProjectReadResult.Accepted)codec.Read(PortableFixture());
        var invalidDocument = accepted.Document with
        {
            Elements =
            [
                accepted.Document.Elements[0],
                accepted.Document.Elements[1] with { Order = accepted.Document.Elements[0].Order },
            ],
        };
        await using var context = new FoundationDbContext(options);
        var store = new PostgresPortableProjectStore(context);

        var result = await store.ImportAsync(
            Guid.Parse("0198ad00-0000-7000-8000-000000000888"),
            Import(accepted with { Document = invalidDocument }, Guid.Parse("0198ad00-0000-7000-9000-000000000901")),
            CancellationToken.None);

        await using var verification = new FoundationDbContext(options);
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.TypeOf<PortableImportStoreResult.DuplicateProject>());
            Assert.That(verification.Projects.Count(), Is.Zero);
            Assert.That(verification.ModelElements.Count(), Is.Zero);
            Assert.That(verification.ModelRelations.Count(), Is.Zero);
            Assert.That(verification.ProjectChangeSets.Count(), Is.Zero);
            Assert.That(verification.PortableProjectSnapshots.Count(), Is.Zero);
        });
    }

    [Test]
    public void Existing_change_sets_are_backfilled_with_one_typed_historical_operation()
    {
        Assert.Multiple(() =>
        {
            Assert.That(backfilledOperationKind, Is.EqualTo("project.created"));
            Assert.That(backfilledOperationCount, Is.EqualTo(1));
        });
    }

    [Test]
    public async Task Failed_change_set_insert_rolls_back_the_project_row()
    {
        await using var context = new FoundationDbContext(options);
        var store = new PostgresProjectCreationStore(context);
        var invalidForStorage = Project(1, "Rollback proof", new string('a', 201));

        Assert.ThrowsAsync<DbUpdateException>(async () =>
            await store.TrySaveAsync(invalidForStorage, new string('f', 64), CancellationToken.None));

        await using var verification = new FoundationDbContext(options);
        var projectCount = await verification.Projects.CountAsync();
        var changeSetCount = await verification.ProjectCreations.CountAsync();
        var operationCount = await verification.ProjectChangeOperations.CountAsync();
        Assert.Multiple(() =>
        {
            Assert.That(projectCount, Is.Zero);
            Assert.That(changeSetCount, Is.Zero);
            Assert.That(operationCount, Is.Zero);
        });
    }

    [Test]
    public async Task Concurrent_workspace_name_writes_commit_exactly_one_project()
    {
        await using var firstContext = new FoundationDbContext(options);
        await using var secondContext = new FoundationDbContext(options);
        var firstStore = new PostgresProjectCreationStore(firstContext);
        var secondStore = new PostgresProjectCreationStore(secondContext);

        var results = await Task.WhenAll(
            firstStore.TrySaveAsync(Project(10, "Shared name", "modeler-1"), new string('a', 64), CancellationToken.None).AsTask(),
            secondStore.TrySaveAsync(Project(20, "SHARED NAME", "modeler-2"), new string('b', 64), CancellationToken.None).AsTask());

        await using var verification = new FoundationDbContext(options);
        var projectCount = await verification.Projects.CountAsync();
        var changeSetCount = await verification.ProjectCreations.CountAsync();
        Assert.Multiple(() =>
        {
            Assert.That(results, Is.EquivalentTo(OneAcceptedOneRejected));
            Assert.That(projectCount, Is.EqualTo(1));
            Assert.That(changeSetCount, Is.EqualTo(1));
        });
    }

    [Test]
    public async Task Actor_and_beneficiary_linked_outcome_commit_as_revisioned_relational_model()
    {
        await using var context = new FoundationDbContext(options);
        var projects = new PostgresProjectCreationStore(context);
        var codec = new ProjectBuilder.Infrastructure.Portability.JsonPortableProjectCodec();
        var snapshots = new PortableProjectSnapshotProjector(context, codec);
        var elements = new PostgresProjectElementStore(context, snapshots);
        var project = Project(30, "Typed model", "modeler");
        Assert.That(await projects.TrySaveAsync(project, new string('c', 64), CancellationToken.None), Is.True);

        var actorTransition = (AddActorTransitionResult.Accepted)ProjectElementTransition.AddActor(
            project, Revision.Initial,
            Accepted(ElementId.Parse("0198ad00-0000-7000-8100-000000000030")),
            Accepted(ElementName.Create("Contributor")),
            Accepted(ContextualRole.Create("A person changing the repository.")),
            ActorKind.HumanRole,
            [Accepted(ActorStatement.Create("Verify the repository."))],
            [Accepted(ActorStatement.Create("Preserve invariants."))],
            ImmutableArray<ActorStatement>.Empty,
            ImmutableArray<ActorStatement>.Empty,
            0,
            Accepted(ChangeSetId.Parse("0198ad00-0000-7000-9100-000000000030")),
            Accepted(ChangeReason.Create("Add the beneficiary actor.")),
            UtcTimestamp.Create(new DateTimeOffset(2026, 8, 15, 22, 0, 0, TimeSpan.Zero)),
            "modeler");
        Assert.That(await elements.CommitActorAsync(actorTransition, new string('d', 64), CancellationToken.None), Is.TypeOf<ElementStoreCommitResult.Committed>());

        var reloaded = await projects.FindByIdAsync(project.Id, CancellationToken.None);
        var outcomeTransition = (AddOutcomeTransitionResult.Accepted)ProjectElementTransition.AddOutcome(
            reloaded!, Accepted(Revision.Create(2)),
            Accepted(ElementId.Parse("0198ad00-0000-7000-8200-000000000030")),
            Accepted(ElementName.Create("Repository verified")),
            Accepted(OutcomeStatement.Create("A relational invariant remains true.")),
            [Accepted(SuccessSignal.Create("Verification exits successfully."))],
            actorTransition.Actor,
            Accepted(RelationId.Parse("0198ad00-0000-7000-8300-000000000030")),
            1,
            Accepted(ChangeSetId.Parse("0198ad00-0000-7000-9200-000000000030")),
            Accepted(ChangeReason.Create("Add the observable outcome.")),
            UtcTimestamp.Create(new DateTimeOffset(2026, 8, 15, 22, 1, 0, TimeSpan.Zero)),
            "modeler");
        Assert.That(await elements.CommitOutcomeAsync(outcomeTransition, new string('e', 64), CancellationToken.None), Is.TypeOf<ElementStoreCommitResult.Committed>());

        var model = await elements.LoadModelAsync(project.Id, CancellationToken.None);
        var history = await elements.LoadChangeHistoryAsync(project.Id, CancellationToken.None);
        var finalProject = await projects.FindByIdAsync(project.Id, CancellationToken.None);
        var exported = await new PostgresPortableProjectStore(context).ExportAsync(project.Id.Value, CancellationToken.None);
        var acceptedExport = (PortableExportStoreResult.Exported)exported;
        var roundTrip = (PortableProjectReadResult.Accepted)codec.Read(acceptedExport.CanonicalJson);
        Assert.Multiple(() =>
        {
            Assert.That(finalProject!.Revision.Value, Is.EqualTo(3));
            Assert.That(model.Actors.Single().Name.Value, Is.EqualTo("Contributor"));
            Assert.That(model.Outcomes.Single().BeneficiaryName, Is.EqualTo("Contributor"));
            Assert.That(model.Outcomes.Single().Outcome.SuccessSignals.Single().Value, Is.EqualTo("Verification exits successfully."));
            Assert.That(model.Relations.Single().Relation.Kind, Is.EqualTo(ProjectBuilder.Domain.Modeling.Relations.ModelRelationKind.BenefitsFrom));
            Assert.That(history.Select(changeSet => changeSet.ResultRevision), Is.EqualTo(ThreeRevisionHistory));
            Assert.That(history.Select(changeSet => changeSet.OperationCount), Is.EqualTo(ThreeOperationCounts));
            Assert.That(history[0].Operations.Select(operation => operation.Kind),
                Is.EqualTo(OutcomeOperationKinds));
            Assert.That(history[0].Operations.Select(operation => operation.Sequence), Is.EqualTo(TwoOperationSequence));
            Assert.That(roundTrip.CanonicalJson, Is.EqualTo(acceptedExport.CanonicalJson));
            Assert.That(roundTrip.ContentHash, Is.EqualTo(acceptedExport.ContentHash));
            Assert.That(roundTrip.Document.Project.Revision, Is.EqualTo(3));
            Assert.That(roundTrip.Document.Project.IntendedOutcomeIds,
                Is.EqualTo(new[] { outcomeTransition.Outcome.Id.Value }));
            Assert.That(roundTrip.Document.Elements.Select(value => value.Kind),
                Is.EqualTo(ActorOutcomeKinds));
        });

        await using (var deletionContext = new FoundationDbContext(options))
        {
            var ownedOutcome = await deletionContext.ModelElements.SingleAsync(
                element => element.Id == outcomeTransition.Outcome.Id.Value);
            deletionContext.ModelElements.Remove(ownedOutcome);
            Assert.ThrowsAsync<DbUpdateException>(async () => await deletionContext.SaveChangesAsync());
        }

        var secondActorId = Guid.Parse("0198ad00-0000-7000-8400-000000000030");
        context.ModelElements.Add(new ModelElementRecord
        {
            Id = secondActorId,
            ProjectId = project.Id.Value,
            Kind = "actor",
            Name = "Reviewer",
            Description = "A second proposed beneficiary.",
            DefinitionStatus = "Defined",
            KnowledgeStatus = "Known",
            Order = 2,
            Version = 1,
            CreatedAt = project.Creation.OccurredAt.Value,
            CreatedBy = "modeler",
        });
        await context.SaveChangesAsync();
        context.ModelRelations.Add(new ModelRelationRecord
        {
            Id = Guid.Parse("0198ad00-0000-7000-8500-000000000030"),
            ProjectId = project.Id.Value,
            Kind = "benefitsFrom",
            SourceElementId = secondActorId,
            TargetElementId = outcomeTransition.Outcome.Id.Value,
            Version = 1,
            CreatedAt = project.Creation.OccurredAt.Value,
            CreatedBy = "modeler",
        });

        Assert.ThrowsAsync<DbUpdateException>(async () => await context.SaveChangesAsync());
    }

    [Test]
    public async Task Failed_multi_element_narrative_insert_rolls_back_every_element_and_revision()
    {
        await using var context = new FoundationDbContext(options);
        var projects = new PostgresProjectCreationStore(context);
        var elements = new PostgresProjectElementStore(context);
        var project = Project(40, "Narrative rollback", "modeler");
        Assert.That(await projects.TrySaveAsync(project, new string('a', 64), CancellationToken.None), Is.True);
        var actor = new ActorDefinition(
            Accepted(ElementId.Parse("0198ad00-0000-7000-8600-000000000040")), project.Id,
            Accepted(ElementName.Create("Modeler")), Accepted(ContextualRole.Create("Defines the model.")),
            ActorKind.HumanRole, [], [], [], [], 0, project.Creation.OccurredAt, "modeler");
        var outcome = new OutcomeDefinition(
            Accepted(ElementId.Parse("0198ad00-0000-7000-8601-000000000040")), project.Id,
            Accepted(ElementName.Create("Project is reviewable")), Accepted(OutcomeStatement.Create("A project is visible.")),
            [Accepted(SuccessSignal.Create("Revision 1 is visible."))], 1, project.Creation.OccurredAt, "modeler");
        var ids = new NarrativeIds(
            Accepted(ElementId.Parse("0198ad00-0000-7000-8610-000000000040")), Accepted(ElementId.Parse("0198ad00-0000-7000-8611-000000000040")),
            Accepted(ElementId.Parse("0198ad00-0000-7000-8612-000000000040")), Accepted(ElementId.Parse("0198ad00-0000-7000-8613-000000000040")),
            Accepted(ElementId.Parse("0198ad00-0000-7000-8614-000000000040")), Accepted(ElementId.Parse("0198ad00-0000-7000-8615-000000000040")),
            Accepted(ElementId.Parse("0198ad00-0000-7000-8616-000000000040")));
        var draft = new NarrativeDraft(
            Accepted(ElementName.Create("Create Project")), Accepted(NarrativeText.Create("A modeler has an intention.")), Accepted(NarrativeText.Create("A project is reviewable.")),
            Accepted(ElementName.Create("Create project")), ScenarioClassification.Happy, [Accepted(NarrativeFact.Create("Workspace exists."))],
            Accepted(NarrativeText.Create("The modeler submits.")), Accepted(NarrativeText.Create("Revision 1 is visible.")),
            Accepted(ElementName.Create("Capture definition")), Accepted(NarrativeText.Create("Accessible form.")), Accepted(NarrativeText.Create("Capture meaning.")),
            Accepted(ElementName.Create("Submit definition")), Accepted(NarrativeText.Create("Create project.")), Accepted(NarrativeText.Create("Validate and commit.")),
            Accepted(NarrativeText.Create("Revision 1 is shown.")), [Accepted(NarrativeFact.Create("Created"))]);
        var transition = (DefineNarrativeTransitionResult.Accepted)NarrativeTransition.Define(
            project, Revision.Initial, outcome, [actor], actor, actor, ids, draft, 0,
            Accepted(ChangeSetId.Parse("0198ad00-0000-7000-9610-000000000040")),
            Accepted(ChangeReason.Create("Prove rollback.")), project.Creation.OccurredAt, new string('x', 201));

        Assert.ThrowsAsync<DbUpdateException>(async () =>
            await elements.CommitNarrativeAsync(transition, new string('b', 64), CancellationToken.None));
        await using var verification = new FoundationDbContext(options);
        Assert.Multiple(() =>
        {
            Assert.That(verification.ModelElements.Count(), Is.Zero);
            Assert.That(verification.Projects.Single().CurrentRevision, Is.EqualTo(1));
            Assert.That(verification.ProjectChangeOperations.Count(), Is.EqualTo(1));
        });
    }

    [Test]
    public async Task Failed_path_packet_insert_rolls_back_conditions_effect_recovery_and_revision()
    {
        await using var context = new FoundationDbContext(options);
        var projects = new PostgresProjectCreationStore(context);
        var elements = new PostgresProjectElementStore(context);
        var project = Project(50, "Path rollback", "modeler");
        Assert.That(await projects.TrySaveAsync(project, new string('c', 64), CancellationToken.None), Is.True);
        var scenarioId = Accepted(ElementId.Parse("0198ad00-0000-7000-8700-000000000050"));
        context.ModelElements.Add(new ModelElementRecord
        {
            Id = scenarioId.Value,
            ProjectId = project.Id.Value,
            Kind = "scenario",
            Name = "Source scenario",
            Description = "Persistence parent fixture.",
            DefinitionStatus = "Defined",
            KnowledgeStatus = "Known",
            Order = 0,
            Version = 1,
            CreatedAt = project.Creation.OccurredAt.Value,
            CreatedBy = "modeler",
        });
        await context.SaveChangesAsync();

        var stateId = Accepted(ElementId.Parse("0198ad00-0000-7000-8702-000000000050"));
        var terminalResult = new SemanticResultDefinition(
            Accepted(ElementId.Parse("0198ad00-0000-7000-8701-000000000050")), project.Id,
            stateId,
            Accepted(ElementName.Create("Invalid")), SemanticResultKind.Invalid,
            Accepted(LogicStatement.Create("Meaning was rejected.")), 1, project.Creation.OccurredAt, "modeler");
        var recoveryResult = new SemanticResultDefinition(
            Accepted(ElementId.Parse("0198ad00-0000-7000-8703-000000000050")), project.Id,
            stateId, Accepted(ElementName.Create("Created")), SemanticResultKind.Success,
            Accepted(LogicStatement.Create("Corrected meaning was accepted.")), 2, project.Creation.OccurredAt, "modeler");
        var ids = new PathIds(
            Accepted(ElementId.Parse("0198ad00-0000-7000-8710-000000000050")),
            Accepted(ElementId.Parse("0198ad00-0000-7000-8711-000000000050")),
            Accepted(ElementId.Parse("0198ad00-0000-7000-8712-000000000050")),
            Accepted(ElementId.Parse("0198ad00-0000-7000-8713-000000000050")),
            Accepted(ElementId.Parse("0198ad00-0000-7000-8714-000000000050")));
        var draft = new PathDraft(
            scenarioId, Accepted(ElementId.Parse("0198ad00-0000-7000-8720-000000000050")),
            terminalResult.Id, recoveryResult.Id, Accepted(ElementId.Parse("0198ad00-0000-7000-8721-000000000050")),
            Accepted(ElementName.Create("Invalid definition")), PathClassification.Exceptional,
            Accepted(ElementName.Create("Definition is invalid")), ConditionKind.Branch,
            Accepted(LogicStatement.Create("Submitted meaning is invalid.")), [], [],
            [Accepted(LogicTerm.Create("Return findings"))], Accepted(LogicStatement.Create("Revision is unchanged.")),
            Accepted(LogicStatement.Create("The modeler sees findings.")), Accepted(ElementName.Create("Present findings")),
            EffectKind.Observation, Accepted(LogicStatement.Create("Present validation findings.")),
            Accepted(ElementName.Create("Correct and retry")), RecoveryStrategy.CorrectAndRetry,
            Accepted(ElementName.Create("Correction chosen")), Accepted(LogicStatement.Create("The modeler corrects meaning.")),
            [Accepted(LogicTerm.Create("Correct fields"))], Accepted(LogicStatement.Create("Meaning is eligible.")),
            Accepted(LogicStatement.Create("The modeler can resubmit.")), Accepted(LogicStatement.Create("Retry after correction.")),
            Accepted(LogicStatement.Create("Corrected intent uses a new operation identity.")),
            Accepted(LogicStatement.Create("Exit after success or cancellation.")),
            Accepted(LogicStatement.Create("No rejected mutation requires reconciliation.")));
        var transition = (DefinePathTransitionResult.Accepted)PathTransition.Define(
            project, Revision.Initial, ids, draft, terminalResult, recoveryResult, 1,
            Accepted(ChangeSetId.Parse("0198ad00-0000-7000-9710-000000000050")),
            Accepted(ChangeReason.Create("Prove path rollback.")), project.Creation.OccurredAt,
            new string('x', 201));

        Assert.ThrowsAsync<DbUpdateException>(async () =>
            await elements.CommitPathAsync(transition, new string('d', 64), CancellationToken.None));
        await using var verification = new FoundationDbContext(options);
        Assert.Multiple(() =>
        {
            Assert.That(verification.ModelElements.Count(), Is.EqualTo(1));
            Assert.That(verification.PathPayloads.Count(), Is.Zero);
            Assert.That(verification.Projects.Single().CurrentRevision, Is.EqualTo(1));
            Assert.That(verification.ProjectChangeOperations.Count(), Is.EqualTo(1));
        });
    }

    private static ProjectDefinition Project(int seed, string name, string createdBy) =>
        ProjectDefinition.Create(
            Accepted(ProjectId.Parse($"0198ad00-0000-7000-8000-{seed:X12}")),
            Accepted(WorkspaceId.Parse("0198ad00-0000-7000-8000-000000000888")),
            Accepted(ElementName.Create(name)),
            Accepted(ProjectPurpose.Create("Prove PostgreSQL transaction behavior.")),
            Accepted(IntendedOutcome.Create("A relational invariant remains true.")),
            Accepted(ChangeSetId.Parse($"0198ad00-0000-7000-9000-{seed:X12}")),
            Accepted(ChangeReason.Create("Create persistence proof.")),
            UtcTimestamp.Create(new DateTimeOffset(2026, 8, 15, 21, 0, seed % 60, TimeSpan.Zero)),
            createdBy);

    private static PortableProjectImport Import(PortableProjectReadResult.Accepted accepted, Guid changeSetId) =>
        new(
            accepted.Document,
            accepted.CanonicalJson,
            accepted.ContentHash,
            "Import the portable persistence fixture.",
            new ProjectActor("modeler"),
            new DateTimeOffset(2026, 8, 16, 4, 30, 0, TimeSpan.Zero),
            changeSetId);

    private static string PortableFixture() => File.ReadAllText(Path.Combine(
        TestContext.CurrentContext.TestDirectory, "Fixtures", "example-importable-project.project-builder.json"));

    private static T Accepted<T>(SemanticResult<T> result)
        where T : notnull =>
        ((SemanticResult<T>.Accepted)result).Value;
}
