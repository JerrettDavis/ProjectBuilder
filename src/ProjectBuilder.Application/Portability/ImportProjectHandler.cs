using ProjectBuilder.Application.Projects.CreateProject;
using ProjectBuilder.Domain.Modeling.Elements;
using ProjectBuilder.Domain.Modeling.Primitives;
using ProjectBuilder.Domain.Modeling.Relations;
using ProjectBuilder.Domain.Projects;

namespace ProjectBuilder.Application.Portability;

public sealed class ImportProjectHandler(
    IPortableProjectCodec codec,
    IPortableProjectStore store,
    IProjectCreationAuthorizer authorizer,
    IApplicationClock clock)
{
    private static readonly HashSet<string> SupportedElementKinds = new(StringComparer.Ordinal)
    {
        "actor",
        "outcome",
    };

    public async ValueTask<ImportProjectResult> HandleAsync(
        ImportProjectCommand command,
        ProjectActor actor,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(actor.Subject))
        {
            return new ImportProjectResult.Denied("An authenticated actor is required.");
        }

        if (!Guid.TryParse(command.WorkspaceId, out var workspaceValue))
        {
            return Invalid("import.workspace.invalid", "$.workspaceId",
                "The workspace identifier is invalid.");
        }
        var workspaceResult = WorkspaceId.Create(workspaceValue);
        if (workspaceResult is SemanticResult<WorkspaceId>.Rejected workspaceError)
            return Invalid("import.workspace.invalid", "$.workspaceId", workspaceError.Error.Message);
        var workspaceId = ((SemanticResult<WorkspaceId>.Accepted)workspaceResult).Value;
        var authorization = await authorizer.AuthorizeAsync(actor, workspaceId, cancellationToken);
        if (!authorization.IsAllowed)
        {
            return new ImportProjectResult.Denied(authorization.Reason);
        }

        if (!Guid.TryParse(command.OperationId, out var operationId))
        {
            return Invalid("import.operation.invalid", "$.operationId", "A canonical GUID operation identifier is required.");
        }

        var reason = ChangeReason.Create(command.Reason);
        if (reason is SemanticResult<ChangeReason>.Rejected rejectedReason)
        {
            return Invalid(rejectedReason.Error.Code, "$.reason", rejectedReason.Error.Message);
        }

        var read = codec.Read(command.Document);
        if (read is PortableProjectReadResult.Rejected rejected)
        {
            return new ImportProjectResult.Invalid(rejected.Findings);
        }

        var accepted = (PortableProjectReadResult.Accepted)read;
        var findings = ValidateSupportedModel(accepted.Document);
        if (findings.Count > 0)
        {
            return new ImportProjectResult.Invalid(findings);
        }

        var result = await store.ImportAsync(
            workspaceValue,
            new PortableProjectImport(
                accepted.Document,
                accepted.CanonicalJson,
                accepted.ContentHash,
                ((SemanticResult<ChangeReason>.Accepted)reason).Value.Value,
                actor,
                clock.GetCurrentTimestamp().Value,
                operationId),
            cancellationToken);

        return result switch
        {
            PortableImportStoreResult.Imported imported => new ImportProjectResult.Imported(imported.Project),
            PortableImportStoreResult.DuplicateProject => new ImportProjectResult.DuplicateProject(),
            PortableImportStoreResult.OperationConflict => new ImportProjectResult.OperationConflict(),
            _ => throw new InvalidOperationException("Unknown portable import store result."),
        };
    }

    private static List<PortableProjectFinding> ValidateSupportedModel(PortableProjectDocument document)
    {
        var findings = new List<PortableProjectFinding>();
        Validate(ProjectId.Create(document.Project.Id), "$.project.id", findings);
        Validate(ElementName.Create(document.Project.Name), "$.project.name", findings);
        Validate(ProjectPurpose.Create(document.Project.Purpose), "$.project.purpose", findings);
        Validate(Revision.Create(document.Project.Revision), "$.project.revision", findings);

        if (document.Project.Tags.Count > 0)
            findings.Add(Unsupported("$.project.tags", "Project tags are not yet live-persisted."));
        if (document.Views.Count > 0)
            findings.Add(Unsupported("$.views", "View definitions are not yet live-persisted."));
        if (document.Claims.Count > 0)
            findings.Add(Unsupported("$.claims", "Claims are not yet live-persisted."));
        if (document.Evidence.Count > 0)
            findings.Add(Unsupported("$.evidence", "Evidence records are not yet live-persisted."));

        var actors = document.Elements.Where(element => element.Kind == "actor").ToDictionary(element => element.Id);
        var outcomes = document.Elements.Where(element => element.Kind == "outcome").ToDictionary(element => element.Id);
        if (document.Elements.GroupBy(element => element.Order).Any(group => group.Count() > 1))
            findings.Add(new("import.order.duplicate", "$.elements", "Live-imported element order values must be unique."));

        for (var index = 0; index < document.Elements.Count; index++)
        {
            var element = document.Elements[index];
            var path = $"$.elements[{index}]";
            if (!SupportedElementKinds.Contains(element.Kind))
            {
                findings.Add(Unsupported(path + ".kind", $"Element kind '{element.Kind}' has no owned live import behavior."));
                continue;
            }

            Validate(ElementId.Create(element.Id), path + ".id", findings);
            Validate(ElementName.Create(element.Name), path + ".name", findings);
            if (element.ProjectId != document.Project.Id)
                findings.Add(new("import.reference.project", path + ".projectId", "Element projectId must match the imported project."));
            if (element.ParentId is not null)
                findings.Add(Unsupported(path + ".parentId", "Actor and Outcome imports must be root elements."));
            if (element.Tags.Count > 0 || element.Sources.Count > 0)
                findings.Add(Unsupported(path, "Element tags and sources are not yet live-persisted."));
            if (element.Version != 1 || element.DefinitionStatus != "defined" || element.KnowledgeStatus != "known")
                findings.Add(Unsupported(path, "The current live profile requires defined, known, version-1 elements."));

            if (element.Kind == "actor") ValidateActor(element, path, findings);
            if (element.Kind == "outcome") ValidateOutcome(element, path, findings);
        }

        if (document.Project.IntendedOutcomeIds.Count == 0 ||
            document.Project.IntendedOutcomeIds.Any(id => !outcomes.ContainsKey(id)))
        {
            findings.Add(new("import.reference.outcome", "$.project.intendedOutcomeIds",
                "Every intended outcome identifier must resolve to an imported Outcome."));
        }

        var targets = new HashSet<Guid>();
        for (var index = 0; index < document.Relations.Count; index++)
        {
            var relation = document.Relations[index];
            var path = $"$.relations[{index}]";
            if (relation.Kind != "benefitsFrom")
            {
                findings.Add(Unsupported(path + ".kind", $"Relation kind '{relation.Kind}' has no owned live import behavior."));
                continue;
            }
            if (!actors.ContainsKey(relation.SourceId) || !outcomes.ContainsKey(relation.TargetId))
                findings.Add(new("PB-REF-002", path, "benefitsFrom must point from an imported Actor to an imported Outcome."));
            if (!targets.Add(relation.TargetId))
                findings.Add(new("PB-REF-003", path + ".targetId", "An Outcome can have only one benefitsFrom source."));
            if (relation.ProjectId != document.Project.Id)
                findings.Add(new("import.reference.project", path + ".projectId", "Relation projectId must match the imported project."));
            if (relation.Version != 1 || relation.DefinitionStatus != "defined" || relation.KnowledgeStatus != "known")
                findings.Add(Unsupported(path, "The current live profile requires defined, known, version-1 relations."));
        }

        foreach (var outcome in outcomes.Values)
        {
            var beneficiaries = Strings(outcome.Payload, "beneficiaryIds");
            var relation = document.Relations.SingleOrDefault(candidate => candidate.TargetId == outcome.Id && candidate.Kind == "benefitsFrom");
            if (beneficiaries.Length != 1 || relation is null || beneficiaries[0] != relation.SourceId.ToString())
                findings.Add(new("import.reference.beneficiary", "$.elements[*].payload.beneficiaryIds",
                    "Outcome beneficiaryIds must match its single benefitsFrom relation."));
        }

        return findings;
    }

    private static void ValidateActor(PortableElement element, string path, List<PortableProjectFinding> findings)
    {
        Validate(ContextualRole.Create(element.Description), path + ".description", findings);
        var kind = Text(element.Payload, "actorKind");
        if (!Enum.TryParse<ActorKind>(kind, true, out _))
            findings.Add(new("actor.kind.invalid", path + ".payload.actorKind", $"Actor kind '{kind}' is invalid."));
        foreach (var property in new[] { "goals", "responsibilities", "authority", "constraints" })
            foreach (var statement in Strings(element.Payload, property))
                Validate(ActorStatement.Create(statement), path + $".payload.{property}", findings);
    }

    private static void ValidateOutcome(PortableElement element, string path, List<PortableProjectFinding> findings)
    {
        Validate(OutcomeStatement.Create(Text(element.Payload, "statement")), path + ".payload.statement", findings);
        foreach (var signal in Strings(element.Payload, "successSignals"))
            Validate(SuccessSignal.Create(signal), path + ".payload.successSignals", findings);
    }

    private static string Text(System.Text.Json.JsonElement payload, string property) =>
        payload.TryGetProperty(property, out var value) && value.ValueKind == System.Text.Json.JsonValueKind.String
            ? value.GetString() ?? string.Empty
            : string.Empty;

    private static string[] Strings(System.Text.Json.JsonElement payload, string property) =>
        payload.TryGetProperty(property, out var value) && value.ValueKind == System.Text.Json.JsonValueKind.Array
            ? value.EnumerateArray().Select(item => item.GetString() ?? string.Empty).ToArray()
            : [];

    private static void Validate<T>(SemanticResult<T> result, string path, List<PortableProjectFinding> findings)
        where T : notnull
    {
        if (result is SemanticResult<T>.Rejected rejected)
            findings.Add(new(rejected.Error.Code, path, rejected.Error.Message));
    }

    private static PortableProjectFinding Unsupported(string path, string message) =>
        new("import.compatibility.unsupported", path, message);

    private static ImportProjectResult.Invalid Invalid(string code, string path, string message) =>
        new([new(code, path, message)]);
}
