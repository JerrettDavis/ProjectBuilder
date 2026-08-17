using System.Collections.Immutable;
using System.Globalization;
using ProjectBuilder.Application.Projects.CreateProject;
using ProjectBuilder.Domain.Modeling.Elements;
using ProjectBuilder.Domain.Modeling.Primitives;
using ProjectBuilder.Domain.Modeling.Transitions;

namespace ProjectBuilder.Application.Modeling.DefineSystemContext;

public sealed class DefineSystemContextHandler(
    IProjectCreationStore projects, IProjectElementStore elements, IProjectEditAuthorizer authorizer,
    IModelIdentitySource identities, IApplicationClock clock)
{
    public async ValueTask<DefineSystemContextResult> HandleAsync(
        DefineSystemContextCommand command, ProjectActor actor, CancellationToken cancellationToken = default)
    {
        var validation = Validate(command);
        if (validation.Errors.Count > 0) return new DefineSystemContextResult.Invalid(validation.Errors);
        var project = await projects.FindByIdAsync(validation.ProjectId!, cancellationToken);
        if (project is null) return new DefineSystemContextResult.ProjectNotFound(command.ProjectId);
        var access = await authorizer.AuthorizeEditAsync(actor, project.WorkspaceId, cancellationToken);
        if (!access.IsAllowed) return new DefineSystemContextResult.Denied(access.Reason);

        var model = await elements.LoadModelAsync(project.Id, cancellationToken);
        var actorIds = model.Actors.Select(item => item.Id).ToHashSet();
        var referencedActors = new[] { validation.Draft!.OwnedSystemOwnerId, validation.Draft.ExternalSystemOwnerId,
            validation.Draft.ContractOwnerId }.Concat(validation.Draft.ActorParticipantIds).Concat(validation.Draft.BoundaryOwnerIds);
        if (referencedActors.Any(id => !actorIds.Contains(id)))
            return new DefineSystemContextResult.ReferenceNotFound("system context actor");
        if (validation.Draft.CrossingEffectId is { } effectId && model.Paths.All(path => path.EffectId != effectId.ToString()))
            return new DefineSystemContextResult.ReferenceNotFound("boundary crossing effect");

        var fingerprint = ModelRequestFingerprint.Create(
            command.ProjectId, command.ExpectedRevision, command.OwnedSystemName, command.OwnedSystemPurpose,
            command.OwnedSystemOwnerId, command.OwnedResponsibilities, command.ExternalSystemName,
            command.ExternalSystemPurpose, command.ExternalSystemOwnerId, command.ExternalResponsibilities,
            command.ExternalKnowledgeStatus, command.InterfaceName, command.InterfaceDescription,
            command.InterfaceKind, string.Join('\n', command.ParticipantIds), command.AcceptedIntents, command.Observations,
            command.AccessibilityConstraints, command.BoundaryName, command.BoundaryDescription,
            string.Join('\n', command.BoundaryKinds), string.Join('\n', command.BoundaryOwnerIds), command.BoundaryKnowledgeStatus,
            command.CrossingEffectId, command.ContractName, command.ContractDescription, command.ContractKind,
            command.ContractVersion, command.ContractOwnerId, command.SchemaReference, command.CompatibilityPolicy,
            command.RequestData, command.ResponseData, command.DataClassification,
            command.ContractKnowledgeStatus, command.Reason);
        var prior = await elements.FindCommitByOperationAsync(validation.OperationId!, cancellationToken);
        if (prior is not null) return await ExistingAsync(prior, validation.OperationId!, fingerprint, project.Id, cancellationToken);

        var ids = new SystemContextIds(identities.NextElementId(), identities.NextElementId(),
            identities.NextElementId(), identities.NextElementId(), identities.NextElementId());
        var transitioned = SystemContextTransition.Define(project, validation.ExpectedRevision!, ids,
            validation.Draft, await elements.NextElementOrderAsync(project.Id, cancellationToken),
            validation.OperationId!, validation.Reason!, clock.GetCurrentTimestamp(), actor.Subject);
        if (transitioned is DefineSystemContextTransitionResult.Conflict conflict)
            return new DefineSystemContextResult.Conflict(conflict.Expected.Value, conflict.Actual.Value,
                ModelApplicationMapping.Conflicts(conflict.Conflicts));
        if (transitioned is DefineSystemContextTransitionResult.Invalid invalid)
            return new DefineSystemContextResult.Invalid(invalid.Errors);
        var accepted = (DefineSystemContextTransitionResult.Accepted)transitioned;
        var stored = await elements.CommitSystemContextAsync(accepted, fingerprint, cancellationToken);
        return stored switch
        {
            ElementStoreCommitResult.Committed => await ReloadAsync(project.Id, accepted.Definitions.OwnedSystem.Id, accepted.Project.Revision, cancellationToken),
            ElementStoreCommitResult.RevisionConflict storeConflict => new DefineSystemContextResult.Conflict(
                validation.ExpectedRevision!.Value, storeConflict.Actual.Value,
                ModelApplicationMapping.RevisionConflict(validation.ExpectedRevision!, storeConflict.Actual)),
            ElementStoreCommitResult.OperationConflict => await ReloadOperationAsync(validation.OperationId!, fingerprint, project.Id, cancellationToken),
            _ => throw new InvalidOperationException("Unknown element store result."),
        };
    }

    private async ValueTask<DefineSystemContextResult> ExistingAsync(StoredElementCommit existing, ChangeSetId operationId,
        string fingerprint, ProjectId projectId, CancellationToken cancellationToken)
    {
        if (existing.ChangeKind != "system-context.defined" || !string.Equals(existing.RequestFingerprint, fingerprint, StringComparison.Ordinal))
            return new DefineSystemContextResult.IdempotencyConflict(operationId.ToString());
        return await ReloadAsync(projectId, existing.ElementId, existing.ResultRevision, cancellationToken);
    }

    private async ValueTask<DefineSystemContextResult> ReloadOperationAsync(ChangeSetId id, string fingerprint,
        ProjectId projectId, CancellationToken cancellationToken)
    {
        var existing = await elements.FindCommitByOperationAsync(id, cancellationToken);
        return existing is null ? throw new InvalidOperationException("An operation conflict could not be reloaded.")
            : await ExistingAsync(existing, id, fingerprint, projectId, cancellationToken);
    }

    private async ValueTask<DefineSystemContextResult> ReloadAsync(ProjectId projectId, ElementId systemId,
        Revision revision, CancellationToken cancellationToken)
    {
        var context = await elements.FindSystemContextAsync(projectId, systemId, cancellationToken)
            ?? throw new InvalidOperationException("Committed system context could not be reloaded.");
        return new DefineSystemContextResult.Defined(context, revision.Value, "Review ownership, trust, and data movement in the System Context lens.");
    }

    private static Validated Validate(DefineSystemContextCommand command)
    {
        var errors = new List<SemanticError>();
        var projectId = ModelInputValidation.Accept(ProjectId.Parse(command.ProjectId), errors);
        var expected = ModelInputValidation.Accept(Revision.Parse(command.ExpectedRevision), errors);
        var operation = ModelInputValidation.Accept(ChangeSetId.Parse(command.OperationId), errors);
        var ownedOwner = ModelInputValidation.Accept(ElementId.Parse(command.OwnedSystemOwnerId), errors);
        var externalOwner = ModelInputValidation.Accept(ElementId.Parse(command.ExternalSystemOwnerId), errors);
        var contractOwner = ModelInputValidation.Accept(ElementId.Parse(command.ContractOwnerId), errors);
        var participants = Ids(command.ParticipantIds, errors, "interface.participant");
        var boundaryOwners = Ids(command.BoundaryOwnerIds, errors, "boundary.owner");
        ElementId? effect = string.IsNullOrWhiteSpace(command.CrossingEffectId) ? null
            : ModelInputValidation.Accept(ElementId.Parse(command.CrossingEffectId), errors);
        var ownedName = ModelInputValidation.Accept(ElementName.Create(command.OwnedSystemName), errors);
        var ownedPurpose = ModelInputValidation.Accept(Description.Create(command.OwnedSystemPurpose), errors);
        var externalName = ModelInputValidation.Accept(ElementName.Create(command.ExternalSystemName), errors);
        var externalPurpose = ModelInputValidation.Accept(Description.Create(command.ExternalSystemPurpose), errors);
        var interfaceName = ModelInputValidation.Accept(ElementName.Create(command.InterfaceName), errors);
        var interfaceDescription = ModelInputValidation.Accept(Description.Create(command.InterfaceDescription), errors);
        var boundaryName = ModelInputValidation.Accept(ElementName.Create(command.BoundaryName), errors);
        var boundaryDescription = ModelInputValidation.Accept(Description.Create(command.BoundaryDescription), errors);
        var contractName = ModelInputValidation.Accept(ElementName.Create(command.ContractName), errors);
        var contractDescription = ModelInputValidation.Accept(Description.Create(command.ContractDescription), errors);
        var interfaceKind = EnumValue<InterfaceKind>(command.InterfaceKind, errors, "interface.kind.invalid");
        var boundaryKinds = EnumValues<BoundaryKind>(command.BoundaryKinds, errors, "boundary.kind.invalid");
        var contractKind = EnumValue<ContractKind>(command.ContractKind, errors, "contract.kind.invalid");
        var externalKnowledge = EnumValue<KnowledgeStatus>(command.ExternalKnowledgeStatus, errors, "system.knowledge.invalid");
        var boundaryKnowledge = EnumValue<KnowledgeStatus>(command.BoundaryKnowledgeStatus, errors, "boundary.knowledge.invalid");
        var contractKnowledge = EnumValue<KnowledgeStatus>(command.ContractKnowledgeStatus, errors, "contract.knowledge.invalid");
        var ownedResponsibilities = Terms(command.OwnedResponsibilities, errors);
        var externalResponsibilities = Terms(command.ExternalResponsibilities, errors);
        var acceptedIntents = Terms(command.AcceptedIntents, errors);
        var observations = Terms(command.Observations, errors);
        var accessibility = Terms(command.AccessibilityConstraints, errors, false);
        var contractVersion = ModelInputValidation.Accept(LogicTerm.Create(command.ContractVersion), errors);
        var schemaReference = ModelInputValidation.Accept(LogicStatement.Create(command.SchemaReference), errors);
        var compatibilityPolicy = ModelInputValidation.Accept(LogicStatement.Create(command.CompatibilityPolicy), errors);
        var requestData = ModelInputValidation.Accept(LogicTerm.Create(command.RequestData), errors);
        var responseData = ModelInputValidation.Accept(LogicTerm.Create(command.ResponseData), errors);
        var dataClassification = ModelInputValidation.Accept(LogicTerm.Create(command.DataClassification), errors);
        var reason = ModelInputValidation.Accept(ChangeReason.Create(command.Reason), errors);
        var draft = errors.Count == 0 ? new SystemContextDraft(
            ownedName!, ownedPurpose!, ownedOwner!, ownedResponsibilities,
            externalName!, externalPurpose!, externalOwner!, externalResponsibilities, externalKnowledge!.Value,
            interfaceName!, interfaceDescription!, interfaceKind!.Value, participants, acceptedIntents,
            observations, accessibility,
            boundaryName!, boundaryDescription!, boundaryKinds, boundaryOwners, boundaryKnowledge!.Value, effect,
            contractName!, contractDescription!, contractKind!.Value,
            contractVersion!, contractOwner!, schemaReference!, compatibilityPolicy!, requestData!, responseData!,
            dataClassification!, contractKnowledge!.Value) : null;
        return new(projectId, expected, operation, draft, reason, errors);
    }

    private static ImmutableArray<ElementId> Ids(IEnumerable<string> values, List<SemanticError> errors, string code)
    {
        var result = values.Select(value => ModelInputValidation.Accept(ElementId.Parse(value), errors)).Where(value => value is not null).Cast<ElementId>().Distinct().ToImmutableArray();
        if (result.IsEmpty) errors.Add(new(code + ".required", "At least one reference is required."));
        return result;
    }

    private static ImmutableArray<LogicTerm> Terms(string input, List<SemanticError> errors, bool required = true)
    {
        var lines = input.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (required && lines.Length == 0) errors.Add(new("system-context.term.required", "At least one value is required."));
        return lines.Select(value => ModelInputValidation.Accept(LogicTerm.Create(value), errors)).Where(value => value is not null).Cast<LogicTerm>().ToImmutableArray();
    }

    private static T? EnumValue<T>(string value, List<SemanticError> errors, string code) where T : struct, Enum
    {
        if (Enum.TryParse<T>(value, true, out var parsed)) return parsed;
        errors.Add(new(code, $"'{value}' is not a supported {typeof(T).Name}."));
        return null;
    }

    private static ImmutableArray<T> EnumValues<T>(IEnumerable<string> values, List<SemanticError> errors, string code) where T : struct, Enum
    {
        var result = values.Select(value => EnumValue<T>(value, errors, code)).Where(value => value.HasValue).Select(value => value!.Value).Distinct().ToImmutableArray();
        if (result.IsEmpty) errors.Add(new(code + ".required", "At least one boundary kind is required."));
        return result;
    }

    private sealed record Validated(ProjectId? ProjectId, Revision? ExpectedRevision, ChangeSetId? OperationId,
        SystemContextDraft? Draft, ChangeReason? Reason, IReadOnlyList<SemanticError> Errors);
}
