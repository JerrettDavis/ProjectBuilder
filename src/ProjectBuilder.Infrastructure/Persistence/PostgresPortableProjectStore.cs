using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using ProjectBuilder.Application.Portability;

namespace ProjectBuilder.Infrastructure.Persistence;

internal sealed class PostgresPortableProjectStore(FoundationDbContext database) : IPortableProjectStore
{
    public async ValueTask<PortableImportStoreResult> ImportAsync(
        Guid workspaceId,
        PortableProjectImport import,
        CancellationToken cancellationToken)
    {
        var document = import.Document;
        var normalizedName = document.Project.Name.ToUpperInvariant();
        var fingerprint = import.ContentHash["sha256:".Length..];
        var existingChange = await database.ProjectChangeSets.AsNoTracking()
            .SingleOrDefaultAsync(change => change.Id == import.ChangeSetId, cancellationToken);
        if (existingChange is not null)
        {
            if (existingChange.ChangeKind != "project.imported" || existingChange.RequestFingerprint != fingerprint)
                return new PortableImportStoreResult.OperationConflict();
            var existingProject = await database.Projects.AsNoTracking().SingleAsync(
                project => project.Id == existingChange.ProjectId, cancellationToken);
            var elementCount = await database.ModelElements.AsNoTracking().CountAsync(
                element => element.ProjectId == existingProject.Id, cancellationToken);
            var relationCount = await database.ModelRelations.AsNoTracking().CountAsync(
                relation => relation.ProjectId == existingProject.Id, cancellationToken);
            var snapshot = await database.PortableProjectSnapshots.AsNoTracking().SingleAsync(
                value => value.ProjectId == existingProject.Id, cancellationToken);
            return new PortableImportStoreResult.Imported(new(
                existingProject.Id.ToString(), existingProject.Name, existingProject.CurrentRevision,
                elementCount, relationCount, snapshot.ContentHash));
        }
        if (await database.Projects.AsNoTracking().AnyAsync(project =>
                project.Id == document.Project.Id ||
                project.WorkspaceId == workspaceId && project.NormalizedName == normalizedName, cancellationToken))
            return new PortableImportStoreResult.DuplicateProject();

        await using var transaction = await database.Database.BeginTransactionAsync(cancellationToken);
        var primaryOutcome = document.Elements.Single(element => element.Id == document.Project.IntendedOutcomeIds[0]);
        var project = new ProjectRecord
        {
            Id = document.Project.Id,
            WorkspaceId = workspaceId,
            Name = document.Project.Name,
            NormalizedName = normalizedName,
            Purpose = document.Project.Purpose,
            IntendedOutcome = primaryOutcome.Payload.GetProperty("statement").GetString()!,
            CurrentRevision = document.Project.Revision,
            CreatedAt = document.Project.CreatedAt,
            CreatedBy = import.Actor.Subject,
        };
        database.Projects.Add(project);

        foreach (var element in document.Elements)
        {
            database.ModelElements.Add(new ModelElementRecord
            {
                Id = element.Id,
                ProjectId = document.Project.Id,
                ParentElementId = element.ParentId,
                Kind = element.Kind,
                Name = element.Name,
                Description = element.Description ?? string.Empty,
                DefinitionStatus = element.DefinitionStatus,
                KnowledgeStatus = element.KnowledgeStatus,
                Order = element.Order,
                Version = element.Version,
                CreatedAt = element.CreatedAt,
                CreatedBy = import.Actor.Subject,
            });
            if (element.Kind == "actor")
            {
                database.ActorPayloads.Add(new ActorPayloadRecord
                {
                    ElementId = element.Id,
                    ActorKind = UpperFirst(element.Payload.GetProperty("actorKind").GetString()!),
                    ContextualRole = element.Description!,
                    GoalsJson = Raw(element.Payload, "goals"),
                    ResponsibilitiesJson = Raw(element.Payload, "responsibilities"),
                    AuthorityJson = Raw(element.Payload, "authority", "[]"),
                    ConstraintsJson = Raw(element.Payload, "constraints", "[]"),
                });
            }
            else
            {
                database.OutcomePayloads.Add(new OutcomePayloadRecord
                {
                    ElementId = element.Id,
                    Statement = element.Payload.GetProperty("statement").GetString()!,
                    SuccessSignalsJson = Raw(element.Payload, "successSignals"),
                });
            }
        }

        foreach (var relation in document.Relations)
        {
            database.ModelRelations.Add(new ModelRelationRecord
            {
                Id = relation.Id,
                ProjectId = document.Project.Id,
                Kind = relation.Kind,
                SourceElementId = relation.SourceId,
                TargetElementId = relation.TargetId,
                Version = relation.Version,
                CreatedAt = relation.CreatedAt,
                CreatedBy = import.Actor.Subject,
            });
        }

        var operationCount = 1 + document.Elements.Count + document.Relations.Count;
        var change = new ProjectChangeSetRecord
        {
            Id = import.ChangeSetId,
            ProjectId = document.Project.Id,
            BaseRevision = null,
            ResultRevision = document.Project.Revision,
            ChangeKind = "project.imported",
            RequestFingerprint = fingerprint,
            Reason = import.Reason,
            OccurredAt = import.ImportedAt,
            CreatedBy = import.Actor.Subject,
            OperationCount = operationCount,
            SemanticSummary = $"project.imported: {operationCount} typed operations from format {document.FormatVersion}.",
            Project = project,
        };
        change.Operations.Add(Operation(change, 0, "project.imported", "project", document.Project.Id, null, $"Imported project '{document.Project.Name}'."));
        var sequence = 1;
        foreach (var element in document.Elements)
            change.Operations.Add(Operation(change, sequence++, "element.added", element.Kind, element.Id, null, $"Added {element.Kind} '{element.Name}'."));
        foreach (var relation in document.Relations)
            change.Operations.Add(Operation(change, sequence++, "relation.added", relation.Kind, null, relation.Id, $"Added {relation.Kind} relation."));
        project.ChangeSets.Add(change);

        database.PortableProjectSnapshots.Add(new PortableProjectSnapshotRecord
        {
            ProjectId = document.Project.Id,
            ModelRevision = document.Project.Revision,
            FormatVersion = document.FormatVersion,
            ContentHash = import.ContentHash,
            CanonicalJson = import.CanonicalJson,
            CreatedAt = import.ImportedAt,
            Project = project,
        });

        try
        {
            await database.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return new PortableImportStoreResult.Imported(new(
                document.Project.Id.ToString(), document.Project.Name, document.Project.Revision,
                document.Elements.Count, document.Relations.Count, import.ContentHash));
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            await transaction.RollbackAsync(cancellationToken);
            database.ChangeTracker.Clear();
            return new PortableImportStoreResult.DuplicateProject();
        }
    }

