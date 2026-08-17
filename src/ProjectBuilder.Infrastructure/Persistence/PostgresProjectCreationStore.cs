using Microsoft.EntityFrameworkCore;
using Npgsql;
using ProjectBuilder.Application.Projects.CreateProject;
using ProjectBuilder.Domain.Modeling.Primitives;
using ProjectBuilder.Domain.Modeling.Transitions;
using ProjectBuilder.Domain.Projects;

namespace ProjectBuilder.Infrastructure.Persistence;

internal sealed class PostgresProjectCreationStore(FoundationDbContext database) : IProjectCreationStore
{
    public async ValueTask<StoredProjectCreation?> FindByOperationAsync(
        ChangeSetId operationId,
        CancellationToken cancellationToken)
    {
        var record = await database.ProjectChangeSets
            .AsNoTracking()
            .Include(creation => creation.Project)
            .SingleOrDefaultAsync(creation =>
                creation.Id == operationId.Value && creation.ChangeKind == "project.created",
                cancellationToken);

        return record is null
            ? null
            : new StoredProjectCreation(Map(record.Project, record), record.RequestFingerprint);
    }

    public ValueTask<bool> NameExistsAsync(
        WorkspaceId workspaceId,
        ElementName name,
        CancellationToken cancellationToken) =>
        new(database.Projects.AsNoTracking().AnyAsync(
            project =>
                project.WorkspaceId == workspaceId.Value &&
                project.NormalizedName == Normalize(name.Value),
            cancellationToken));

    public async ValueTask<bool> TrySaveAsync(
        ProjectDefinition project,
        string requestFingerprint,
        CancellationToken cancellationToken)
    {
        var projectRecord = new ProjectRecord
        {
            Id = project.Id.Value,
            WorkspaceId = project.WorkspaceId.Value,
            Name = project.Name.Value,
            NormalizedName = Normalize(project.Name.Value),
            Purpose = project.Purpose.Value,
            IntendedOutcome = project.IntendedOutcome.Value,
            CurrentRevision = project.Revision.Value,
            CreatedAt = project.Creation.OccurredAt.Value,
            CreatedBy = project.Creation.CreatedBy,
        };
        var creationRecord = new ProjectChangeSetRecord
        {
            Id = project.Creation.Id.Value,
            ProjectId = project.Id.Value,
            RequestFingerprint = requestFingerprint,
            ResultRevision = project.Creation.ResultRevision.Value,
            ChangeKind = "project.created",
            Reason = project.Creation.Reason.Value,
            OccurredAt = project.Creation.OccurredAt.Value,
            CreatedBy = project.Creation.CreatedBy,
            OperationCount = project.Creation.Operations.Length,
            SemanticSummary = "project.created: 1 typed operation.",
            Project = projectRecord,
        };
        ProjectChangeOperationPersistence.Attach(creationRecord, project.Creation.Operations);
        projectRecord.ChangeSets.Add(creationRecord);
        database.Projects.Add(projectRecord);

        try
        {
            await database.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            database.ChangeTracker.Clear();
            return false;
        }
    }

    public async ValueTask<ProjectDefinition?> FindByIdAsync(
        ProjectId projectId,
        CancellationToken cancellationToken)
    {
        var record = await database.Projects
            .AsNoTracking()
            .SingleOrDefaultAsync(project => project.Id == projectId.Value, cancellationToken);
        if (record is null)
        {
            return null;
        }

        var creation = await database.ProjectChangeSets.AsNoTracking().SingleAsync(
            change => change.ProjectId == projectId.Value &&
                (change.ChangeKind == "project.created" || change.ChangeKind == "project.imported"),
            cancellationToken);
        return Map(record, creation);
    }

    private static ProjectDefinition Map(ProjectRecord project, ProjectChangeSetRecord creation) =>
        ProjectDefinition.Restore(
            Accepted(ProjectId.Create(project.Id)),
            Accepted(WorkspaceId.Create(project.WorkspaceId)),
            Accepted(ElementName.Create(project.Name)),
            Accepted(ProjectPurpose.Create(project.Purpose)),
            Accepted(IntendedOutcome.Create(project.IntendedOutcome)),
            Accepted(Revision.Create(project.CurrentRevision)),
            new CreatedProjectChangeSet(
                Accepted(ChangeSetId.Create(creation.Id)),
                Accepted(ProjectId.Create(project.Id)),
                Accepted(Revision.Create(creation.ResultRevision)),
                Accepted(ChangeReason.Create(creation.Reason)),
                UtcTimestamp.Create(creation.OccurredAt),
                creation.CreatedBy,
                [new ProjectChangeOperation.ProjectCreated(
                    0,
                    Accepted(ProjectId.Create(project.Id)),
                    Accepted(ElementName.Create(project.Name)))]));

    private static T Accepted<T>(SemanticResult<T> result)
        where T : notnull =>
        result is SemanticResult<T>.Accepted accepted
            ? accepted.Value
            : throw new InvalidOperationException("Persisted project data violated its domain contract.");

    private static string Normalize(string name) => name.ToUpperInvariant();
}
