using System.Collections.Immutable;
using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using ProjectBuilder.Application.Modeling;
using ProjectBuilder.Application.Traceability;
using ProjectBuilder.Domain.Modeling.Elements;
using ProjectBuilder.Domain.Modeling.Gaps;
using ProjectBuilder.Domain.Modeling.Primitives;
using ProjectBuilder.Domain.Modeling.Relations;
using ProjectBuilder.Domain.Modeling.Traceability;
using ProjectBuilder.Domain.Modeling.Transitions;

namespace ProjectBuilder.Infrastructure.Persistence;

internal sealed class PostgresProjectElementStore(
    FoundationDbContext database,
    PortableProjectSnapshotProjector? portableSnapshots = null) : IProjectElementStore, ITraceabilityStore
{
    public async ValueTask<StoredElementCommit?> FindCommitByOperationAsync(
        ChangeSetId operationId,
        CancellationToken cancellationToken)
    {
        var record = await database.ProjectChangeSets.AsNoTracking().SingleOrDefaultAsync(
            x => x.Id == operationId.Value && x.ElementId != null,
            cancellationToken);
        return record is null
            ? null
            : new(record.ChangeKind, record.RequestFingerprint, Revision(record.ResultRevision), ElementId(record.ElementId!.Value));
    }

    public async ValueTask<ActorDefinition?> FindActorAsync(
        ProjectId projectId,
        ElementId actorId,
        CancellationToken cancellationToken)
    {
        var record = await database.ModelElements.AsNoTracking().Include(x => x.Actor).SingleOrDefaultAsync(
            x => x.ProjectId == projectId.Value && x.Id == actorId.Value && x.Kind == "actor",
            cancellationToken);
        return record is null ? null : Actor(record);
    }

    public async ValueTask<StoredOutcome?> FindOutcomeAsync(
        ProjectId projectId,
        ElementId outcomeId,
        CancellationToken cancellationToken)
    {
        var record = await database.ModelElements.AsNoTracking().Include(x => x.Outcome).SingleOrDefaultAsync(
            x => x.ProjectId == projectId.Value && x.Id == outcomeId.Value && x.Kind == "outcome",
            cancellationToken);
        return record is null ? null : await StoredOutcomeAsync(record, cancellationToken);
    }

    public async ValueTask<CapabilityDefinition?> FindCapabilityAsync(
        ProjectId projectId, ElementId capabilityId, CancellationToken cancellationToken)
    {
        var record = await database.ModelElements.AsNoTracking().Include(x => x.Capability).SingleOrDefaultAsync(
            x => x.ProjectId == projectId.Value && x.Id == capabilityId.Value && x.Kind == "capability", cancellationToken);
        return record is null ? null : Capability(record);
    }

    public async ValueTask<int> NextElementOrderAsync(ProjectId projectId, CancellationToken cancellationToken)
    {
        var max = await database.ModelElements.AsNoTracking()
            .Where(x => x.ProjectId == projectId.Value)
            .Select(x => (int?)x.Order)
            .MaxAsync(cancellationToken);
        return (max ?? -1) + 1;
    }

    public ValueTask<ElementStoreCommitResult> CommitActorAsync(
        AddActorTransitionResult.Accepted commit,
        string requestFingerprint,
        CancellationToken cancellationToken) =>
        CommitAsync(
            commit.ChangeSet,
            requestFingerprint,
            Element(commit.Actor, "actor"),
            new ActorPayloadRecord
            {
                ElementId = commit.Actor.Id.Value,
                ActorKind = commit.Actor.ActorKind.ToString(),
                ContextualRole = commit.Actor.ContextualRole.Value,
                GoalsJson = Json(commit.Actor.Goals.Select(x => x.Value)),
                ResponsibilitiesJson = Json(commit.Actor.Responsibilities.Select(x => x.Value)),
                AuthorityJson = Json(commit.Actor.Authority.Select(x => x.Value)),
                ConstraintsJson = Json(commit.Actor.Constraints.Select(x => x.Value)),
            },
            null,
            null,
            null,
            cancellationToken);

    public ValueTask<ElementStoreCommitResult> CommitOutcomeAsync(
        AddOutcomeTransitionResult.Accepted commit,
        string requestFingerprint,
        CancellationToken cancellationToken) =>
        CommitAsync(
            commit.ChangeSet,
            requestFingerprint,
            Element(commit.Outcome, "outcome"),
            null,
            new OutcomePayloadRecord
            {
                ElementId = commit.Outcome.Id.Value,
                Statement = commit.Outcome.Statement.Value,
                SuccessSignalsJson = Json(commit.Outcome.SuccessSignals.Select(x => x.Value)),
            },
            null,
            new ModelRelationRecord
            {
                Id = commit.Beneficiary.Id.Value,
                ProjectId = commit.Beneficiary.ProjectId.Value,
                Kind = ModelRelationRegistry.Describe(commit.Beneficiary.Kind).Key,
                SourceElementId = commit.Beneficiary.SourceId.Value,
                TargetElementId = commit.Beneficiary.TargetId.Value,
                Version = 1,
                CreatedAt = commit.Beneficiary.CreatedAt.Value,
                CreatedBy = commit.Beneficiary.CreatedBy,
            },
            cancellationToken);

    public ValueTask<ElementStoreCommitResult> CommitCapabilityAsync(
        AddCapabilityTransitionResult.Accepted commit, string requestFingerprint, CancellationToken cancellationToken) =>
        CommitAsync(commit.ChangeSet, requestFingerprint, Element(commit.Capability, "capability"), null, null,
            new CapabilityPayloadRecord
            {
                ElementId = commit.Capability.Id.Value,
                OutcomeIdsJson = Json(commit.Capability.OutcomeIds.Select(id => id.Value.ToString("D", CultureInfo.InvariantCulture))),
                Priority = commit.Capability.Priority.ToString(),
            }, null, cancellationToken);

    public async ValueTask<ElementStoreCommitResult> UpdateActorAsync(
        UpdateActorTransitionResult.Accepted commit,
        string requestFingerprint,
        CancellationToken cancellationToken)
    {
        await using var transaction = await database.Database.BeginTransactionAsync(cancellationToken);
        var conflict = await AdvanceRevisionAsync(commit.ChangeSet, cancellationToken);
        if (conflict is not null) { await transaction.RollbackAsync(cancellationToken); return conflict; }
        var element = await database.ModelElements.Include(x => x.Actor).SingleAsync(
            x => x.ProjectId == commit.Actor.ProjectId.Value && x.Id == commit.Actor.Id.Value && x.Kind == "actor", cancellationToken);
        Apply(element, commit.Actor);
        element.Actor!.ActorKind = commit.Actor.ActorKind.ToString();
        element.Actor.ContextualRole = commit.Actor.ContextualRole.Value;
        element.Actor.GoalsJson = Json(commit.Actor.Goals.Select(x => x.Value));
        element.Actor.ResponsibilitiesJson = Json(commit.Actor.Responsibilities.Select(x => x.Value));
        element.Actor.AuthorityJson = Json(commit.Actor.Authority.Select(x => x.Value));
        element.Actor.ConstraintsJson = Json(commit.Actor.Constraints.Select(x => x.Value));
        database.ProjectChangeSets.Add(ChangeSet(commit.ChangeSet, requestFingerprint));
        return await CompleteUpdateAsync(commit.ChangeSet, transaction, cancellationToken);
    }

    public async ValueTask<ElementStoreCommitResult> UpdateOutcomeAsync(
        UpdateOutcomeTransitionResult.Accepted commit,
        string requestFingerprint,
        CancellationToken cancellationToken)
    {
        await using var transaction = await database.Database.BeginTransactionAsync(cancellationToken);
        var conflict = await AdvanceRevisionAsync(commit.ChangeSet, cancellationToken);
        if (conflict is not null) { await transaction.RollbackAsync(cancellationToken); return conflict; }
        var element = await database.ModelElements.Include(x => x.Outcome).SingleAsync(
            x => x.ProjectId == commit.Outcome.ProjectId.Value && x.Id == commit.Outcome.Id.Value && x.Kind == "outcome", cancellationToken);
        Apply(element, commit.Outcome);
        element.Outcome!.Statement = commit.Outcome.Statement.Value;
        element.Outcome.SuccessSignalsJson = Json(commit.Outcome.SuccessSignals.Select(x => x.Value));
        var relation = await database.ModelRelations.SingleAsync(x => x.Id == commit.Beneficiary.Id.Value, cancellationToken);
        relation.SourceElementId = commit.Beneficiary.SourceId.Value;
        database.ProjectChangeSets.Add(ChangeSet(commit.ChangeSet, requestFingerprint));
        return await CompleteUpdateAsync(commit.ChangeSet, transaction, cancellationToken);
    }

    private async ValueTask<ElementStoreCommitResult> CompleteUpdateAsync(
        ProjectModelChangeSet change,
        Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction,
        CancellationToken cancellationToken)
    {
        try
        {
            await database.SaveChangesAsync(cancellationToken);
            if (portableSnapshots is not null)
            {
                await portableSnapshots.RefreshSupportedAsync(change.ProjectId.Value, change.ResultRevision.Value, change.OccurredAt.Value, cancellationToken);
                await database.SaveChangesAsync(cancellationToken);
            }
            await transaction.CommitAsync(cancellationToken);
            return new ElementStoreCommitResult.Committed();
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            await transaction.RollbackAsync(cancellationToken);
            database.ChangeTracker.Clear();
            return new ElementStoreCommitResult.OperationConflict();
        }
    }

    private static void Apply(ModelElementRecord record, ModelElement element)
    {
        record.Name = element.Name.Value;
        record.Description = element.Description.Value;
        record.DefinitionStatus = element.DefinitionStatus.ToString();
        record.KnowledgeStatus = element.KnowledgeStatus.ToString();
        record.Version = element.Version;
    }

    public async ValueTask<ElementStoreCommitResult> CommitNarrativeAsync(
        DefineNarrativeTransitionResult.Accepted commit,
        string requestFingerprint,
        CancellationToken cancellationToken)
    {
        await using var transaction = await database.Database.BeginTransactionAsync(cancellationToken);
        var conflict = await AdvanceRevisionAsync(commit.ChangeSet, cancellationToken);
        if (conflict is not null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return conflict;
        }

        foreach (var element in commit.Narrative.Elements)
        {
            database.ModelElements.Add(Element(element, Kind(element)));
            database.NarrativePayloads.Add(new NarrativePayloadRecord
            {
                ElementId = element.Id.Value,
                PayloadJson = NarrativeJson(element),
            });
        }
        database.ProjectChangeSets.Add(ChangeSet(commit.ChangeSet, requestFingerprint));
        try
        {
            await database.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return new ElementStoreCommitResult.Committed();
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            await transaction.RollbackAsync(cancellationToken);
            database.ChangeTracker.Clear();
            return new ElementStoreCommitResult.OperationConflict();
        }
    }

    public async ValueTask<NarrativeOverview?> FindNarrativeAsync(
        ProjectId projectId,
        ElementId episodeId,
        CancellationToken cancellationToken)
    {
        var records = await NarrativeRecordsAsync(projectId, cancellationToken);
        var episode = records.SingleOrDefault(x => x.Id == episodeId.Value && x.Kind == "episode");
        return episode is null ? null : await NarrativeOverviewAsync(episode, records, cancellationToken);
    }

    public async ValueTask<ElementStoreCommitResult> CommitStateLogicAsync(
        DefineStateLogicTransitionResult.Accepted commit,
        string requestFingerprint,
        CancellationToken cancellationToken)
    {
        await using var transaction = await database.Database.BeginTransactionAsync(cancellationToken);
        var conflict = await AdvanceRevisionAsync(commit.ChangeSet, cancellationToken);
        if (conflict is not null) { await transaction.RollbackAsync(cancellationToken); return conflict; }
        foreach (var element in commit.Definitions.Elements)
        {
            database.ModelElements.Add(Element(element, StateLogicKind(element)));
            database.StateLogicPayloads.Add(new StateLogicPayloadRecord { ElementId = element.Id.Value, PayloadJson = StateLogicJson(element) });
        }
        database.ProjectChangeSets.Add(ChangeSet(commit.ChangeSet, requestFingerprint));
        try
        {
            await database.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return new ElementStoreCommitResult.Committed();
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            await transaction.RollbackAsync(cancellationToken); database.ChangeTracker.Clear();
            return new ElementStoreCommitResult.OperationConflict();
        }
    }

    public async ValueTask<StateLogicOverview?> FindStateLogicAsync(
        ProjectId projectId, ElementId stateId, CancellationToken cancellationToken)
    {
        var records = await StateLogicRecordsAsync(projectId, cancellationToken);
        var state = records.SingleOrDefault(x => x.Id == stateId.Value && x.Kind == "stateDefinition");
        return state is null ? null : await StateLogicOverviewAsync(state, records, cancellationToken);
    }

    public async ValueTask<SemanticResultDefinition?> FindSemanticResultAsync(
        ProjectId projectId, ElementId resultId, CancellationToken cancellationToken)
    {
        var record = await database.ModelElements.AsNoTracking().Include(x => x.StateLogic).SingleOrDefaultAsync(
            x => x.ProjectId == projectId.Value && x.Id == resultId.Value && x.Kind == "resultDefinition",
            cancellationToken);
        if (record is null) return null;
        using var json = JsonDocument.Parse(record.StateLogic!.PayloadJson);
        return new SemanticResultDefinition(ElementId(record.Id), ProjectId(record.ProjectId),
            ElementId(record.ParentElementId!.Value), Accepted(ElementName.Create(record.Name)),
            Enum.Parse<SemanticResultKind>(Text(json, "ResultKind"), true),
            Accepted(LogicStatement.Create(Text(json, "Meaning"))), record.Order,
            UtcTimestamp.Create(record.CreatedAt), record.CreatedBy);
    }

    public async ValueTask<ElementStoreCommitResult> CommitPathAsync(
        DefinePathTransitionResult.Accepted commit,
        string requestFingerprint,
        CancellationToken cancellationToken)
    {
        await using var transaction = await database.Database.BeginTransactionAsync(cancellationToken);
        var conflict = await AdvanceRevisionAsync(commit.ChangeSet, cancellationToken);
        if (conflict is not null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return conflict;
        }

        foreach (var element in commit.Definitions.Elements)
        {
            database.ModelElements.Add(Element(element, PathKind(element)));
            database.PathPayloads.Add(new PathPayloadRecord
            {
                ElementId = element.Id.Value,
                PayloadJson = PathJson(element),
            });
        }
        database.ProjectChangeSets.Add(ChangeSet(commit.ChangeSet, requestFingerprint));
        try
        {
            await database.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return new ElementStoreCommitResult.Committed();
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            await transaction.RollbackAsync(cancellationToken);
            database.ChangeTracker.Clear();
            return new ElementStoreCommitResult.OperationConflict();
        }
    }

    public async ValueTask<PathOverview?> FindPathAsync(
        ProjectId projectId, ElementId branchPathId, CancellationToken cancellationToken)
    {
        var records = await PathRecordsAsync(projectId, cancellationToken);
        var branch = records.SingleOrDefault(record =>
            record.Id == branchPathId.Value && record.Kind == "path");
        return branch is null ? null : await PathOverviewAsync(branch, records, cancellationToken);
    }

    public async ValueTask<ElementStoreCommitResult> CommitSystemContextAsync(
        DefineSystemContextTransitionResult.Accepted commit, string requestFingerprint,
        CancellationToken cancellationToken)
    {
        await using var transaction = await database.Database.BeginTransactionAsync(cancellationToken);
        var conflict = await AdvanceRevisionAsync(commit.ChangeSet, cancellationToken);
        if (conflict is not null) { await transaction.RollbackAsync(cancellationToken); return conflict; }
        foreach (var element in commit.Definitions.Elements)
        {
            database.ModelElements.Add(Element(element, SystemContextKind(element)));
            database.SystemContextPayloads.Add(new SystemContextPayloadRecord
            { ElementId = element.Id.Value, PayloadJson = SystemContextJson(element) });
        }
        database.ProjectChangeSets.Add(ChangeSet(commit.ChangeSet, requestFingerprint));
        try
        {
            await database.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return new ElementStoreCommitResult.Committed();
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            await transaction.RollbackAsync(cancellationToken); database.ChangeTracker.Clear();
            return new ElementStoreCommitResult.OperationConflict();
        }
    }

    public async ValueTask<SystemContextOverview?> FindSystemContextAsync(
        ProjectId projectId, ElementId ownedSystemId, CancellationToken cancellationToken)
    {
        var records = await SystemContextRecordsAsync(projectId, cancellationToken);
        var owned = records.SingleOrDefault(record => record.Id == ownedSystemId.Value && record.Kind == "system");
        return owned is null ? null : await SystemContextOverviewAsync(owned, records, cancellationToken);
    }

    public async ValueTask<ProjectModelSnapshot> LoadModelAsync(
        ProjectId projectId,
        CancellationToken cancellationToken)
    {
        var actors = await database.ModelElements.AsNoTracking().Include(x => x.Actor)
            .Where(x => x.ProjectId == projectId.Value && x.Kind == "actor")
            .OrderBy(x => x.Order).ToListAsync(cancellationToken);
        var outcomes = await database.ModelElements.AsNoTracking().Include(x => x.Outcome)
            .Where(x => x.ProjectId == projectId.Value && x.Kind == "outcome")
            .OrderBy(x => x.Order).ToListAsync(cancellationToken);
        var capabilities = await database.ModelElements.AsNoTracking().Include(x => x.Capability)
            .Where(x => x.ProjectId == projectId.Value && x.Kind == "capability")
            .OrderBy(x => x.Order).ToListAsync(cancellationToken);

        var storedOutcomes = ImmutableArray.CreateBuilder<StoredOutcome>(outcomes.Count);
        foreach (var outcome in outcomes)
        {
            storedOutcomes.Add(await StoredOutcomeAsync(outcome, cancellationToken));
        }

        var narrativeRecords = await NarrativeRecordsAsync(projectId, cancellationToken);
        var narratives = ImmutableArray.CreateBuilder<NarrativeOverview>();
        foreach (var episode in narrativeRecords.Where(x => x.Kind == "episode").OrderBy(x => x.Order))
            narratives.Add(await NarrativeOverviewAsync(episode, narrativeRecords, cancellationToken));

        var stateLogicRecords = await StateLogicRecordsAsync(projectId, cancellationToken);
        var stateLogic = ImmutableArray.CreateBuilder<StateLogicOverview>();
        foreach (var state in stateLogicRecords.Where(x => x.Kind == "stateDefinition").OrderBy(x => x.Order))
            stateLogic.Add(await StateLogicOverviewAsync(state, stateLogicRecords, cancellationToken));

        var pathRecords = await PathRecordsAsync(projectId, cancellationToken);
        var paths = ImmutableArray.CreateBuilder<PathOverview>();
        foreach (var branch in pathRecords.Where(record => record.Kind == "path").OrderBy(record => record.Order))
        {
            using var json = JsonDocument.Parse(branch.Path!.PayloadJson);
            if (json.RootElement.GetProperty("RecoversFromPathId").ValueKind == JsonValueKind.Null)
                paths.Add(await PathOverviewAsync(branch, pathRecords, cancellationToken));
        }

        var systemContextRecords = await SystemContextRecordsAsync(projectId, cancellationToken);
        var systemContexts = ImmutableArray.CreateBuilder<SystemContextOverview>();
        foreach (var owned in systemContextRecords.Where(record => record.Kind == "system" && PayloadText(record, "Classification") == "owned").OrderBy(record => record.Order))
            systemContexts.Add(await SystemContextOverviewAsync(owned, systemContextRecords, cancellationToken));

        var relationRecords = await database.ModelRelations.AsNoTracking()
            .Include(record => record.Source)
            .Include(record => record.Target)
            .Where(record => record.ProjectId == projectId.Value)
            .OrderBy(record => record.Kind)
            .ThenBy(record => record.SourceElementId)
            .ThenBy(record => record.TargetElementId)
            .ToListAsync(cancellationToken);
        var relations = relationRecords.Select(record =>
        {
            var descriptor = ModelRelationRegistry.Describe(record.Kind);
            var relation = (SemanticResult<ModelRelationDefinition>.Accepted)ModelRelationRegistry.Create(
                RelationIdentifier(record.Id), projectId, descriptor.Kind,
                ElementId(record.SourceElementId), ElementKind(record.Source.Kind),
                ElementId(record.TargetElementId), ElementKind(record.Target.Kind),
                UtcTimestamp.Create(record.CreatedAt), record.CreatedBy);
            return new StoredModelRelation(relation.Value, record.Source.Name, record.Target.Name);
        }).ToImmutableArray();

        var dispositionRecords = await database.GapDispositions.AsNoTracking()
            .Where(record => record.ProjectId == projectId.Value)
            .OrderBy(record => record.ProfileId).ThenBy(record => record.RuleCode).ThenBy(record => record.ScopeId)
            .ToListAsync(cancellationToken);
        var authorityNames = await database.ModelElements.AsNoTracking()
            .Where(record => record.ProjectId == projectId.Value && record.Kind == "actor")
            .ToDictionaryAsync(record => record.Id, record => record.Name, cancellationToken);
        var dispositions = dispositionRecords.Select(record => new GapDispositionOverview(
            record.Id.ToString("D", CultureInfo.InvariantCulture), record.ProfileId, record.RuleCode,
            record.ScopeId.ToString("D", CultureInfo.InvariantCulture), record.Disposition,
            record.Rationale, record.Consequence,
            record.AuthorityActorId.ToString("D", CultureInfo.InvariantCulture),
            authorityNames.GetValueOrDefault(record.AuthorityActorId, "Unknown authority"),
            record.ReviewOn, record.TargetMilestone,
            record.CreatedAt.ToString("O", CultureInfo.InvariantCulture), record.CreatedBy)).ToImmutableArray();

        return new(
            [.. actors.Select(Actor)],
            storedOutcomes.ToImmutable(),
            [.. capabilities.Select(Capability)],
            narratives.ToImmutable(),
            stateLogic.ToImmutable(),
            paths.ToImmutable(),
            relations,
            dispositions,
            systemContexts.ToImmutable());
    }

    public async ValueTask<ElementStoreCommitResult> CommitGapDispositionAsync(
        RecordGapDispositionTransitionResult.Accepted commit,
        string requestFingerprint,
        CancellationToken cancellationToken)
    {
        await using var transaction = await database.Database.BeginTransactionAsync(cancellationToken);
        var conflict = await AdvanceRevisionAsync(commit.ChangeSet, cancellationToken);
        if (conflict is not null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return conflict;
        }

        var disposition = commit.Disposition;
        database.GapDispositions.Add(new GapDispositionRecord
        {
            Id = disposition.Id.Value,
            ProjectId = disposition.ProjectId.Value,
            ProfileId = disposition.ProfileId,
            RuleCode = disposition.RuleCode,
            ScopeId = disposition.ScopeId.Value,
            Disposition = disposition.Disposition.ToString(),
            Rationale = disposition.Rationale.Value,
            Consequence = disposition.Consequence.Value,
            AuthorityActorId = disposition.AuthorityActorId.Value,
            ReviewOn = disposition.ReviewOn,
            TargetMilestone = disposition.TargetMilestone,
            CreatedAt = disposition.CreatedAt.Value,
            CreatedBy = disposition.CreatedBy,
        });
        database.ProjectChangeSets.Add(ChangeSet(commit.ChangeSet, requestFingerprint));
        try
        {
            await database.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return new ElementStoreCommitResult.Committed();
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            await transaction.RollbackAsync(cancellationToken);
            database.ChangeTracker.Clear();
            return new ElementStoreCommitResult.OperationConflict();
        }
    }

    public async ValueTask<ElementStoreCommitResult> CommitEvidencePacketAsync(
        DefineEvidencePacketTransitionResult.Accepted commit, string requestFingerprint,
        CancellationToken cancellationToken)
    {
        await using var transaction = await database.Database.BeginTransactionAsync(cancellationToken);
        var conflict = await AdvanceRevisionAsync(commit.ChangeSet, cancellationToken);
        if (conflict is not null) { await transaction.RollbackAsync(cancellationToken); return conflict; }
        var claim = commit.Packet.Claim; var evidence = commit.Packet.Evidence;
        database.Claims.Add(new ClaimRecord
        {
            Id = claim.Id.Value,
            ProjectId = claim.ProjectId.Value,
            Kind = LowerFirst(claim.Kind.ToString()),
            Statement = claim.Statement.Value,
            Status = LowerFirst(claim.Status.ToString()),
            ElementIdsJson = Json(claim.ElementIds.Select(id => id.ToString())),
            EvidenceId = claim.EvidenceId.Value,
            OwnerId = claim.OwnerId.Value,
            TagsJson = Json(claim.Tags.Select(tag => tag.Value)),
            CreatedAt = claim.CreatedAt.Value,
            CreatedBy = claim.CreatedBy,
        });
        database.Evidence.Add(new EvidenceRecord
        {
            Id = evidence.Id.Value,
            ProjectId = evidence.ProjectId.Value,
            Kind = LowerFirst(evidence.Kind.ToString()),
            Status = LowerFirst(evidence.Status.ToString()),
            ClaimId = evidence.ClaimId.Value,
            Producer = evidence.Producer.Value,
            ProducedAt = evidence.ProducedAt.Value,
            ModelRevision = evidence.ModelRevision.Value,
            Environment = evidence.Environment.Value,
            Summary = evidence.Summary.Value,
            LimitationsJson = Json(evidence.Limitations.Select(item => item.Value)),
            CreatedAt = evidence.CreatedAt.Value,
            CreatedBy = evidence.CreatedBy,
        });
        var change = ChangeSet(commit.ChangeSet, requestFingerprint); change.ElementId = claim.Id.Value;
        database.ProjectChangeSets.Add(change);
        try
        {
            await database.SaveChangesAsync(cancellationToken); await transaction.CommitAsync(cancellationToken);
            return new ElementStoreCommitResult.Committed();
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        { await transaction.RollbackAsync(cancellationToken); database.ChangeTracker.Clear(); return new ElementStoreCommitResult.OperationConflict(); }
    }

    public async ValueTask<TraceabilitySnapshot> LoadTraceabilityAsync(ProjectId projectId, CancellationToken cancellationToken)
    {
        var claims = await database.Claims.AsNoTracking().Where(item => item.ProjectId == projectId.Value)
            .OrderBy(item => item.CreatedAt).ThenBy(item => item.Id).ToListAsync(cancellationToken);
        var evidence = await database.Evidence.AsNoTracking().Where(item => item.ProjectId == projectId.Value)
            .OrderBy(item => item.ProducedAt).ThenBy(item => item.Id).ToListAsync(cancellationToken);
        var ownerIds = claims.Select(item => item.OwnerId).Distinct().ToArray();
        var owners = await database.ModelElements.AsNoTracking().Where(item => item.ProjectId == projectId.Value && ownerIds.Contains(item.Id))
            .ToDictionaryAsync(item => item.Id, item => item.Name, cancellationToken);
        return new([.. claims.Select(item => ClaimOverview(item, owners.GetValueOrDefault(item.OwnerId, "Unknown authority")))],
            [.. evidence.Select(EvidenceOverview)]);
    }

    public async ValueTask<(ClaimOverview Claim, EvidenceOverview Evidence)?> FindEvidencePacketAsync(
        ProjectId projectId, ClaimId claimId, CancellationToken cancellationToken)
    {
        var claim = await database.Claims.AsNoTracking().SingleOrDefaultAsync(item => item.ProjectId == projectId.Value && item.Id == claimId.Value, cancellationToken);
        if (claim is null) return null;
        var evidence = await database.Evidence.AsNoTracking().SingleAsync(item => item.ClaimId == claim.Id, cancellationToken);
        var owner = await database.ModelElements.AsNoTracking().Where(item => item.Id == claim.OwnerId).Select(item => item.Name).SingleOrDefaultAsync(cancellationToken) ?? "Unknown authority";
        return (ClaimOverview(claim, owner), EvidenceOverview(evidence));
    }

    private static ClaimOverview ClaimOverview(ClaimRecord item, string ownerName) => new(
        item.Id.ToString(), item.Kind, item.Statement, item.Status,
        JsonSerializer.Deserialize<string[]>(item.ElementIdsJson)!, item.OwnerId.ToString(), ownerName,
        JsonSerializer.Deserialize<string[]>(item.TagsJson)!, item.CreatedAt.ToString("O", CultureInfo.InvariantCulture), item.CreatedBy);
    private static EvidenceOverview EvidenceOverview(EvidenceRecord item) => new(
        item.Id.ToString(), item.Kind, item.Status, item.ClaimId.ToString(), item.Producer,
        item.ProducedAt.ToString("O", CultureInfo.InvariantCulture), item.ModelRevision, item.Environment,
        item.Summary, JsonSerializer.Deserialize<string[]>(item.LimitationsJson)!, item.CreatedBy);

    public async ValueTask<ImmutableArray<ChangeSetOverview>> LoadChangeHistoryAsync(
        ProjectId projectId,
        CancellationToken cancellationToken)
    {
        var records = await database.ProjectChangeSets.AsNoTracking()
            .Include(changeSet => changeSet.Operations)
            .Where(changeSet => changeSet.ProjectId == projectId.Value)
            .OrderByDescending(changeSet => changeSet.ResultRevision)
            .ToListAsync(cancellationToken);
        return [.. records.Select(changeSet => new ChangeSetOverview(
            changeSet.Id.ToString("D", CultureInfo.InvariantCulture),
            changeSet.BaseRevision,
            changeSet.ResultRevision,
            changeSet.ChangeKind,
            changeSet.Reason,
            changeSet.CreatedBy,
            changeSet.OccurredAt.ToString("O", CultureInfo.InvariantCulture),
            changeSet.OperationCount,
            changeSet.SemanticSummary,
            changeSet.Operations.OrderBy(operation => operation.Sequence)
                .Select(operation => new ChangeOperationOverview(
                    operation.Sequence,
                    operation.Kind,
                    operation.SubjectKind,
                    operation.ElementId?.ToString("D", CultureInfo.InvariantCulture),
                    operation.RelationId?.ToString("D", CultureInfo.InvariantCulture),
                    operation.Summary))
                .ToArray()))];
    }

    private async ValueTask<ElementStoreCommitResult> CommitAsync(
        ProjectModelChangeSet change,
        string fingerprint,
        ModelElementRecord element,
        ActorPayloadRecord? actor,
        OutcomePayloadRecord? outcome,
        CapabilityPayloadRecord? capability,
        ModelRelationRecord? relation,
        CancellationToken cancellationToken)
    {
        await using var transaction = await database.Database.BeginTransactionAsync(cancellationToken);
        var conflict = await AdvanceRevisionAsync(change, cancellationToken);
        if (conflict is not null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return conflict;
        }

        database.ModelElements.Add(element);
        if (actor is not null) database.ActorPayloads.Add(actor);
        if (outcome is not null) database.OutcomePayloads.Add(outcome);
        if (capability is not null) database.CapabilityPayloads.Add(capability);
        if (relation is not null) database.ModelRelations.Add(relation);
        database.ProjectChangeSets.Add(ChangeSet(change, fingerprint));

        try
        {
            await database.SaveChangesAsync(cancellationToken);
            if (portableSnapshots is not null)
            {
                await portableSnapshots.RefreshSupportedAsync(
                    change.ProjectId.Value,
                    change.ResultRevision.Value,
                    change.OccurredAt.Value,
                    cancellationToken);
                await database.SaveChangesAsync(cancellationToken);
            }
            await transaction.CommitAsync(cancellationToken);
            return new ElementStoreCommitResult.Committed();
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            await transaction.RollbackAsync(cancellationToken);
            database.ChangeTracker.Clear();
            return new ElementStoreCommitResult.OperationConflict();
        }
    }

    private async ValueTask<StoredOutcome> StoredOutcomeAsync(
        ModelElementRecord record,
        CancellationToken cancellationToken)
    {
        var relation = await database.ModelRelations.AsNoTracking().SingleAsync(
            x => x.ProjectId == record.ProjectId && x.TargetElementId == record.Id && x.Kind == "benefitsFrom",
            cancellationToken);
        var beneficiary = await database.ModelElements.AsNoTracking().SingleAsync(
            x => x.Id == relation.SourceElementId,
            cancellationToken);
        return new(Outcome(record), RelationIdentifier(relation.Id), ElementId(beneficiary.Id), beneficiary.Name);
    }

    private static ModelElementRecord Element(ModelElement element, string kind) => new()
    {
        Id = element.Id.Value,
        ProjectId = element.ProjectId.Value,
        ParentElementId = element.ParentId?.Value,
        Kind = kind,
        Name = element.Name.Value,
        Description = element.Description.Value,
        DefinitionStatus = element.DefinitionStatus.ToString(),
        KnowledgeStatus = element.KnowledgeStatus.ToString(),
        Order = element.Order,
        Version = element.Version,
        CreatedAt = element.CreatedAt.Value,
        CreatedBy = element.CreatedBy,
    };

    private async ValueTask<ElementStoreCommitResult.RevisionConflict?> AdvanceRevisionAsync(
        ProjectModelChangeSet change,
        CancellationToken cancellationToken)
    {
        var affected = await database.Projects
            .Where(x => x.Id == change.ProjectId.Value && x.CurrentRevision == change.BaseRevision.Value)
            .ExecuteUpdateAsync(update => update.SetProperty(x => x.CurrentRevision, change.ResultRevision.Value), cancellationToken);
        if (affected != 0) return null;
        var actual = await database.Projects.AsNoTracking().Where(x => x.Id == change.ProjectId.Value)
            .Select(x => x.CurrentRevision).SingleAsync(cancellationToken);
        return new(Revision(actual));
    }

    private static ProjectChangeSetRecord ChangeSet(ProjectModelChangeSet change, string fingerprint)
    {
        var record = new ProjectChangeSetRecord
        {
            Id = change.Id.Value,
            ProjectId = change.ProjectId.Value,
            BaseRevision = change.BaseRevision.Value,
            ResultRevision = change.ResultRevision.Value,
            ElementId = change.ChangedElementId.Value,
            ChangeKind = change.ChangeKind,
            RequestFingerprint = fingerprint,
            Reason = change.Reason.Value,
            OccurredAt = change.OccurredAt.Value,
            CreatedBy = change.CreatedBy,
            OperationCount = change.Operations.Length,
            SemanticSummary = $"{change.ChangeKind}: {change.Operations.Length} typed operation(s).",
        };
        ProjectChangeOperationPersistence.Attach(record, change.Operations);
        return record;
    }

    private async Task<List<ModelElementRecord>> NarrativeRecordsAsync(ProjectId projectId, CancellationToken cancellationToken) =>
        await database.ModelElements.AsNoTracking().Include(x => x.Narrative)
            .Where(x => x.ProjectId == projectId.Value &&
                (x.Kind == "episode" || x.Kind == "scenario" || x.Kind == "scene" ||
                 x.Kind == "interaction" || x.Kind == "intent" || x.Kind == "step" || x.Kind == "observation"))
            .OrderBy(x => x.Order).ToListAsync(cancellationToken);

    private async ValueTask<NarrativeOverview> NarrativeOverviewAsync(
        ModelElementRecord episode, List<ModelElementRecord> records, CancellationToken cancellationToken)
    {
        var scenario = Child(records, episode, "scenario");
        var scene = Child(records, scenario, "scene");
        var interaction = Child(records, scene, "interaction");
        var intent = Child(records, interaction, "intent");
        var step = Child(records, interaction, "step");
        var observation = Child(records, interaction, "observation");
        using var episodeJson = JsonDocument.Parse(episode.Narrative!.PayloadJson);
        using var scenarioJson = JsonDocument.Parse(scenario.Narrative!.PayloadJson);
        using var sceneJson = JsonDocument.Parse(scene.Narrative!.PayloadJson);
        using var interactionJson = JsonDocument.Parse(interaction.Narrative!.PayloadJson);
        using var intentJson = JsonDocument.Parse(intent.Narrative!.PayloadJson);
        using var stepJson = JsonDocument.Parse(step.Narrative!.PayloadJson);
        using var observationJson = JsonDocument.Parse(observation.Narrative!.PayloadJson);
        var outcomeId = episodeJson.RootElement.GetProperty("OutcomeId").GetGuid();
        var initiatorId = interactionJson.RootElement.GetProperty("InitiatorId").GetGuid();
        var receiverId = interactionJson.RootElement.GetProperty("ReceiverId").GetGuid();
        var names = await database.ModelElements.AsNoTracking()
            .Where(x => x.ProjectId == episode.ProjectId && (x.Id == outcomeId || x.Id == initiatorId || x.Id == receiverId))
            .ToDictionaryAsync(x => x.Id, x => x.Name, cancellationToken);
        return new(
            episode.Id.ToString(), episode.Name, Text(episodeJson, "Start"), Text(episodeJson, "End"), names[outcomeId],
            scenario.Id.ToString(), scenario.Name, Text(scenarioJson, "Classification"), Strings(scenarioJson, "StartingFacts"),
            Text(scenarioJson, "Trigger"), Text(scenarioJson, "ExpectedOutcome"), scene.Name,
            Text(sceneJson, "Setting"), Text(sceneJson, "Responsibility"), interaction.Name,
            names[initiatorId], names[receiverId], Text(intentJson, "Statement"), Text(stepJson, "Statement"),
            Text(observationJson, "Statement"), Strings(interactionJson, "SemanticResults"),
            outcomeId.ToString(), scene.Id.ToString(), initiatorId.ToString(), receiverId.ToString(),
            interaction.Id.ToString(), intent.Id.ToString(), step.Id.ToString(), observation.Id.ToString());
    }

    private static ModelElementRecord Child(List<ModelElementRecord> records, ModelElementRecord parent, string kind) =>
        records.Single(x => x.ParentElementId == parent.Id && x.Kind == kind);
    private static string Text(JsonDocument json, string property) => json.RootElement.GetProperty(property).GetString()!;
    private static string[] Strings(JsonDocument json, string property) =>
        [.. json.RootElement.GetProperty(property).EnumerateArray().Select(x => x.GetString()!)];
    private static string Kind(ModelElement element) => element switch
    {
        EpisodeDefinition => "episode",
        ScenarioDefinition => "scenario",
        SceneDefinition => "scene",
        InteractionDefinition => "interaction",
        IntentDefinition => "intent",
        StepDefinition => "step",
        ObservationDefinition => "observation",
        _ => throw new InvalidOperationException("Unknown narrative element."),
    };
    private static string NarrativeJson(ModelElement element) => element switch
    {
        EpisodeDefinition x => JsonSerializer.Serialize(new { Start = x.Start.Value, End = x.End.Value, OutcomeId = x.OutcomeId.Value, ParticipantIds = x.ParticipantIds.Select(y => y.Value) }),
        ScenarioDefinition x => JsonSerializer.Serialize(new { Classification = LowerFirst(x.Classification.ToString()), StartingFacts = x.StartingFacts.Select(y => y.Value), Trigger = x.Trigger.Value, ExpectedOutcome = x.ExpectedOutcome.Value, ParticipantIds = x.ParticipantIds.Select(y => y.Value) }),
        SceneDefinition x => JsonSerializer.Serialize(new { Setting = x.Setting.Value, Responsibility = x.Responsibility.Value, ParticipantIds = x.ParticipantIds.Select(y => y.Value) }),
        InteractionDefinition x => JsonSerializer.Serialize(new { InitiatorId = x.InitiatorId.Value, ReceiverId = x.ReceiverId.Value, SemanticResults = x.SemanticResults.Select(y => y.Value) }),
        IntentDefinition x => JsonSerializer.Serialize(new { Statement = x.Statement.Value, ActorId = x.ExpressedById.Value }),
        StepDefinition x => JsonSerializer.Serialize(new { Statement = x.Statement.Value }),
        ObservationDefinition x => JsonSerializer.Serialize(new { Statement = x.Statement.Value, ActorId = x.VisibleToId.Value }),
        _ => throw new InvalidOperationException("Unknown narrative element."),
    };
    private static string LowerFirst(string value) => char.ToLowerInvariant(value[0]) + value[1..];

    private async Task<List<ModelElementRecord>> StateLogicRecordsAsync(ProjectId projectId, CancellationToken cancellationToken) =>
        await database.ModelElements.AsNoTracking().Include(x => x.StateLogic)
            .Where(x => x.ProjectId == projectId.Value &&
                (x.Kind == "stateDefinition" || x.Kind == "factDefinition" || x.Kind == "ruleDefinition" ||
                 x.Kind == "invariantDefinition" || x.Kind == "resultDefinition" || x.Kind == "transitionDefinition"))
            .OrderBy(x => x.Order).ToListAsync(cancellationToken);

    private async ValueTask<StateLogicOverview> StateLogicOverviewAsync(
        ModelElementRecord state, List<ModelElementRecord> records, CancellationToken cancellationToken)
    {
        var fact = Child(records, state, "factDefinition"); var rule = Child(records, state, "ruleDefinition");
        var invariant = Child(records, state, "invariantDefinition"); var transition = Child(records, state, "transitionDefinition");
        var resultRecords = records.Where(x => x.ParentElementId == state.Id && x.Kind == "resultDefinition").OrderBy(x => x.Order).ToArray();
        using var stateJson = JsonDocument.Parse(state.StateLogic!.PayloadJson);
        using var factJson = JsonDocument.Parse(fact.StateLogic!.PayloadJson);
        using var ruleJson = JsonDocument.Parse(rule.StateLogic!.PayloadJson);
        using var invariantJson = JsonDocument.Parse(invariant.StateLogic!.PayloadJson);
        using var transitionJson = JsonDocument.Parse(transition.StateLogic!.PayloadJson);
        var ownerId = stateJson.RootElement.GetProperty("OwnerId").GetGuid();
        var ownerName = await database.ModelElements.AsNoTracking().Where(x => x.Id == ownerId).Select(x => x.Name).SingleAsync(cancellationToken);
        var ruleAuthorityOwnerId = ruleJson.RootElement.GetProperty("AuthorityOwnerId").GetGuid();
        var ruleAuthorityOwnerName = await database.ModelElements.AsNoTracking().Where(x => x.Id == ruleAuthorityOwnerId)
            .Select(x => x.Name).SingleAsync(cancellationToken);
        var results = new List<SemanticResultOverview>();
        foreach (var record in resultRecords)
        {
            using var json = JsonDocument.Parse(record.StateLogic!.PayloadJson);
            results.Add(new(record.Id.ToString(), record.Name, Text(json, "ResultKind"), Text(json, "Meaning")));
        }
        return new(state.Id.ToString(), state.Name, Text(stateJson, "Category"), Strings(stateJson, "Structure"),
            Strings(stateJson, "Values"), ownerName, fact.Id.ToString(), fact.Name, Text(factJson, "ValueType"), Text(factJson, "Authority"),
            Text(factJson, "Mutability"), rule.Id.ToString(), rule.Name, Text(ruleJson, "Kind"), Text(ruleJson, "Statement"),
            invariant.Name, Text(invariantJson, "Statement"), Text(invariantJson, "FalsifyingExample"),
            Strings(invariantJson, "ProofExpectation"), results, transition.Id.ToString(), transition.Name,
            Text(transitionJson, "SourcePredicate"), Text(transitionJson, "Trigger"), Text(transitionJson, "TargetPredicate"),
            ownerId.ToString(), Strings(factJson, "AllowedKnowledge"),
            ruleAuthorityOwnerId.ToString(), invariant.Id.ToString(),
            Guids(invariantJson, "ScopeIds"), Guids(transitionJson, "ChangedFactIds"),
            Guids(transitionJson, "RuleIds"), Guids(transitionJson, "InvariantIds"), Guids(transitionJson, "ResultIds"),
            ruleAuthorityOwnerName);
    }

    private static string[] Guids(JsonDocument json, string name) =>
        [.. json.RootElement.GetProperty(name).EnumerateArray().Select(value => value.GetGuid().ToString())];

    private static string StateLogicKind(ModelElement element) => element switch
    {
        StateDefinition => "stateDefinition",
        FactDefinition => "factDefinition",
        RuleDefinition => "ruleDefinition",
        InvariantDefinition => "invariantDefinition",
        SemanticResultDefinition => "resultDefinition",
        TransitionDefinition => "transitionDefinition",
        _ => throw new InvalidOperationException("Unknown state/logic element."),
    };
    private static string StateLogicJson(ModelElement element) => element switch
    {
        StateDefinition x => JsonSerializer.Serialize(new { Category = LowerFirst(x.Category.ToString()), Structure = x.Structure.Select(y => y.Value), Values = x.Values.Select(y => y.Value), OwnerId = x.OwnerId.Value }),
        FactDefinition x => JsonSerializer.Serialize(new { ValueType = x.ValueType.Value, Authority = x.Authority.Value, Mutability = LowerFirst(x.Mutability.ToString()), AllowedKnowledge = x.AllowedKnowledge.Select(y => LowerFirst(y.ToString())) }),
        RuleDefinition x => JsonSerializer.Serialize(new { Kind = LowerFirst(x.Kind.ToString()), Statement = x.Statement.Value, AuthorityOwnerId = x.AuthorityOwnerId.Value }),
        InvariantDefinition x => JsonSerializer.Serialize(new { Statement = x.Statement.Value, ScopeIds = x.ScopeIds.Select(y => y.Value), FalsifyingExample = x.FalsifyingExample.Value, ProofExpectation = x.ProofExpectation.Select(y => y.Value) }),
        SemanticResultDefinition x => JsonSerializer.Serialize(new { ResultKind = LowerFirst(x.ResultKind.ToString()), Meaning = x.Meaning.Value }),
        TransitionDefinition x => JsonSerializer.Serialize(new { SourcePredicate = x.SourcePredicate.Value, Trigger = x.Trigger.Value, TargetPredicate = x.TargetPredicate.Value, ChangedFactIds = x.ChangedFactIds.Select(y => y.Value), RuleIds = x.RuleIds.Select(y => y.Value), InvariantIds = x.InvariantIds.Select(y => y.Value), ResultIds = x.ResultIds.Select(y => y.Value) }),
        _ => throw new InvalidOperationException("Unknown state/logic element."),
    };

    private async Task<List<ModelElementRecord>> PathRecordsAsync(ProjectId projectId, CancellationToken cancellationToken) =>
        await database.ModelElements.AsNoTracking().Include(record => record.Path)
            .Where(record => record.ProjectId == projectId.Value &&
                (record.Kind == "path" || record.Kind == "condition" || record.Kind == "effectDefinition"))
            .OrderBy(record => record.Order).ToListAsync(cancellationToken);

    private async ValueTask<PathOverview> PathOverviewAsync(
        ModelElementRecord branch, List<ModelElementRecord> records, CancellationToken cancellationToken)
    {
        using var branchJson = JsonDocument.Parse(branch.Path!.PayloadJson);
        var recoveryId = branchJson.RootElement.GetProperty("RecoveryPathId").GetGuid();
        var recovery = records.Single(record => record.Id == recoveryId && record.Kind == "path");
        var branchCondition = Child(records, branch, "condition");
        var effect = Child(records, branch, "effectDefinition");
        var recoveryCondition = Child(records, recovery, "condition");
        using var branchConditionJson = JsonDocument.Parse(branchCondition.Path!.PayloadJson);
        using var effectJson = JsonDocument.Parse(effect.Path!.PayloadJson);
        using var recoveryJson = JsonDocument.Parse(recovery.Path!.PayloadJson);
        using var recoveryConditionJson = JsonDocument.Parse(recoveryCondition.Path!.PayloadJson);
        var scenarioId = branch.ParentElementId!.Value;
        var transitionId = branchJson.RootElement.GetProperty("SourceTransitionId").GetGuid();
        var terminalResultId = branchJson.RootElement.GetProperty("TerminalResultId").GetGuid();
        var recoveryResultId = recoveryJson.RootElement.GetProperty("TerminalResultId").GetGuid();
        var ownerId = branchJson.RootElement.GetProperty("OwnerId").GetGuid();
        var names = await database.ModelElements.AsNoTracking()
            .Where(record => record.ProjectId == branch.ProjectId &&
                (record.Id == scenarioId || record.Id == transitionId || record.Id == terminalResultId ||
                 record.Id == recoveryResultId || record.Id == ownerId))
            .ToDictionaryAsync(record => record.Id, record => record.Name, cancellationToken);
        var terminalResult = await FindSemanticResultAsync(ProjectId(branch.ProjectId), ElementId(terminalResultId), cancellationToken) ??
            throw new InvalidOperationException("A persisted path references a missing terminal result.");
        return new(
            branch.Id.ToString(), branch.Name, Text(branchJson, "Classification"), names[scenarioId], names[transitionId],
            branchCondition.Name, Text(branchConditionJson, "Kind"), Text(branchConditionJson, "Statement"),
            Strings(branchJson, "Segments"), names[terminalResultId], LowerFirst(terminalResult.ResultKind.ToString()),
            Text(branchJson, "TerminalState"), Text(branchJson, "Observation"), names[ownerId],
            effect.Name, Text(effectJson, "Kind"), Text(effectJson, "Statement"),
            recovery.Id.ToString(), recovery.Name, Text(recoveryJson, "RecoveryStrategy"),
            Text(recoveryConditionJson, "Statement"), Strings(recoveryJson, "Segments"),
            names[recoveryResultId], Text(recoveryJson, "TerminalState"), Text(recoveryJson, "Observation"),
            OptionalText(recoveryJson, "RetryPolicy"), OptionalText(recoveryJson, "IdempotencyAnalysis"),
            OptionalText(recoveryJson, "ExitCondition"), OptionalText(recoveryJson, "Reconciliation"),
            scenarioId.ToString(), transitionId.ToString(), branchCondition.Id.ToString(), terminalResultId.ToString(),
            ownerId.ToString(), effect.Id.ToString(), recoveryCondition.Id.ToString(), recoveryResultId.ToString());
    }

    private static string PathKind(ModelElement element) => element switch
    {
        PathDefinition => "path",
        ConditionDefinition => "condition",
        EffectDefinition => "effectDefinition",
        _ => throw new InvalidOperationException("Unknown path element."),
    };

    private static string PathJson(ModelElement element) => element switch
    {
        PathDefinition path => JsonSerializer.Serialize(new
        {
            Classification = LowerFirst(path.Classification.ToString()),
            SourceTransitionId = path.SourceTransitionId.Value,
            ConditionIds = path.ConditionIds.Select(id => id.Value),
            Segments = path.Segments.Select(segment => segment.Value),
            TerminalResultId = path.TerminalResultId?.Value,
            TargetTransitionId = path.TargetTransitionId?.Value,
            TerminalState = path.TerminalState.Value,
            Observation = path.Observation.Value,
            OwnerId = path.OwnerId.Value,
            RecoveryPathId = path.RecoveryPathId?.Value,
            RecoversFromPathId = path.RecoversFromPathId?.Value,
            RecoveryStrategy = path.RecoveryStrategy is null ? null : LowerFirst(path.RecoveryStrategy.Value.ToString()),
            RetryPolicy = path.RetryPolicy?.Value,
            IdempotencyAnalysis = path.IdempotencyAnalysis?.Value,
            ExitCondition = path.ExitCondition?.Value,
            Reconciliation = path.Reconciliation?.Value,
        }),
        ConditionDefinition condition => JsonSerializer.Serialize(new
        {
            Kind = LowerFirst(condition.Kind.ToString()),
            Statement = condition.Statement.Value,
            FactIds = condition.FactIds.Select(id => id.Value),
            RuleIds = condition.RuleIds.Select(id => id.Value),
        }),
        EffectDefinition effect => JsonSerializer.Serialize(new
        {
            Kind = LowerFirst(effect.Kind.ToString()),
            Statement = effect.Statement.Value,
            FailurePathId = effect.FailurePathId?.Value,
        }),
        _ => throw new InvalidOperationException("Unknown path element."),
    };

    private async Task<List<ModelElementRecord>> SystemContextRecordsAsync(ProjectId projectId, CancellationToken cancellationToken) =>
        await database.ModelElements.AsNoTracking().Include(record => record.SystemContext)
            .Where(record => record.ProjectId == projectId.Value &&
                (record.Kind == "system" || record.Kind == "interface" || record.Kind == "boundary" || record.Kind == "contract"))
            .OrderBy(record => record.Order).ToListAsync(cancellationToken);

    private async ValueTask<SystemContextOverview> SystemContextOverviewAsync(
        ModelElementRecord owned, List<ModelElementRecord> records, CancellationToken cancellationToken)
    {
        var systemInterface = Child(records, owned, "interface");
        var boundary = Child(records, systemInterface, "boundary");
        var contract = Child(records, boundary, "contract");
        using var ownedJson = JsonDocument.Parse(owned.SystemContext!.PayloadJson);
        using var interfaceJson = JsonDocument.Parse(systemInterface.SystemContext!.PayloadJson);
        using var boundaryJson = JsonDocument.Parse(boundary.SystemContext!.PayloadJson);
        using var contractJson = JsonDocument.Parse(contract.SystemContext!.PayloadJson);
        var externalId = boundaryJson.RootElement.GetProperty("TargetSystemId").GetGuid();
        var external = records.Single(record => record.Id == externalId && record.Kind == "system");
        using var externalJson = JsonDocument.Parse(external.SystemContext!.PayloadJson);
        var actorIds = new[] { ownedJson.RootElement.GetProperty("OwnerId").GetGuid(), externalJson.RootElement.GetProperty("OwnerId").GetGuid(), contractJson.RootElement.GetProperty("OwnerId").GetGuid() }
            .Concat(interfaceJson.RootElement.GetProperty("ParticipantIds").EnumerateArray().Select(value => value.GetGuid()))
            .Concat(boundaryJson.RootElement.GetProperty("OwnerIds").EnumerateArray().Select(value => value.GetGuid())).Distinct().ToArray();
        var actorNames = await database.ModelElements.AsNoTracking().Where(record => record.ProjectId == owned.ProjectId && actorIds.Contains(record.Id))
            .ToDictionaryAsync(record => record.Id, record => record.Name, cancellationToken);
        var participantIds = interfaceJson.RootElement.GetProperty("ParticipantIds").EnumerateArray().Select(value => value.GetGuid()).ToArray();
        var boundaryOwnerIds = boundaryJson.RootElement.GetProperty("OwnerIds").EnumerateArray().Select(value => value.GetGuid()).ToArray();
        var effectProperty = boundaryJson.RootElement.GetProperty("CrossingEffectId");
        var effectId = effectProperty.ValueKind == JsonValueKind.Null ? (Guid?)null : effectProperty.GetGuid();
        var effectName = effectId is null ? null : await database.ModelElements.AsNoTracking()
            .Where(record => record.ProjectId == owned.ProjectId && record.Id == effectId.Value)
            .Select(record => record.Name).SingleOrDefaultAsync(cancellationToken);
        return new(
            owned.Id.ToString(), owned.Name, owned.Description, Text(ownedJson, "OwnerId"), actorNames.GetValueOrDefault(ownedJson.RootElement.GetProperty("OwnerId").GetGuid(), "Unknown owner"), Strings(ownedJson, "Responsibilities"),
            external.Id.ToString(), external.Name, external.Description, Text(externalJson, "OwnerId"), actorNames.GetValueOrDefault(externalJson.RootElement.GetProperty("OwnerId").GetGuid(), "Unknown owner"), Strings(externalJson, "Responsibilities"), external.KnowledgeStatus,
            systemInterface.Id.ToString(), systemInterface.Name, systemInterface.Description, Text(interfaceJson, "Kind"),
            participantIds.Select(id => id.ToString()).ToArray(), participantIds.Select(id => id == external.Id ? external.Name : actorNames.GetValueOrDefault(id, "Unknown participant")).ToArray(),
            Strings(interfaceJson, "AcceptedIntents"), Strings(interfaceJson, "Observations"), Strings(interfaceJson, "AccessibilityConstraints"),
            boundary.Id.ToString(), boundary.Name, boundary.Description, Strings(boundaryJson, "Kinds"),
            boundaryOwnerIds.Select(id => id.ToString()).ToArray(), boundaryOwnerIds.Select(id => actorNames.GetValueOrDefault(id, "Unknown owner")).ToArray(),
            boundary.KnowledgeStatus, effectId?.ToString(), effectName,
            contract.Id.ToString(), contract.Name, contract.Description, Text(contractJson, "Kind"), Text(contractJson, "Version"),
            Text(contractJson, "OwnerId"), actorNames.GetValueOrDefault(contractJson.RootElement.GetProperty("OwnerId").GetGuid(), "Unknown owner"),
            Text(contractJson, "SchemaReference"), Text(contractJson, "CompatibilityPolicy"), Text(contractJson, "RequestData"),
            Text(contractJson, "ResponseData"), Text(contractJson, "DataClassification"), contract.KnowledgeStatus);
    }

    private static string SystemContextKind(ModelElement element) => element switch
    {
        SystemDefinition => "system",
        InterfaceDefinition => "interface",
        BoundaryDefinition => "boundary",
        ContractDefinition => "contract",
        _ => throw new InvalidOperationException("Unknown system context element."),
    };

    private static string SystemContextJson(ModelElement element) => element switch
    {
        SystemDefinition x => JsonSerializer.Serialize(new { Classification = LowerFirst(x.Classification.ToString()), OwnerId = x.OwnerId.Value, Responsibilities = x.Responsibilities.Select(value => value.Value) }),
        InterfaceDefinition x => JsonSerializer.Serialize(new { Kind = LowerFirst(x.Kind.ToString()), ParticipantIds = x.ParticipantIds.Select(id => id.Value), AcceptedIntents = x.AcceptedIntents.Select(value => value.Value), Observations = x.Observations.Select(value => value.Value), AccessibilityConstraints = x.AccessibilityConstraints.Select(value => value.Value), ContractId = x.ContractId.Value }),
        BoundaryDefinition x => JsonSerializer.Serialize(new { Kinds = x.Kinds.Select(value => LowerFirst(value.ToString())), OwnerIds = x.OwnerIds.Select(id => id.Value), SourceSystemId = x.SourceSystemId.Value, TargetSystemId = x.TargetSystemId.Value, CrossingEffectId = x.CrossingEffectId?.Value }),
        ContractDefinition x => JsonSerializer.Serialize(new { Kind = LowerFirst(x.Kind.ToString()), Version = x.ContractVersion.Value, OwnerId = x.OwnerId.Value, SchemaReference = x.SchemaReference.Value, CompatibilityPolicy = x.CompatibilityPolicy.Value, RequestData = x.RequestData.Value, ResponseData = x.ResponseData.Value, DataClassification = x.DataClassification.Value }),
        _ => throw new InvalidOperationException("Unknown system context element."),
    };

    private static string PayloadText(ModelElementRecord record, string property)
    {
        using var json = JsonDocument.Parse(record.SystemContext!.PayloadJson);
        return Text(json, property);
    }

    private static string OptionalText(JsonDocument json, string property) =>
        json.RootElement.GetProperty(property).ValueKind == JsonValueKind.Null
            ? string.Empty
            : Text(json, property);

    private static ActorDefinition Actor(ModelElementRecord record) => new(
        ElementId(record.Id), ProjectId(record.ProjectId), Accepted(ElementName.Create(record.Name)),
        Accepted(ContextualRole.Create(record.Actor!.ContextualRole)),
        Enum.Parse<ActorKind>(record.Actor.ActorKind),
        Statements(record.Actor.GoalsJson), Statements(record.Actor.ResponsibilitiesJson),
        Statements(record.Actor.AuthorityJson), Statements(record.Actor.ConstraintsJson),
        record.Order, UtcTimestamp.Create(record.CreatedAt), record.CreatedBy,
        Enum.Parse<KnowledgeStatus>(record.KnowledgeStatus, ignoreCase: true));

    private static OutcomeDefinition Outcome(ModelElementRecord record) => new(
        ElementId(record.Id), ProjectId(record.ProjectId), Accepted(ElementName.Create(record.Name)),
        Accepted(OutcomeStatement.Create(record.Outcome!.Statement)),
        [.. JsonSerializer.Deserialize<string[]>(record.Outcome.SuccessSignalsJson)!.Select(x => Accepted(SuccessSignal.Create(x)))],
        record.Order, UtcTimestamp.Create(record.CreatedAt), record.CreatedBy,
        Enum.Parse<KnowledgeStatus>(record.KnowledgeStatus, ignoreCase: true));

    private static CapabilityDefinition Capability(ModelElementRecord record) => new(
        ElementId(record.Id), ProjectId(record.ProjectId), Accepted(ElementName.Create(record.Name)),
        Accepted(Description.Create(record.Description)),
        [.. JsonSerializer.Deserialize<string[]>(record.Capability!.OutcomeIdsJson)!.Select(value => Accepted(ProjectBuilder.Domain.Modeling.Primitives.ElementId.Parse(value)))],
        Enum.Parse<CapabilityPriority>(record.Capability.Priority), record.Order,
        UtcTimestamp.Create(record.CreatedAt), record.CreatedBy,
        Enum.Parse<KnowledgeStatus>(record.KnowledgeStatus, ignoreCase: true));

    private static ImmutableArray<ActorStatement> Statements(string json) =>
        [.. JsonSerializer.Deserialize<string[]>(json)!.Select(x => Accepted(ActorStatement.Create(x)))];

    private static string Json(IEnumerable<string> values) => JsonSerializer.Serialize(values);
    private static ProjectId ProjectId(Guid value) => Accepted(ProjectBuilder.Domain.Modeling.Primitives.ProjectId.Create(value));
    private static ElementId ElementId(Guid value) => Accepted(ProjectBuilder.Domain.Modeling.Primitives.ElementId.Create(value));
    private static RelationId RelationIdentifier(Guid value) => Accepted(ProjectBuilder.Domain.Modeling.Primitives.RelationId.Create(value));
    private static Revision Revision(long value) => Accepted(ProjectBuilder.Domain.Modeling.Primitives.Revision.Create(value));
    private static ModelElementKind ElementKind(string kind) => kind switch
    {
        "actor" => ModelElementKind.Actor,
        "outcome" => ModelElementKind.Outcome,
        _ => throw new InvalidOperationException($"Persisted relation endpoint kind '{kind}' is not registered."),
    };
    private static T Accepted<T>(SemanticResult<T> result) where T : notnull =>
        result is SemanticResult<T>.Accepted accepted ? accepted.Value :
            throw new InvalidOperationException("Persisted model data violated its domain contract.");
}