    public async ValueTask<PortableExportStoreResult> ExportAsync(Guid projectId, CancellationToken cancellationToken)
    {
        var project = await database.Projects.AsNoTracking().SingleOrDefaultAsync(value => value.Id == projectId, cancellationToken);
        if (project is null) return new PortableExportStoreResult.NotFound();
        var snapshot = await database.PortableProjectSnapshots.AsNoTracking().SingleOrDefaultAsync(value => value.ProjectId == projectId, cancellationToken);
        if (snapshot is null) return new PortableExportStoreResult.SnapshotStale(0, project.CurrentRevision);
        return snapshot.ModelRevision == project.CurrentRevision
            ? new PortableExportStoreResult.Exported(snapshot.CanonicalJson, snapshot.ContentHash)
            : new PortableExportStoreResult.SnapshotStale(snapshot.ModelRevision, project.CurrentRevision);
    }

    private static ProjectChangeOperationRecord Operation(
        ProjectChangeSetRecord change, int sequence, string kind, string subjectKind,
        Guid? elementId, Guid? relationId, string summary) => new()
        {
            ChangeSetId = change.Id,
            ProjectId = change.ProjectId,
            Sequence = sequence,
            Kind = kind,
            SubjectKind = subjectKind,
            ElementId = elementId,
            RelationId = relationId,
            Summary = summary,
            PayloadJson = JsonSerializer.Serialize(new { kind, subjectKind, elementId, relationId }),
            ChangeSet = change,
        };

    private static string Raw(JsonElement payload, string property, string fallback = "") =>
        payload.TryGetProperty(property, out var value) ? value.GetRawText() : fallback;

    private static string UpperFirst(string value) => char.ToUpperInvariant(value[0]) + value[1..];
}
