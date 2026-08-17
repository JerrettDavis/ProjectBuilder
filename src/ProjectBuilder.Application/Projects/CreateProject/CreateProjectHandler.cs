using System.Security.Cryptography;
using System.Text;
using ProjectBuilder.Domain.Modeling.Primitives;
using ProjectBuilder.Domain.Projects;

namespace ProjectBuilder.Application.Projects.CreateProject;

public sealed class CreateProjectHandler(
    IProjectCreationStore store,
    IProjectCreationAuthorizer authorizer,
    IProjectIdentitySource identities,
    IApplicationClock clock)
{
    public async ValueTask<CreateProjectResult> HandleAsync(
        CreateProjectCommand command,
        ProjectActor actor,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(actor.Subject))
        {
            return new CreateProjectResult.Denied("An authenticated actor is required.");
        }

        var validation = Validate(command);
        if (validation.Errors.Count > 0)
        {
            return new CreateProjectResult.Invalid(validation.Errors);
        }

        var workspaceId = validation.WorkspaceId!;
        var operationId = validation.OperationId!;
        var authorization = await authorizer.AuthorizeAsync(actor, workspaceId, cancellationToken);
        if (!authorization.IsAllowed)
        {
            return new CreateProjectResult.Denied(authorization.Reason);
        }

        var fingerprint = CreateFingerprint(command);
        var existing = await store.FindByOperationAsync(operationId, cancellationToken);
        if (existing is not null)
        {
            return string.Equals(existing.RequestFingerprint, fingerprint, StringComparison.Ordinal)
                ? Created(existing.Project)
                : new CreateProjectResult.IdempotencyConflict(command.OperationId);
        }

        if (await store.NameExistsAsync(workspaceId, validation.Name!, cancellationToken))
        {
            return new CreateProjectResult.DuplicateName(validation.Name!.Value);
        }

        var project = ProjectDefinition.Create(
            identities.NextProjectId(),
            workspaceId,
            validation.Name!,
            validation.Purpose!,
            validation.IntendedOutcome!,
            operationId,
            validation.Reason!,
            clock.GetCurrentTimestamp(),
            actor.Subject);

        if (await store.TrySaveAsync(project, fingerprint, cancellationToken))
        {
            return Created(project);
        }

        existing = await store.FindByOperationAsync(operationId, cancellationToken);
        if (existing is not null)
        {
            return string.Equals(existing.RequestFingerprint, fingerprint, StringComparison.Ordinal)
                ? Created(existing.Project)
                : new CreateProjectResult.IdempotencyConflict(command.OperationId);
        }

        return new CreateProjectResult.DuplicateName(validation.Name!.Value);
    }

    private static CreateProjectResult.Created Created(ProjectDefinition project) =>
        new(ToOverview(project), "Add the actors who participate in this outcome.");

    internal static ProjectOverview ToOverview(ProjectDefinition project) =>
        new(
            project.Id.ToString(),
            project.WorkspaceId.ToString(),
            project.Name.Value,
            project.Purpose.Value,
            project.IntendedOutcome.Value,
            project.Revision.Value,
            project.Creation.Reason.Value,
            project.Creation.OccurredAt.ToString());

    private static ValidatedCreation Validate(CreateProjectCommand command)
    {
        var errors = new List<SemanticError>();
        var workspaceId = Accept(WorkspaceId.Parse(command.WorkspaceId), errors);
        var operationId = Accept(ChangeSetId.Parse(command.OperationId), errors);
        var name = Accept(ElementName.Create(command.Name), errors);
        var purpose = Accept(ProjectPurpose.Create(command.Purpose), errors);
        var intendedOutcome = Accept(IntendedOutcome.Create(command.IntendedOutcome), errors);
        var reason = Accept(ChangeReason.Create(command.Reason), errors);

        return new ValidatedCreation(workspaceId, operationId, name, purpose, intendedOutcome, reason, errors);
    }

    private static T? Accept<T>(SemanticResult<T> result, List<SemanticError> errors)
        where T : class
    {
        if (result is SemanticResult<T>.Accepted accepted)
        {
            return accepted.Value;
        }

        errors.Add(((SemanticResult<T>.Rejected)result).Error);
        return null;
    }

    private static string CreateFingerprint(CreateProjectCommand command)
    {
        var canonical = string.Join(
            '\n',
            command.WorkspaceId,
            command.Name.Trim(),
            command.Purpose.Trim(),
            command.IntendedOutcome.Trim(),
            command.Reason.Trim());
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
    }

    private sealed record ValidatedCreation(
        WorkspaceId? WorkspaceId,
        ChangeSetId? OperationId,
        ElementName? Name,
        ProjectPurpose? Purpose,
        IntendedOutcome? IntendedOutcome,
        ChangeReason? Reason,
        IReadOnlyList<SemanticError> Errors);
}
