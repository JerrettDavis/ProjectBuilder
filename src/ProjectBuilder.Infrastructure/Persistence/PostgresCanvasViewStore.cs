using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ProjectBuilder.Application.Views;
using ProjectBuilder.Domain.Modeling.Primitives;

namespace ProjectBuilder.Infrastructure.Persistence;

internal sealed class PostgresCanvasViewStore(FoundationDbContext database) : ICanvasViewStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async ValueTask<CanvasViewOverview?> FindAsync(
        ProjectId projectId, string lens, string scopeKey, string visibility, string ownerKey,
        long currentModelRevision, CancellationToken cancellationToken)
    {
        var record = await Query(projectId, lens, scopeKey, visibility, ownerKey)
            .AsNoTracking().SingleOrDefaultAsync(cancellationToken);
        return record is null ? null : ToOverview(record, currentModelRevision);
    }

    public async ValueTask<CanvasViewStoreResult> SaveAsync(
        ProjectId projectId, string name, string lens, string scopeKey, string visibility, string ownerKey,
        long modelRevision, long expectedLayoutVersion, CanvasLayoutOverview layout,
        UtcTimestamp updatedAt, string updatedBy, CancellationToken cancellationToken)
    {
        var existing = await Query(projectId, lens, scopeKey, visibility, ownerKey)
            .SingleOrDefaultAsync(cancellationToken);
        if (existing is null)
        {
            if (expectedLayoutVersion != 0) return new CanvasViewStoreResult.Conflict(0);
            existing = new CanvasViewRecord
            {
                Id = Guid.CreateVersion7(),
                ProjectId = projectId.Value,
                Name = name,
                Lens = lens,
                ScopeKey = scopeKey,
                Visibility = visibility,
                OwnerKey = ownerKey,
                ModelRevision = modelRevision,
                LayoutVersion = 1,
                LayoutJson = JsonSerializer.Serialize(layout, JsonOptions),
                UpdatedAt = updatedAt.Value,
                UpdatedBy = updatedBy,
            };
            database.CanvasViews.Add(existing);
        }
        else
        {
            if (existing.LayoutVersion != expectedLayoutVersion)
                return new CanvasViewStoreResult.Conflict(existing.LayoutVersion);
            database.Entry(existing).Property(record => record.LayoutVersion).OriginalValue = expectedLayoutVersion;
            existing.Name = name;
            existing.ModelRevision = modelRevision;
            existing.LayoutVersion++;
            existing.LayoutJson = JsonSerializer.Serialize(layout, JsonOptions);
            existing.UpdatedAt = updatedAt.Value;
            existing.UpdatedBy = updatedBy;
        }

        try
        {
            await database.SaveChangesAsync(cancellationToken);
            return new CanvasViewStoreResult.Saved(ToOverview(existing, modelRevision));
        }
        catch (DbUpdateConcurrencyException)
        {
            database.ChangeTracker.Clear();
            var actual = await Query(projectId, lens, scopeKey, visibility, ownerKey)
                .AsNoTracking().Select(record => record.LayoutVersion).SingleOrDefaultAsync(cancellationToken);
            return new CanvasViewStoreResult.Conflict(actual);
        }
        catch (DbUpdateException)
        {
            database.ChangeTracker.Clear();
            var actual = await Query(projectId, lens, scopeKey, visibility, ownerKey)
                .AsNoTracking().Select(record => record.LayoutVersion).SingleOrDefaultAsync(cancellationToken);
            return new CanvasViewStoreResult.Conflict(actual);
        }
    }

    public async ValueTask<CanvasViewStoreResult> ResetAsync(
        ProjectId projectId, string lens, string scopeKey, string visibility, string ownerKey,
        long expectedLayoutVersion, CancellationToken cancellationToken)
    {
        var existing = await Query(projectId, lens, scopeKey, visibility, ownerKey)
            .SingleOrDefaultAsync(cancellationToken);
        if (existing is null) return new CanvasViewStoreResult.Missing();
        if (existing.LayoutVersion != expectedLayoutVersion)
            return new CanvasViewStoreResult.Conflict(existing.LayoutVersion);
        database.Entry(existing).Property(record => record.LayoutVersion).OriginalValue = expectedLayoutVersion;
        database.CanvasViews.Remove(existing);
        try
        {
            await database.SaveChangesAsync(cancellationToken);
            return new CanvasViewStoreResult.Reset();
        }
        catch (DbUpdateConcurrencyException)
        {
            database.ChangeTracker.Clear();
            var actual = await Query(projectId, lens, scopeKey, visibility, ownerKey)
                .AsNoTracking().Select(record => record.LayoutVersion).SingleOrDefaultAsync(cancellationToken);
            return new CanvasViewStoreResult.Conflict(actual);
        }
    }

    private IQueryable<CanvasViewRecord> Query(
        ProjectId projectId, string lens, string scopeKey, string visibility, string ownerKey) =>
        database.CanvasViews.Where(record => record.ProjectId == projectId.Value && record.Lens == lens &&
            record.ScopeKey == scopeKey && record.Visibility == visibility && record.OwnerKey == ownerKey);

    private static CanvasViewOverview ToOverview(CanvasViewRecord record, long currentModelRevision) => new(
        record.Id.ToString(), record.ProjectId.ToString(), record.Name, record.Lens, record.ScopeKey,
        record.Visibility, record.OwnerKey, record.ModelRevision, record.LayoutVersion,
        JsonSerializer.Deserialize<CanvasLayoutOverview>(record.LayoutJson, JsonOptions)!,
        record.UpdatedAt.ToString("O"), record.UpdatedBy, record.ModelRevision != currentModelRevision);
}
