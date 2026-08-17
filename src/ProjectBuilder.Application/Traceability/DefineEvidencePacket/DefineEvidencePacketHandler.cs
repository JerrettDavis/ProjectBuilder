using System.Collections.Immutable;
using ProjectBuilder.Application.Modeling;
using ProjectBuilder.Application.Projects.CreateProject;
using ProjectBuilder.Domain.Modeling.Elements;
using ProjectBuilder.Domain.Modeling.Primitives;
using ProjectBuilder.Domain.Modeling.Traceability;
using ProjectBuilder.Domain.Modeling.Transitions;

namespace ProjectBuilder.Application.Traceability.DefineEvidencePacket;

public sealed class DefineEvidencePacketHandler(
    IProjectCreationStore projects, IProjectElementStore elements, ITraceabilityStore traceability,
    IProjectEditAuthorizer authorizer, IModelIdentitySource identities, IApplicationClock clock)
{
    public async ValueTask<DefineEvidencePacketResult> HandleAsync(
        DefineEvidencePacketCommand command, ProjectActor actor, CancellationToken cancellationToken = default)
    {
        var validated = Validate(command);
        if (validated.Errors.Count > 0) return new DefineEvidencePacketResult.Invalid(validated.Errors);
        var project = await projects.FindByIdAsync(validated.ProjectId!, cancellationToken);
        if (project is null) return new DefineEvidencePacketResult.ProjectNotFound(command.ProjectId);
        var access = await authorizer.AuthorizeEditAsync(actor, project.WorkspaceId, cancellationToken);
        if (!access.IsAllowed) return new DefineEvidencePacketResult.Denied(access.Reason);
        var model = await elements.LoadModelAsync(project.Id, cancellationToken);
        var draft = validated.Draft!;
        if (model.Actors.All(item => item.Id != draft.OwnerId)) return new DefineEvidencePacketResult.ReferenceNotFound("claim owner");
        var knownIds = KnownElementIds(model);
        if (draft.ElementIds.Any(id => !knownIds.Contains(id))) return new DefineEvidencePacketResult.ReferenceNotFound("claim scope");

        var fingerprint = ModelRequestFingerprint.Create(command.ProjectId, command.ExpectedRevision,
            command.ClaimKind, command.ClaimStatement, command.ClaimStatus, string.Join('\n', command.ElementIds),
            command.OwnerId, command.Tags, command.EvidenceKind, command.EvidenceStatus, command.Producer,
            command.Environment, command.Summary, command.Limitations, command.Reason);
        var operation = await elements.FindCommitByOperationAsync(validated.OperationId!, cancellationToken);
        if (operation is not null) return await ExistingAsync(operation, validated.OperationId!, fingerprint, project.Id, cancellationToken);
        var claimId = ClaimId.From(identities.NextElementId());
        var evidenceId = EvidenceId.From(identities.NextElementId());
        var transitioned = TraceabilityTransition.Define(project, validated.ExpectedRevision!, claimId,
            evidenceId, draft, validated.OperationId!, validated.Reason!, clock.GetCurrentTimestamp(), actor.Subject);
        if (transitioned is DefineEvidencePacketTransitionResult.Conflict conflict)
            return new DefineEvidencePacketResult.Conflict(conflict.Expected.Value, conflict.Actual.Value,
                ModelApplicationMapping.Conflicts(conflict.Conflicts));
        if (transitioned is DefineEvidencePacketTransitionResult.Invalid invalid)
            return new DefineEvidencePacketResult.Invalid(invalid.Errors);
        var accepted = (DefineEvidencePacketTransitionResult.Accepted)transitioned;
        var stored = await traceability.CommitEvidencePacketAsync(accepted, fingerprint, cancellationToken);
        return stored switch
        {
            ElementStoreCommitResult.Committed => await ReloadAsync(project.Id, claimId, accepted.Project.Revision, cancellationToken),
            ElementStoreCommitResult.RevisionConflict storeConflict => new DefineEvidencePacketResult.Conflict(
                validated.ExpectedRevision!.Value, storeConflict.Actual.Value,
                ModelApplicationMapping.RevisionConflict(validated.ExpectedRevision!, storeConflict.Actual)),
            ElementStoreCommitResult.OperationConflict => await ReloadOperationAsync(validated.OperationId!, fingerprint, project.Id, cancellationToken),
            _ => throw new InvalidOperationException("Unknown traceability store result."),
        };
    }

    private async ValueTask<DefineEvidencePacketResult> ExistingAsync(StoredElementCommit existing,
        ChangeSetId operationId, string fingerprint, ProjectId projectId, CancellationToken cancellationToken)
    {
        if (existing.ChangeKind != "evidence-packet.defined" || !string.Equals(existing.RequestFingerprint, fingerprint, StringComparison.Ordinal))
            return new DefineEvidencePacketResult.IdempotencyConflict(operationId.ToString());
        return await ReloadAsync(projectId, ClaimId.From(existing.ElementId), existing.ResultRevision, cancellationToken);
    }

    private async ValueTask<DefineEvidencePacketResult> ReloadOperationAsync(ChangeSetId operationId,
        string fingerprint, ProjectId projectId, CancellationToken cancellationToken)
    {
        var existing = await elements.FindCommitByOperationAsync(operationId, cancellationToken);
        return existing is null ? throw new InvalidOperationException("An operation conflict could not be reloaded.")
            : await ExistingAsync(existing, operationId, fingerprint, projectId, cancellationToken);
    }

    private async ValueTask<DefineEvidencePacketResult> ReloadAsync(ProjectId projectId, ClaimId claimId,
        Revision revision, CancellationToken cancellationToken)
    {
        var packet = await traceability.FindEvidencePacketAsync(projectId, claimId, cancellationToken)
            ?? throw new InvalidOperationException("Committed evidence packet could not be reloaded.");
        return new DefineEvidencePacketResult.Defined(packet.Claim, packet.Evidence, revision.Value,
            "Inspect outcome coverage and change impact in the Traceability Atlas.");
    }

    private static HashSet<ElementId> KnownElementIds(ProjectModelSnapshot model)
    {
        var ids = model.Actors.Select(item => item.Id).Concat(model.Outcomes.Select(item => item.Outcome.Id))
            .Concat(model.Capabilities.Select(item => item.Id)).ToHashSet();
        foreach (var narrative in model.Narratives)
            foreach (var value in new[] { narrative.EpisodeId, narrative.ScenarioId, narrative.SceneId, narrative.InteractionId, narrative.IntentId, narrative.StepId, narrative.ObservationId }) Add(value);
        foreach (var state in model.StateLogic)
        {
            foreach (var value in new[] { state.StateId, state.FactId, state.RuleId, state.InvariantId, state.TransitionId }) Add(value);
            foreach (var result in state.Results) Add(result.Id);
        }
        foreach (var path in model.Paths) foreach (var value in new[] { path.BranchPathId, path.BranchConditionId, path.EffectId, path.RecoveryPathId, path.RecoveryConditionId }) Add(value);
        foreach (var context in model.SystemContexts.IsDefault ? [] : model.SystemContexts)
            foreach (var value in new[] { context.OwnedSystemId, context.ExternalSystemId, context.InterfaceId, context.BoundaryId, context.ContractId }) Add(value);
        return ids;
        void Add(string value) { if (ElementId.Parse(value) is SemanticResult<ElementId>.Accepted accepted) ids.Add(accepted.Value); }
    }

    private static Validated Validate(DefineEvidencePacketCommand command)
    {
        var errors = new List<SemanticError>();
        var projectId = ModelInputValidation.Accept(ProjectId.Parse(command.ProjectId), errors);
        var revision = ModelInputValidation.Accept(Revision.Parse(command.ExpectedRevision), errors);
        var operation = ModelInputValidation.Accept(ChangeSetId.Parse(command.OperationId), errors);
        var owner = ModelInputValidation.Accept(ElementId.Parse(command.OwnerId), errors);
        var elementIds = command.ElementIds.Select(value => ModelInputValidation.Accept(ElementId.Parse(value), errors))
            .Where(value => value is not null).Cast<ElementId>().Distinct().ToImmutableArray();
        var claimKind = EnumValue<ClaimKind>(command.ClaimKind, errors, "claim.kind.invalid");
        var claimStatus = EnumValue<ClaimStatus>(command.ClaimStatus, errors, "claim.status.invalid");
        var evidenceKind = EnumValue<EvidenceKind>(command.EvidenceKind, errors, "evidence.kind.invalid");
        var evidenceStatus = EnumValue<EvidenceStatus>(command.EvidenceStatus, errors, "evidence.status.invalid");
        var statement = ModelInputValidation.Accept(LogicStatement.Create(command.ClaimStatement), errors);
        var producer = ModelInputValidation.Accept(LogicTerm.Create(command.Producer), errors);
        var environment = ModelInputValidation.Accept(LogicTerm.Create(command.Environment), errors);
        var summary = ModelInputValidation.Accept(LogicStatement.Create(command.Summary), errors);
        var tags = Terms(command.Tags, errors, false); var limitations = Terms(command.Limitations, errors, false);
        var reason = ModelInputValidation.Accept(ChangeReason.Create(command.Reason), errors);
        if (elementIds.IsEmpty) errors.Add(new("claim.scope.required", "At least one semantic definition must be linked."));
        var draft = errors.Count == 0 ? new EvidencePacketDraft(claimKind!.Value, statement!, claimStatus!.Value,
            elementIds, owner!, tags, evidenceKind!.Value, evidenceStatus!.Value, producer!, environment!, summary!, limitations) : null;
        return new(projectId, revision, operation, draft, reason, errors);
    }

    private static ImmutableArray<LogicTerm> Terms(string input, List<SemanticError> errors, bool required)
    {
        var values = input.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (required && values.Length == 0) errors.Add(new("traceability.term.required", "At least one value is required."));
        return values.Select(value => ModelInputValidation.Accept(LogicTerm.Create(value), errors)).Where(value => value is not null).Cast<LogicTerm>().ToImmutableArray();
    }
    private static T? EnumValue<T>(string value, List<SemanticError> errors, string code) where T : struct, Enum
    { if (Enum.TryParse<T>(value, true, out var parsed)) return parsed; errors.Add(new(code, $"'{value}' is not supported.")); return null; }
    private sealed record Validated(ProjectId? ProjectId, Revision? ExpectedRevision, ChangeSetId? OperationId,
        EvidencePacketDraft? Draft, ChangeReason? Reason, IReadOnlyList<SemanticError> Errors);
}
