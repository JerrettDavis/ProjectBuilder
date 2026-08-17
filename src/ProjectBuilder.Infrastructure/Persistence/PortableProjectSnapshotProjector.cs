using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ProjectBuilder.Application.Portability;
using ProjectBuilder.Infrastructure.Portability;

namespace ProjectBuilder.Infrastructure.Persistence;

internal sealed class PortableProjectSnapshotProjector(
    FoundationDbContext database,
    IPortableProjectCodec codec)
{
    private static readonly JsonElement EmptyObject = JsonSerializer.SerializeToElement(new { });

    public async ValueTask<bool> RefreshSupportedAsync(
        Guid projectId,
        long revision,
        DateTimeOffset exportedAt,
        CancellationToken cancellationToken)
    {
        var project = await database.Projects.AsNoTracking().SingleAsync(value => value.Id == projectId, cancellationToken);
        var elements = await database.ModelElements
            .Include(value => value.Actor)
            .Include(value => value.Outcome)
            .Where(value => value.ProjectId == projectId)
            .OrderBy(value => value.Order)
            .ThenBy(value => value.Id)
            .ToListAsync(cancellationToken);
        var relations = await database.ModelRelations
            .Where(value => value.ProjectId == projectId)
            .OrderBy(value => value.Id)
            .ToListAsync(cancellationToken);

        if (project.CurrentRevision != revision ||
            elements.Count == 0 ||
            elements.Any(value => value.Kind is not ("actor" or "outcome")) ||
            elements.Any(value => !string.Equals(value.KnowledgeStatus, "known", StringComparison.OrdinalIgnoreCase)) ||
            relations.Any(value => value.Kind != "benefitsFrom"))
            return false;

        var intendedOutcomes = elements
            .Where(value => value.Kind == "outcome" &&
                string.Equals(value.Outcome!.Statement, project.IntendedOutcome, StringComparison.Ordinal))
            .Select(value => value.Id)
            .Order()
            .ToArray();
        if (intendedOutcomes.Length == 0)
            return false;

        var document = new PortableProjectDocument(
            "project-builder",
            JsonPortableProjectCodec.CurrentFormatVersion,
            exportedAt,
            new("Project Builder", "0.1.0-foundation", null),
            new(
                project.Id,
                null,
                project.Name,
                project.Purpose,
                "active",
                "defined",
                null,
                revision,
                intendedOutcomes,
                [],
                project.CreatedAt,
                exportedAt,
                null),
            elements.Select(element => Element(element, relations)).ToArray(),
            relations.Select(Relation).ToArray(),
            [],
            [],
            [],
            EmptyObject);

        if (codec.Write(document) is not PortableProjectReadResult.Accepted accepted)
            throw new InvalidOperationException("The supported native model did not produce a schema-valid portable snapshot.");

        var snapshot = await database.PortableProjectSnapshots.SingleOrDefaultAsync(
            value => value.ProjectId == projectId, cancellationToken);
        if (snapshot is null)
        {
            snapshot = new PortableProjectSnapshotRecord { ProjectId = projectId };
            database.PortableProjectSnapshots.Add(snapshot);
        }
        snapshot.ModelRevision = revision;
        snapshot.FormatVersion = accepted.Document.FormatVersion;
        snapshot.ContentHash = accepted.ContentHash;
        snapshot.CanonicalJson = accepted.CanonicalJson;
        snapshot.CreatedAt = exportedAt;
        return true;
    }

    private static PortableElement Element(
        ModelElementRecord element,
        IReadOnlyList<ModelRelationRecord> relations) => new(
        element.Id,
        element.ProjectId,
        element.ParentElementId,
        element.Kind,
        element.Name,
        string.IsNullOrEmpty(element.Description) ? null : element.Description,
        LowerFirst(element.DefinitionStatus),
        LowerFirst(element.KnowledgeStatus),
        element.Order,
        [],
        [],
        element.CreatedAt,
        element.CreatedAt,
        element.Version,
        element.Kind == "actor" ? ActorPayload(element.Actor!) : OutcomePayload(element, element.Outcome!, relations));

    private static PortableRelation Relation(ModelRelationRecord relation) => new(
        relation.Id,
        relation.ProjectId,
        relation.Kind,
        relation.SourceElementId,
        relation.TargetElementId,
        null,
        "defined",
        "known",
        relation.CreatedAt,
        relation.CreatedAt,
        relation.Version,
        EmptyObject);

    private static JsonElement ActorPayload(ActorPayloadRecord actor) => JsonSerializer.SerializeToElement(new
    {
        actorKind = LowerFirst(actor.ActorKind),
        goals = Strings(actor.GoalsJson),
        responsibilities = Strings(actor.ResponsibilitiesJson),
        authority = Strings(actor.AuthorityJson),
        constraints = Strings(actor.ConstraintsJson),
    });

    private static JsonElement OutcomePayload(
        ModelElementRecord element,
        OutcomePayloadRecord outcome,
        IReadOnlyList<ModelRelationRecord> relations) =>
        JsonSerializer.SerializeToElement(new
        {
            statement = outcome.Statement,
            beneficiaryIds = relations
                .Where(value => value.Kind == "benefitsFrom" && value.TargetElementId == element.Id)
                .Select(value => value.SourceElementId)
                .Order()
                .ToArray(),
            successSignals = Strings(outcome.SuccessSignalsJson),
        });

    private static string[] Strings(string json) =>
        JsonSerializer.Deserialize<string[]>(json) ?? [];

    private static string LowerFirst(string value) =>
        char.ToLowerInvariant(value[0]) + value[1..];
}
