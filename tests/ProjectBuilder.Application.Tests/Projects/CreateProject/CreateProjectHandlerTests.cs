using ProjectBuilder.Application.Projects.CreateProject;
using ProjectBuilder.Domain.Modeling.Primitives;
using ProjectBuilder.Domain.Projects;

namespace ProjectBuilder.Application.Tests.Projects.CreateProject;

public sealed class CreateProjectHandlerTests
{
    private static readonly string[] InvalidCommandErrorCodes =
    [
        "identity.format",
        "identity.format",
        "name.required",
        "project.purpose.required",
        "project.intended_outcome.required",
        "reason.too_short",
    ];

    private static readonly ProjectId GeneratedProjectId = Accepted(
        ProjectId.Parse("0198ad00-0000-7000-8000-000000000501"));

    private static readonly UtcTimestamp Now = UtcTimestamp.Create(
        new DateTimeOffset(2026, 8, 15, 20, 30, 0, TimeSpan.Zero));

    [Test]
    public async Task Authorized_creation_returns_revision_one_and_allowed_next_action()
    {
        var store = new RecordingStore();
        var handler = CreateHandler(store, ProjectCreationAuthorization.Allowed);

        var result = await handler.HandleAsync(ValidCommand(), new ProjectActor("modeler-1"));

        Assert.That(result, Is.TypeOf<CreateProjectResult.Created>());
        var created = (CreateProjectResult.Created)result;
        Assert.Multiple(() =>
        {
            Assert.That(created.Project.Id, Is.EqualTo(GeneratedProjectId.ToString()));
            Assert.That(created.Project.Revision, Is.EqualTo(1));
            Assert.That(created.Project.CreationReason, Is.EqualTo("Create the project definition."));
            Assert.That(created.Project.CreatedAt, Is.EqualTo("2026-08-15T20:30:00.0000000Z"));
            Assert.That(created.AllowedNextAction, Does.Contain("actors"));
            Assert.That(store.Saved, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public async Task Blank_values_return_all_semantic_errors_without_persistence()
    {
        var store = new RecordingStore();
        var handler = CreateHandler(store, ProjectCreationAuthorization.Allowed);
        var command = new CreateProjectCommand("", "", " ", "", " ", "x");

        var result = await handler.HandleAsync(command, new ProjectActor("modeler-1"));

        Assert.That(result, Is.TypeOf<CreateProjectResult.Invalid>());
        var invalid = (CreateProjectResult.Invalid)result;
        Assert.Multiple(() =>
        {
            Assert.That(invalid.Errors.Select(error => error.Code), Is.EquivalentTo(InvalidCommandErrorCodes));
            Assert.That(store.Saved, Is.Empty);
        });
    }

    [Test]
    public async Task Unauthorized_actor_is_denied_before_store_access()
    {
        var store = new RecordingStore();
        var handler = CreateHandler(store, ProjectCreationAuthorization.Denied("Workspace membership is required."));

        var result = await handler.HandleAsync(ValidCommand(), new ProjectActor("outsider"));

        Assert.That(result, Is.EqualTo(new CreateProjectResult.Denied("Workspace membership is required.")));
        Assert.That(store.CallCount, Is.EqualTo(0));
    }

    [Test]
    public async Task Exact_idempotent_retry_returns_the_original_project_without_a_second_save()
    {
        var store = new RecordingStore();
        var handler = CreateHandler(store, ProjectCreationAuthorization.Allowed);

        var first = await handler.HandleAsync(ValidCommand(), new ProjectActor("modeler-1"));
        var retry = await handler.HandleAsync(ValidCommand(), new ProjectActor("modeler-1"));

        Assert.That(first, Is.EqualTo(retry));
        Assert.That(store.Saved, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task Reusing_an_operation_for_different_content_returns_conflict()
    {
        var store = new RecordingStore();
        var handler = CreateHandler(store, ProjectCreationAuthorization.Allowed);
        await handler.HandleAsync(ValidCommand(), new ProjectActor("modeler-1"));
        var changed = ValidCommand() with { Purpose = "A different semantic purpose." };

        var result = await handler.HandleAsync(changed, new ProjectActor("modeler-1"));

        Assert.That(result, Is.TypeOf<CreateProjectResult.IdempotencyConflict>());
        Assert.That(store.Saved, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task Duplicate_name_in_the_workspace_returns_a_named_result()
    {
        var store = new RecordingStore { DuplicateName = true };
        var handler = CreateHandler(store, ProjectCreationAuthorization.Allowed);

        var result = await handler.HandleAsync(ValidCommand(), new ProjectActor("modeler-1"));

        Assert.That(result, Is.EqualTo(new CreateProjectResult.DuplicateName("Project Builder")));
        Assert.That(store.Saved, Is.Empty);
    }

    private static CreateProjectHandler CreateHandler(
        RecordingStore store,
        ProjectCreationAuthorization authorization) =>
        new(
            store,
            new FixedAuthorizer(authorization),
            new FixedIdentitySource(GeneratedProjectId),
            new FixedClock(Now));

    private static CreateProjectCommand ValidCommand() =>
        new(
            "0198ad00-0000-7000-8000-000000000510",
            "0198ad00-0000-7000-8000-000000000511",
            "Project Builder",
            "Model system meaning before implementation structure.",
            "A contributor can create and inspect a project.",
            "Create the project definition.");

    private static T Accepted<T>(SemanticResult<T> result)
        where T : notnull =>
        ((SemanticResult<T>.Accepted)result).Value;

    private sealed class FixedAuthorizer(ProjectCreationAuthorization authorization)
        : IProjectCreationAuthorizer
    {
        public ValueTask<ProjectCreationAuthorization> AuthorizeAsync(
            ProjectActor actor,
            WorkspaceId workspaceId,
            CancellationToken cancellationToken) =>
            ValueTask.FromResult(authorization);
    }

    private sealed class FixedIdentitySource(ProjectId projectId) : IProjectIdentitySource
    {
        public ProjectId NextProjectId() => projectId;
    }

    private sealed class FixedClock(UtcTimestamp now) : IApplicationClock
    {
        public UtcTimestamp GetCurrentTimestamp() => now;
    }

    private sealed class RecordingStore : IProjectCreationStore
    {
        private readonly Dictionary<ChangeSetId, StoredProjectCreation> creations = [];

        public List<ProjectDefinition> Saved { get; } = [];

        public bool DuplicateName { get; init; }

        public int CallCount { get; private set; }

        public ValueTask<StoredProjectCreation?> FindByOperationAsync(
            ChangeSetId operationId,
            CancellationToken cancellationToken)
        {
            CallCount++;
            creations.TryGetValue(operationId, out var creation);
            return ValueTask.FromResult(creation);
        }

        public ValueTask<bool> NameExistsAsync(
            WorkspaceId workspaceId,
            ElementName name,
            CancellationToken cancellationToken)
        {
            CallCount++;
            return ValueTask.FromResult(DuplicateName);
        }

        public ValueTask<bool> TrySaveAsync(
            ProjectDefinition project,
            string requestFingerprint,
            CancellationToken cancellationToken)
        {
            CallCount++;
            Saved.Add(project);
            creations.Add(project.Creation.Id, new StoredProjectCreation(project, requestFingerprint));
            return ValueTask.FromResult(true);
        }

        public ValueTask<ProjectDefinition?> FindByIdAsync(
            ProjectId projectId,
            CancellationToken cancellationToken) =>
            ValueTask.FromResult(Saved.SingleOrDefault(project => project.Id == projectId));
    }
}
