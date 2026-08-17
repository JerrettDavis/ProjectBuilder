using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ProjectBuilder.Application.Collaboration.GetProjectWorkshop;
using ProjectBuilder.Application.Guidance.GetProjectGuidance;
using ProjectBuilder.Application.Modeling;
using ProjectBuilder.Application.Modeling.AddActor;
using ProjectBuilder.Application.Modeling.AddCapability;
using ProjectBuilder.Application.Modeling.AddOutcome;
using ProjectBuilder.Application.Modeling.DefineNarrative;
using ProjectBuilder.Application.Modeling.DefinePath;
using ProjectBuilder.Application.Modeling.DefineStateLogic;
using ProjectBuilder.Application.Modeling.DefineSystemContext;
using ProjectBuilder.Application.Modeling.GetProjectModel;
using ProjectBuilder.Application.Modeling.UpdateActor;
using ProjectBuilder.Application.Modeling.UpdateOutcome;
using ProjectBuilder.Application.Portability;
using ProjectBuilder.Application.Projects.CreateProject;
using ProjectBuilder.Application.Projects.GetProject;
using ProjectBuilder.Application.Traceability;
using ProjectBuilder.Application.Traceability.DefineEvidencePacket;
using ProjectBuilder.Application.Validation.GetProjectFindings;
using ProjectBuilder.Application.Validation.GetProjectRecommendations;
using ProjectBuilder.Application.Validation.RecordGapDisposition;
using ProjectBuilder.Application.Views;
using ProjectBuilder.Contracts.Projects;
using ProjectBuilder.Projections.Lenses;

namespace ProjectBuilder.Web.Projects;

internal static class ProjectEndpoints
{
    internal static IEndpointRouteBuilder MapProjectEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/v1/workspaces/current", (LocalDevelopmentProjectAccess access) =>
                new LocalWorkspaceResponse(access.WorkspaceId.ToString(), "Local workspace", "development-only"))
            .WithName("GetCurrentWorkspace");

        endpoints.MapPost(
                "/api/v1/workspaces/{workspaceId}/projects",
                CreateProjectAsync)
            .WithName("CreateProject")
            .Produces<ProjectResponse>(StatusCodes.Status201Created)
            .Produces<ProjectProblemResponse>(StatusCodes.Status400BadRequest)
            .Produces<ProjectProblemResponse>(StatusCodes.Status403Forbidden)
            .Produces<ProjectProblemResponse>(StatusCodes.Status409Conflict)
            .Produces<ProjectProblemResponse>(StatusCodes.Status422UnprocessableEntity);

        endpoints.MapGet("/api/v1/projects/{projectId}", GetProjectAsync)
            .WithName("GetProject")
            .Produces<ProjectResponse>()
            .Produces<ProjectProblemResponse>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);

        endpoints.MapGet("/api/v1/projects/{projectId}/model", GetModelAsync)
            .WithName("GetProjectModel");

        endpoints.MapGet("/api/v1/projects/{projectId}/lens", GetLensAsync)
            .WithName("GetProjectLens")
            .Produces<LensProjectionResponse>()
            .Produces<ProjectProblemResponse>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);

        endpoints.MapGet("/api/v1/projects/{projectId}/lenses/story-map", GetStoryMapAsync)
            .WithName("GetProjectStoryMap")
            .Produces<LensProjectionResponse>()
            .Produces<ProjectProblemResponse>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);

        endpoints.MapGet("/api/v1/projects/{projectId}/lenses/scenario-flow/{scenarioId}", GetScenarioFlowAsync)
            .WithName("GetProjectScenarioFlow")
            .Produces<ScenarioFlowProjectionResponse>()
            .Produces<ProjectProblemResponse>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);

        endpoints.MapGet("/api/v1/projects/{projectId}/lenses/state-rule/{stateId}", GetStateRuleAsync)
            .WithName("GetProjectStateRule")
            .Produces<StateRuleProjectionResponse>()
            .Produces<ProjectProblemResponse>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);

        endpoints.MapGet("/api/v1/projects/{projectId}/lenses/system-context/{systemId}", GetSystemContextAsync)
            .WithName("GetProjectSystemContext")
            .Produces<SystemContextProjectionResponse>()
            .Produces<ProjectProblemResponse>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);

        endpoints.MapGet("/api/v1/projects/{projectId}/lenses/traceability", GetTraceabilityAsync)
            .WithName("GetProjectTraceability").Produces<TraceabilityProjectionResponse>();

        endpoints.MapGet("/api/v1/projects/{projectId}/findings", GetFindingsAsync)
            .WithName("GetProjectFindings");

        endpoints.MapGet("/api/v1/projects/{projectId}/recommendations", GetRecommendationsAsync)
            .WithName("GetProjectRecommendations");

        endpoints.MapGet("/api/v1/projects/{projectId}/workshop", GetWorkshopAsync)
            .WithName("GetProjectWorkshop");

        endpoints.MapGet("/api/v1/projects/{projectId}/guidance", GetGuidanceAsync)
            .WithName("GetProjectGuidance");

        endpoints.MapPost("/api/v1/workspaces/{workspaceId}/projects/import", ImportProjectAsync)
            .WithName("ImportProject");

        endpoints.MapGet("/api/v1/projects/{projectId}/export", ExportProjectAsync)
            .WithName("ExportProject");

        endpoints.MapPost("/api/v1/projects/{projectId}/actors", AddActorAsync)
            .WithName("AddActor");

        endpoints.MapPut("/api/v1/projects/{projectId}/actors/{actorId}", UpdateActorAsync)
            .WithName("UpdateActor");

        endpoints.MapPost("/api/v1/projects/{projectId}/outcomes", AddOutcomeAsync)
            .WithName("AddOutcome");

        endpoints.MapPost("/api/v1/projects/{projectId}/capabilities", AddCapabilityAsync)
            .WithName("AddCapability");

        endpoints.MapPut("/api/v1/projects/{projectId}/outcomes/{outcomeId}", UpdateOutcomeAsync)
            .WithName("UpdateOutcome");

        endpoints.MapPost("/api/v1/projects/{projectId}/narratives", DefineNarrativeAsync)
            .WithName("DefineNarrative");

        endpoints.MapPost("/api/v1/projects/{projectId}/state-logic", DefineStateLogicAsync)
            .WithName("DefineStateLogic");

        endpoints.MapPost("/api/v1/projects/{projectId}/paths", DefinePathAsync)
            .WithName("DefinePath");

        endpoints.MapPost("/api/v1/projects/{projectId}/system-contexts", DefineSystemContextAsync)
            .WithName("DefineSystemContext");

        endpoints.MapPost("/api/v1/projects/{projectId}/evidence-packets", DefineEvidencePacketAsync)
            .WithName("DefineEvidencePacket");

        endpoints.MapGet("/api/v1/projects/{projectId}/canvas-view", GetCanvasViewAsync)
            .WithName("GetCanvasView");

        endpoints.MapPut("/api/v1/projects/{projectId}/canvas-view", SaveCanvasViewAsync)
            .WithName("SaveCanvasView");

        endpoints.MapDelete("/api/v1/projects/{projectId}/canvas-view", ResetCanvasViewAsync)
            .WithName("ResetCanvasView");

        endpoints.MapPost("/api/v1/projects/{projectId}/gap-dispositions", RecordGapDispositionAsync)
            .WithName("RecordGapDisposition");

        return endpoints;
    }

    private static async Task<IResult> GetCanvasViewAsync(
        string projectId, [FromQuery] string? lens, [FromQuery] string? scopeKey,
        [FromQuery] string? visibility, CanvasViewHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.GetAsync(projectId, lens ?? "custom", scopeKey ?? "project-definition",
            visibility ?? "personal", LocalDevelopmentProjectAccess.ActorSubject, cancellationToken);
        return result switch
        {
            CanvasViewResult.Found found => Results.Ok(ToResponse(found.View, found.SemanticRevision)),
            CanvasViewResult.Missing => Results.NoContent(),
            CanvasViewResult.Invalid invalid => Results.BadRequest(new ProjectProblemResponse(
                "canvas-view.invalid", "Canvas view request is invalid.", invalid.Errors)),
            CanvasViewResult.ProjectNotFound => Results.NotFound(),
            _ => throw new UnreachableException(),
        };
    }

    private static async Task<IResult> SaveCanvasViewAsync(
        string projectId, [FromBody] SaveCanvasViewRequest request,
        CanvasViewHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.SaveAsync(new SaveCanvasViewCommand(projectId, request.Name,
            request.Lens, request.ScopeKey, request.Visibility, request.ModelRevision,
            request.ExpectedLayoutVersion, new CanvasLayoutOverview(
                new(request.Layout.Viewport.X, request.Layout.Viewport.Y, request.Layout.Viewport.Zoom),
                request.Layout.Alignment,
                [.. request.Layout.Nodes.Select(node => new CanvasNodePlacementOverview(
                    node.ElementId, node.X, node.Y, node.Width, node.Height, node.Collapsed))],
                request.Layout.InputHash), LocalDevelopmentProjectAccess.ActorSubject), cancellationToken);
        return result switch
        {
            CanvasViewResult.Saved saved => Results.Ok(ToResponse(saved.View, saved.SemanticRevision)),
            CanvasViewResult.Invalid invalid => Results.BadRequest(new ProjectProblemResponse(
                "canvas-view.invalid", "Canvas view request is invalid.", invalid.Errors)),
            CanvasViewResult.Conflict conflict => Results.Conflict(new ProjectProblemResponse(
                "canvas-view.version.conflict", "The saved view changed after this layout was loaded.",
                new Dictionary<string, string[]>
                {
                    ["layoutVersion"] =
                    [$"Expected layout version {conflict.ExpectedLayoutVersion}; actual version is {conflict.ActualLayoutVersion}."]
                })),
            CanvasViewResult.ProjectNotFound => Results.NotFound(),
            _ => throw new UnreachableException(),
        };
    }

    private static async Task<IResult> ResetCanvasViewAsync(
        string projectId, [FromBody] ResetCanvasViewRequest request,
        CanvasViewHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.ResetAsync(new ResetCanvasViewCommand(projectId, request.Lens,
            request.ScopeKey, request.Visibility, request.ExpectedLayoutVersion,
            LocalDevelopmentProjectAccess.ActorSubject), cancellationToken);
        return result switch
        {
            CanvasViewResult.Reset reset => Results.Ok(new { reset.SemanticRevision }),
            CanvasViewResult.Missing missing => Results.Ok(new { missing.SemanticRevision }),
            CanvasViewResult.Invalid invalid => Results.BadRequest(new ProjectProblemResponse(
                "canvas-view.invalid", "Canvas view request is invalid.", invalid.Errors)),
            CanvasViewResult.Conflict conflict => Results.Conflict(new ProjectProblemResponse(
                "canvas-view.version.conflict", "The saved view changed after this layout was loaded.",
                new Dictionary<string, string[]>
                {
                    ["layoutVersion"] =
                    [$"Expected layout version {conflict.ExpectedLayoutVersion}; actual version is {conflict.ActualLayoutVersion}."]
                })),
            CanvasViewResult.ProjectNotFound => Results.NotFound(),
            _ => throw new UnreachableException(),
        };
    }

    private static CanvasViewResponse ToResponse(CanvasViewOverview view, long semanticRevision) => new(
        view.Id, view.ProjectId, view.Name, view.Lens, view.ScopeKey, view.Visibility, view.OwnerKey,
        view.ModelRevision, view.LayoutVersion, new CanvasLayoutRequest(
            new(view.Layout.Viewport.X, view.Layout.Viewport.Y, view.Layout.Viewport.Zoom),
            view.Layout.Alignment,
            [.. view.Layout.Nodes.Select(node => new CanvasNodePlacementRequest(
                node.ElementId, node.X, node.Y, node.Width, node.Height, node.Collapsed))],
            view.Layout.InputHash), view.UpdatedAt, view.UpdatedBy, view.IsStale, semanticRevision);

    private static async Task<IResult> GetGuidanceAsync(
        string projectId, GetProjectGuidanceHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(projectId, cancellationToken);
        return result switch
        {
            GetProjectGuidanceResult.Found found => Results.Ok(new ProjectGuidanceResponse(
                found.Guidance.ProjectId, found.Guidance.ProjectName, found.Guidance.Revision,
                found.Guidance.RegistryVersion,
                [.. found.Guidance.Stages.Select(stage => new GuidanceStageResponse(
                    stage.Id, stage.Name, stage.Status, stage.ApplicablePromptCount, stage.Explanation))],
                [.. found.Guidance.Prompts.Select(prompt => new GuidancePromptResponse(
                    prompt.Id, prompt.Version, prompt.Stage, prompt.Order, prompt.Question,
                    prompt.WhyThisMatters, prompt.LearningContent, prompt.TriggerExplanation,
                    prompt.RelatedFactKinds, prompt.Examples,
                    [.. prompt.AnswerMappings.Select(answer => new GuidanceAnswerResponse(
                        answer.Key, answer.Label, answer.Kind, answer.ResultingChange,
                        answer.RequiresRationale, answer.RepairPath))], prompt.PrimaryRepairPath))])),
            GetProjectGuidanceResult.Invalid invalid => Results.BadRequest(Problem(invalid.Code, invalid.Message)),
            GetProjectGuidanceResult.NotFound => Results.NotFound(),
            _ => throw new UnreachableException(),
        };
    }

    private static async Task<IResult> RecordGapDispositionAsync(
        string projectId, [FromBody] RecordGapDispositionRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? operationId,
        [FromHeader(Name = "If-Match")] string? revision,
        RecordGapDispositionHandler handler, CancellationToken cancellationToken)
    {
        var headerProblem = RequiredEditHeaders(operationId, revision);
        if (headerProblem is not null) return headerProblem;
        var result = await handler.HandleAsync(new RecordGapDispositionCommand(
            projectId, RevisionText(revision!), operationId!, request.ProfileId, request.RuleCode,
            request.ScopeId, request.Disposition, request.Rationale, request.Consequence,
            request.AuthorityActorId, request.ReviewOn, request.TargetMilestone, request.Reason),
            new ProjectActor(LocalDevelopmentProjectAccess.ActorSubject), cancellationToken);
        return result switch
        {
            RecordGapDispositionResult.Recorded recorded => Results.Created(
                $"/api/v1/projects/{projectId}/gap-dispositions/{recorded.Disposition.Id}",
                new GapDispositionResponse(
                    recorded.Disposition.Id, recorded.Disposition.ProfileId, recorded.Disposition.RuleCode,
                    recorded.Disposition.ScopeId, recorded.Disposition.Disposition, recorded.Disposition.Rationale,
                    recorded.Disposition.Consequence, recorded.Disposition.AuthorityActorId,
                    recorded.Disposition.AuthorityName, recorded.Disposition.ReviewOn,
                    recorded.Disposition.TargetMilestone, recorded.Disposition.CreatedAt,
                    recorded.Disposition.CreatedBy, recorded.Revision)),
            RecordGapDispositionResult.Invalid invalid => Invalid(invalid.Errors),
            RecordGapDispositionResult.Denied denied => Results.Json(Problem("project.denied", denied.Reason), statusCode: 403),
            RecordGapDispositionResult.ProjectNotFound => Results.NotFound(),
            RecordGapDispositionResult.ReferenceNotFound missing => Results.UnprocessableEntity(
                Problem("gap.reference.not_found", $"The {missing.Reference} is not available in this project.")),
            RecordGapDispositionResult.FindingNotFound missing => Results.UnprocessableEntity(
                Problem("gap.finding.not_found", $"Finding {missing.RuleCode} in scope {missing.ScopeId} is not open for profile {missing.ProfileId}.")),
            RecordGapDispositionResult.Conflict conflict => Results.Conflict(
                ConflictProblem(conflict.Expected, conflict.Actual, conflict.Conflicts)),
            RecordGapDispositionResult.IdempotencyConflict conflict => Results.Conflict(
                Problem("project.operation.conflict", $"Operation '{conflict.OperationId}' was already used.")),
            _ => throw new UnreachableException(),
        };
    }

    private static async Task<IResult> ImportProjectAsync(
        string workspaceId,
        [FromBody] ImportProjectRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? operationId,
        ImportProjectHandler handler,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(operationId))
            return Results.BadRequest(Problem("project.operation.required", "An Idempotency-Key header is required."));

        var result = await handler.HandleAsync(
            new ImportProjectCommand(workspaceId, operationId, request.Document, request.Reason),
            new ProjectActor(LocalDevelopmentProjectAccess.ActorSubject), cancellationToken);
        return result switch
        {
            ImportProjectResult.Imported imported => Results.Created(
                $"/api/v1/projects/{imported.Project.ProjectId}",
                new ImportProjectResponse(
                    imported.Project.ProjectId, imported.Project.Name, imported.Project.Revision,
                    imported.Project.ElementCount, imported.Project.RelationCount,
                    imported.Project.ContentHash, $"/api/v1/projects/{imported.Project.ProjectId}/export")),
            ImportProjectResult.Invalid invalid => Results.UnprocessableEntity(new PortableProjectProblemResponse(
                "project.import.invalid", "The project document cannot be imported.",
                [.. invalid.Findings.Select(finding => new PortableProjectFindingResponse(
                    finding.Code, finding.Path, finding.Message))])),
            ImportProjectResult.Denied denied => Results.Json(
                new PortableProjectProblemResponse("project.denied", denied.Reason, []), statusCode: StatusCodes.Status403Forbidden),
            ImportProjectResult.DuplicateProject => Results.Conflict(new PortableProjectProblemResponse(
                "project.import.duplicate", "The project identifier or normalized workspace name already exists.", [])),
            ImportProjectResult.OperationConflict => Results.Conflict(new PortableProjectProblemResponse(
                "project.operation.conflict", "The import operation identifier was already used.", [])),
            _ => throw new UnreachableException(),
        };
    }

    private static async Task<IResult> ExportProjectAsync(
        string projectId,
        IPortableProjectStore store,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(projectId, out var id))
            return Results.BadRequest(Problem("project.id.invalid", "The project identifier is invalid."));
        var result = await store.ExportAsync(id, cancellationToken);
        return result switch
        {
            PortableExportStoreResult.Exported exported => Results.Text(
                exported.CanonicalJson, "application/vnd.projectbuilder.project+json", System.Text.Encoding.UTF8,
                StatusCodes.Status200OK),
            PortableExportStoreResult.NotFound => Results.NotFound(),
            PortableExportStoreResult.SnapshotStale stale => Results.Conflict(new PortableProjectProblemResponse(
                "project.export.snapshot_stale",
                $"The portable snapshot is revision {stale.SnapshotRevision}; current model revision is {stale.CurrentRevision}.", [])),
            _ => throw new UnreachableException(),
        };
    }

    private static async Task<IResult> GetModelAsync(
        string projectId,
        GetProjectModelHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(projectId, cancellationToken);
        return result switch
        {
            GetProjectModelResult.Found found => Results.Ok(ToResponse(found.Model)),
            GetProjectModelResult.Invalid invalid => Results.BadRequest(Problem(invalid.Error.Code, invalid.Error.Message)),
            GetProjectModelResult.NotFound => Results.NotFound(),
            _ => throw new UnreachableException(),
        };
    }

    private static async Task<IResult> GetLensAsync(
        string projectId, [FromQuery] string? kind, [FromQuery] string? status, [FromQuery] string? q,
        GetProjectModelHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(projectId, cancellationToken);
        return result switch
        {
            GetProjectModelResult.Found found => Results.Ok(ProjectDefinitionLensProjector.Project(
                ToResponse(found.Model), new LensProjectionRequest(Split(kind), Split(status), q))),
            GetProjectModelResult.Invalid invalid => Results.BadRequest(Problem(invalid.Error.Code, invalid.Error.Message)),
            GetProjectModelResult.NotFound => Results.NotFound(),
            _ => throw new UnreachableException(),
        };
    }

    private static async Task<IResult> GetStoryMapAsync(
        string projectId, [FromQuery] string? kind, [FromQuery] string? status,
        [FromQuery] string? overlay, [FromQuery] string? q,
        GetProjectModelHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(projectId, cancellationToken);
        return result switch
        {
            GetProjectModelResult.Found found => Results.Ok(StoryMapLensProjector.Project(
                ToResponse(found.Model), new LensProjectionRequest(Split(kind), Split(status), q,
                    overlay is null ? ["priority", "status"] : Split(overlay)))),
            GetProjectModelResult.Invalid invalid => Results.BadRequest(Problem(invalid.Error.Code, invalid.Error.Message)),
            GetProjectModelResult.NotFound => Results.NotFound(),
            _ => throw new UnreachableException(),
        };
    }

    private static string[] Split(string? value) => string.IsNullOrWhiteSpace(value)
        ? []
        : value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static async Task<IResult> GetScenarioFlowAsync(
        string projectId, string scenarioId, GetProjectModelHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(projectId, cancellationToken);
        if (result is GetProjectModelResult.Invalid invalid)
            return Results.BadRequest(Problem(invalid.Error.Code, invalid.Error.Message));
        if (result is GetProjectModelResult.NotFound) return Results.NotFound();
        try
        {
            return Results.Ok(ScenarioFlowLensProjector.Project(
                ToResponse(((GetProjectModelResult.Found)result).Model), scenarioId));
        }
        catch (ArgumentException)
        {
            return Results.NotFound();
        }
    }

    private static async Task<IResult> GetStateRuleAsync(
        string projectId, string stateId, GetProjectModelHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(projectId, cancellationToken);
        if (result is GetProjectModelResult.Invalid invalid)
            return Results.BadRequest(Problem(invalid.Error.Code, invalid.Error.Message));
        if (result is GetProjectModelResult.NotFound) return Results.NotFound();
        try
        {
            return Results.Ok(StateRuleLensProjector.Project(
                ToResponse(((GetProjectModelResult.Found)result).Model), stateId));
        }
        catch (ArgumentException)
        {
            return Results.NotFound();
        }
    }

    private static async Task<IResult> GetSystemContextAsync(
        string projectId, string systemId, string? overlay,
        GetProjectModelHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(projectId, cancellationToken);
        if (result is GetProjectModelResult.Invalid invalid) return Results.BadRequest(Problem(invalid.Error.Code, invalid.Error.Message));
        if (result is GetProjectModelResult.NotFound) return Results.NotFound();
        try
        {
            return Results.Ok(SystemContextLensProjector.Project(
                ToResponse(((GetProjectModelResult.Found)result).Model), systemId, overlay ?? "ownership"));
        }
        catch (ArgumentException exception)
        {
            return Results.BadRequest(Problem("system-context.invalid", exception.Message));
        }
    }

    private static async Task<IResult> GetTraceabilityAsync(string projectId, string? view,
        GetProjectModelHandler handler, ITraceabilityStore traceability, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(projectId, cancellationToken);
        if (result is GetProjectModelResult.Invalid invalid) return Results.BadRequest(Problem(invalid.Error.Code, invalid.Error.Message));
        if (result is GetProjectModelResult.NotFound) return Results.NotFound();
        var found = (GetProjectModelResult.Found)result;
        var trace = await traceability.LoadTraceabilityAsync(ProjectBuilder.Domain.Modeling.Primitives.ProjectId.Parse(projectId) is ProjectBuilder.Domain.Modeling.Primitives.SemanticResult<ProjectBuilder.Domain.Modeling.Primitives.ProjectId>.Accepted accepted ? accepted.Value : throw new UnreachableException(), cancellationToken);
        return Results.Ok(TraceabilityLensProjector.Project(ToResponse(found.Model), ToResponse(trace), view ?? "outcomes"));
    }

    private static async Task<IResult> GetFindingsAsync(
        string projectId, [FromQuery] string? profile, GetProjectFindingsHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(projectId, profile ?? "discovery", cancellationToken);
        return result switch
        {
            GetProjectFindingsResult.Found found => Results.Ok(new ProjectFindingsResponse(
                found.Overview.ProjectId, found.Overview.ProjectName, found.Overview.Revision,
                new PurposeProfileResponse(found.Overview.Profile.Id, found.Overview.Profile.Name, found.Overview.Profile.Description),
                [.. found.Overview.AvailableProfiles.Select(item => new PurposeProfileResponse(item.Id, item.Name, item.Description))],
                [.. found.Overview.Coverage.Select(item => new CoverageDimensionResponse(
                    item.Id, item.Name, item.Status, item.Required, item.FindingCount, item.Explanation, item.RepairPath))],
                [.. found.Overview.Predicates.Select(item => new ProfilePredicateResponse(item.Code, item.Name, item.Satisfied, item.Explanation))],
                [.. found.Overview.Findings.Select(item => new ProjectFindingResponse(
                    item.Code, item.Severity, item.Status, item.Category, item.Title, item.Explanation,
                    item.Rule, item.ScopeId, item.ScopeKind, item.ScopeName, item.Owner,
                    item.RepairLabel, item.RepairPath, item.RepairAvailable,
                    item.DispositionId, item.DispositionRationale, item.DispositionConsequence,
                    item.AuthorityActorId, item.AuthorityName, item.ReviewOn, item.TargetMilestone))],
                [.. found.Overview.EvidenceRequirements.Select(item => new EvidenceRequirementResponse(
                    item.ClaimKind, item.ClaimName, item.Requirement, item.Status, item.Owner,
                    item.ScopeId, item.ScopePath))],
                [.. found.Overview.Authorities.Select(item => new GapAuthorityResponse(item.Id, item.Name, item.ContextualRole))])),
            GetProjectFindingsResult.Invalid invalid => Results.BadRequest(Problem(invalid.Code, invalid.Message)),
            GetProjectFindingsResult.NotFound => Results.NotFound(),
            _ => throw new UnreachableException(),
        };
    }

    private static async Task<IResult> GetRecommendationsAsync(
        string projectId, [FromQuery] string? profile, GetProjectRecommendationsHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(projectId, profile ?? "discovery", cancellationToken);
        return result switch
        {
            GetProjectRecommendationsResult.Found found => Results.Ok(new ProjectRecommendationsResponse(
                found.Overview.ProjectId, found.Overview.ProjectName, found.Overview.Revision, found.Overview.RuleVersion,
                new PurposeProfileResponse(found.Overview.Profile.Id, found.Overview.Profile.Name, found.Overview.Profile.Description),
                found.Overview.RecentChangeKind, found.Overview.RecentChangeRevision, found.Overview.PrimaryRecommendationId,
                [.. found.Overview.Candidates.Select(candidate => new RecommendationCandidateResponse(
                    candidate.Id, candidate.Rank, candidate.Stage, candidate.Title, candidate.ActionLabel, candidate.Path,
                    candidate.Status, candidate.Priority, candidate.Rationale, candidate.FindingCodes, candidate.Dependencies,
                    [.. candidate.Signals.Select(signal => new RecommendationSignalResponse(
                        signal.Kind, signal.Label, signal.Value, signal.Explanation))]))])),
            GetProjectRecommendationsResult.Invalid invalid => Results.BadRequest(Problem(invalid.Code, invalid.Message)),
            GetProjectRecommendationsResult.NotFound => Results.NotFound(),
            _ => throw new InvalidOperationException("Unknown recommendation result."),
        };
    }

    private static async Task<IResult> GetWorkshopAsync(
        string projectId, [FromQuery] string? profile, GetProjectWorkshopHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(projectId, profile ?? "discovery", cancellationToken);
        return result switch
        {
            GetProjectWorkshopResult.Found found => Results.Ok(new ProjectWorkshopResponse(
                found.Workshop.ProjectId, found.Workshop.ProjectName, found.Workshop.Purpose,
                found.Workshop.IntendedOutcome, found.Workshop.Revision, found.Workshop.ProfileId,
                found.Workshop.ProfileName, found.Workshop.BriefVersion, found.Workshop.PrimaryRecommendation,
                [.. found.Workshop.Participants.Select(item => new WorkshopParticipantResponse(
                    item.Id, item.Name, item.Role, item.Contribution))],
                [.. found.Workshop.Agenda.Select(item => new WorkshopAgendaItemResponse(
                    item.Id, item.Order, item.Phase, item.Title, item.IntendedResult, item.Minutes,
                    item.Status, item.SourceLabel, item.SourcePath))],
                [.. found.Workshop.FocusItems.Select(item => new WorkshopFocusResponse(
                    item.Kind, item.Code, item.Title, item.Severity, item.Path))])),
            GetProjectWorkshopResult.Invalid invalid => Results.BadRequest(Problem(invalid.Code, invalid.Message)),
            GetProjectWorkshopResult.NotFound => Results.NotFound(),
            _ => throw new InvalidOperationException("Unknown workshop result."),
        };
    }

    private static async Task<IResult> DefinePathAsync(
        string projectId, [FromBody] DefinePathRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? operationId,
        [FromHeader(Name = "If-Match")] string? revision,
        DefinePathHandler handler, CancellationToken cancellationToken)
    {
        var headerProblem = RequiredEditHeaders(operationId, revision);
        if (headerProblem is not null) return headerProblem;
        var result = await handler.HandleAsync(new DefinePathCommand(
            projectId, RevisionText(revision!), operationId!, request.ScenarioId,
            request.SourceTransitionId, request.TerminalResultId, request.RecoveryResultId,
            request.OwnerId, request.BranchName, request.BranchClassification,
            request.BranchConditionName, request.BranchConditionKind, request.BranchCondition,
            request.BranchFactIds, request.BranchRuleIds, request.BranchSegments,
            request.BranchTerminalState, request.BranchObservation,
            request.EffectName, request.EffectKind, request.EffectStatement,
            request.RecoveryName, request.RecoveryStrategy, request.RecoveryConditionName,
            request.RecoveryCondition, request.RecoverySegments, request.RecoveryTerminalState,
            request.RecoveryObservation, request.RetryPolicy, request.IdempotencyAnalysis,
            request.ExitCondition, request.Reconciliation, request.Reason),
            new ProjectActor(LocalDevelopmentProjectAccess.ActorSubject), cancellationToken);
        return result switch
        {
            DefinePathResult.Defined defined => Results.Created(
                $"/api/v1/projects/{projectId}/paths/{defined.Path.BranchPathId}",
                new DefinePathResponse(ToResponse(defined.Path), defined.Revision, defined.AllowedNextAction)),
            DefinePathResult.Invalid invalid => Invalid(invalid.Errors),
            DefinePathResult.Denied denied => Results.Json(Problem("project.denied", denied.Reason), statusCode: 403),
            DefinePathResult.ProjectNotFound => Results.NotFound(),
            DefinePathResult.ReferenceNotFound missing => Results.UnprocessableEntity(
                Problem("path.reference.not_found", $"The {missing.Reference} is not available in this project.")),
            DefinePathResult.Conflict conflict => Results.Conflict(
                ConflictProblem(conflict.Expected, conflict.Actual, conflict.Conflicts)),
            DefinePathResult.IdempotencyConflict conflict => Results.Conflict(
                Problem("project.operation.conflict", $"Operation '{conflict.OperationId}' was already used for different content.")),
            _ => throw new UnreachableException(),
        };
    }

    private static async Task<IResult> DefineStateLogicAsync(
        string projectId, [FromBody] DefineStateLogicRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? operationId,
        [FromHeader(Name = "If-Match")] string? revision,
        DefineStateLogicHandler handler, CancellationToken cancellationToken)
    {
        var headerProblem = RequiredEditHeaders(operationId, revision);
        if (headerProblem is not null) return headerProblem;
        var result = await handler.HandleAsync(new DefineStateLogicCommand(
            projectId, RevisionText(revision!), operationId!, request.StateName, request.StateCategory,
            request.StateStructure, request.StateValues, request.OwnerId, request.FactName,
            request.FactValueType, request.FactAuthority, request.FactMutability, request.RuleName,
            request.RuleKind, request.RuleStatement, request.RuleAuthorityOwnerId, request.InvariantName,
            request.InvariantStatement, request.FalsifyingExample, request.ProofExpectation,
            request.SemanticResults, request.TransitionName, request.SourcePredicate, request.Trigger,
            request.TargetPredicate, request.Reason),
            new ProjectActor(LocalDevelopmentProjectAccess.ActorSubject), cancellationToken);
        return result switch
        {
            DefineStateLogicResult.Defined defined => Results.Created(
                $"/api/v1/projects/{projectId}/state-logic/{defined.Definitions.StateId}",
                new DefineStateLogicResponse(ToResponse(defined.Definitions), defined.Revision, defined.AllowedNextAction)),
            DefineStateLogicResult.Invalid invalid => Invalid(invalid.Errors),
            DefineStateLogicResult.Denied denied => Results.Json(Problem("project.denied", denied.Reason), statusCode: 403),
            DefineStateLogicResult.ProjectNotFound => Results.NotFound(),
            DefineStateLogicResult.ReferenceNotFound missing => Results.UnprocessableEntity(Problem("state.reference.not_found", $"The {missing.Reference} is not available in this project.")),
            DefineStateLogicResult.Conflict conflict => Results.Conflict(ConflictProblem(conflict.Expected, conflict.Actual, conflict.Conflicts)),
            DefineStateLogicResult.IdempotencyConflict conflict => Results.Conflict(Problem("project.operation.conflict", $"Operation '{conflict.OperationId}' was already used for different content.")),
            _ => throw new UnreachableException(),
        };
    }

    private static async Task<IResult> DefineSystemContextAsync(
        string projectId, [FromBody] DefineSystemContextRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? operationId,
        [FromHeader(Name = "If-Match")] string? revision,
        DefineSystemContextHandler handler, CancellationToken cancellationToken)
    {
        var headerProblem = RequiredEditHeaders(operationId, revision);
        if (headerProblem is not null) return headerProblem;
        var result = await handler.HandleAsync(new DefineSystemContextCommand(
            projectId, RevisionText(revision!), operationId!, request.OwnedSystemName, request.OwnedSystemPurpose,
            request.OwnedSystemOwnerId, request.OwnedResponsibilities, request.ExternalSystemName,
            request.ExternalSystemPurpose, request.ExternalSystemOwnerId, request.ExternalResponsibilities,
            request.ExternalKnowledgeStatus, request.InterfaceName, request.InterfaceDescription,
            request.InterfaceKind, request.ParticipantIds, request.AcceptedIntents, request.Observations,
            request.AccessibilityConstraints, request.BoundaryName, request.BoundaryDescription,
            request.BoundaryKinds, request.BoundaryOwnerIds, request.BoundaryKnowledgeStatus,
            request.CrossingEffectId, request.ContractName, request.ContractDescription, request.ContractKind,
            request.ContractVersion, request.ContractOwnerId, request.SchemaReference, request.CompatibilityPolicy,
            request.RequestData, request.ResponseData, request.DataClassification, request.ContractKnowledgeStatus,
            request.Reason), new ProjectActor(LocalDevelopmentProjectAccess.ActorSubject), cancellationToken);
        return result switch
        {
            DefineSystemContextResult.Defined defined => Results.Created(
                $"/api/v1/projects/{projectId}/system-contexts/{defined.Context.OwnedSystemId}",
                new DefineSystemContextResponse(ToResponse(defined.Context), defined.Revision, defined.AllowedNextAction)),
            DefineSystemContextResult.Invalid invalid => Invalid(invalid.Errors),
            DefineSystemContextResult.Denied denied => Results.Json(Problem("project.denied", denied.Reason), statusCode: 403),
            DefineSystemContextResult.ProjectNotFound => Results.NotFound(),
            DefineSystemContextResult.ReferenceNotFound missing => Results.UnprocessableEntity(Problem("system-context.reference.not_found", $"The {missing.Reference} is not available in this project.")),
            DefineSystemContextResult.Conflict conflict => Results.Conflict(ConflictProblem(conflict.Expected, conflict.Actual, conflict.Conflicts)),
            DefineSystemContextResult.IdempotencyConflict conflict => Results.Conflict(Problem("project.operation.conflict", $"Operation '{conflict.OperationId}' was already used for different content.")),
            _ => throw new UnreachableException(),
        };
    }

    private static async Task<IResult> DefineEvidencePacketAsync(string projectId,
        [FromBody] DefineEvidencePacketRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? operationId,
        [FromHeader(Name = "If-Match")] string? revision,
        DefineEvidencePacketHandler handler, CancellationToken cancellationToken)
    {
        var headerProblem = RequiredEditHeaders(operationId, revision); if (headerProblem is not null) return headerProblem;
        var result = await handler.HandleAsync(new DefineEvidencePacketCommand(projectId, RevisionText(revision!), operationId!,
            request.ClaimKind, request.ClaimStatement, request.ClaimStatus, request.ElementIds, request.OwnerId,
            request.Tags, request.EvidenceKind, request.EvidenceStatus, request.Producer, request.Environment,
            request.Summary, request.Limitations, request.Reason),
            new ProjectActor(LocalDevelopmentProjectAccess.ActorSubject), cancellationToken);
        return result switch
        {
            DefineEvidencePacketResult.Defined defined => Results.Created($"/api/v1/projects/{projectId}/evidence-packets/{defined.Claim.Id}",
                new DefineEvidencePacketResponse(ToResponse(defined.Claim), ToResponse(defined.Evidence), defined.Revision, defined.AllowedNextAction)),
            DefineEvidencePacketResult.Invalid invalid => Invalid(invalid.Errors),
            DefineEvidencePacketResult.Denied denied => Results.Json(Problem("project.denied", denied.Reason), statusCode: 403),
            DefineEvidencePacketResult.ProjectNotFound => Results.NotFound(),
            DefineEvidencePacketResult.ReferenceNotFound missing => Results.UnprocessableEntity(Problem("traceability.reference.not_found", $"The {missing.Reference} is not available in this project.")),
            DefineEvidencePacketResult.Conflict conflict => Results.Conflict(ConflictProblem(conflict.Expected, conflict.Actual, conflict.Conflicts)),
            DefineEvidencePacketResult.IdempotencyConflict conflict => Results.Conflict(Problem("project.operation.conflict", $"Operation '{conflict.OperationId}' was already used for different content.")),
            _ => throw new UnreachableException(),
        };
    }

    private static async Task<IResult> DefineNarrativeAsync(
        string projectId,
        [FromBody] DefineNarrativeRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? operationId,
        [FromHeader(Name = "If-Match")] string? revision,
        DefineNarrativeHandler handler,
        CancellationToken cancellationToken)
    {
        var headerProblem = RequiredEditHeaders(operationId, revision);
        if (headerProblem is not null) return headerProblem;
        var result = await handler.HandleAsync(new DefineNarrativeCommand(
            projectId, RevisionText(revision!), operationId!, request.OutcomeId,
            string.Join(',', request.ParticipantIds), request.InitiatorId, request.ReceiverId,
            request.EpisodeName, request.EpisodeStart, request.EpisodeEnd,
            request.ScenarioName, request.Classification, request.StartingFacts, request.Trigger,
            request.ExpectedOutcome, request.SceneName, request.Setting, request.Responsibility,
            request.InteractionName, request.Intent, request.Step, request.Observation,
            request.SemanticResults, request.Reason),
            new ProjectActor(LocalDevelopmentProjectAccess.ActorSubject), cancellationToken);
        return result switch
        {
            DefineNarrativeResult.Defined defined => Results.Created(
                $"/api/v1/projects/{projectId}/narratives/{defined.Narrative.EpisodeId}",
                new DefineNarrativeResponse(ToResponse(defined.Narrative), defined.Revision, defined.AllowedNextAction)),
            DefineNarrativeResult.Invalid invalid => Invalid(invalid.Errors),
            DefineNarrativeResult.Denied denied => Results.Json(Problem("project.denied", denied.Reason), statusCode: 403),
            DefineNarrativeResult.ProjectNotFound => Results.NotFound(),
            DefineNarrativeResult.ReferenceNotFound missing => Results.UnprocessableEntity(Problem("narrative.reference.not_found", $"The {missing.Reference} is not available in this project.")),
            DefineNarrativeResult.Conflict conflict => Results.Conflict(ConflictProblem(conflict.Expected, conflict.Actual, conflict.Conflicts)),
            DefineNarrativeResult.IdempotencyConflict conflict => Results.Conflict(Problem("project.operation.conflict", $"Operation '{conflict.OperationId}' was already used for different content.")),
            _ => throw new UnreachableException(),
        };
    }

    private static async Task<IResult> AddActorAsync(
        string projectId,
        [FromBody] AddActorRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? operationId,
        [FromHeader(Name = "If-Match")] string? revision,
        AddActorHandler handler,
        CancellationToken cancellationToken)
    {
        var headerProblem = RequiredEditHeaders(operationId, revision);
        if (headerProblem is not null) return headerProblem;

        var result = await handler.HandleAsync(new AddActorCommand(
            projectId, RevisionText(revision!), operationId!, request.Name, request.ActorKind,
            request.ContextualRole, request.Goals, request.Responsibilities, request.Authority,
            request.Constraints, request.Reason, request.KnowledgeStatus),
            new ProjectActor(LocalDevelopmentProjectAccess.ActorSubject), cancellationToken);
        return result switch
        {
            AddActorResult.Added added => Results.Created(
                $"/api/v1/projects/{projectId}/actors/{added.Actor.Id}",
                new AddActorResponse(ToResponse(added.Actor), added.Revision, added.AllowedNextAction)),
            AddActorResult.Invalid invalid => Invalid(invalid.Errors),
            AddActorResult.Denied denied => Results.Json(Problem("project.denied", denied.Reason), statusCode: 403),
            AddActorResult.ProjectNotFound => Results.NotFound(),
            AddActorResult.Conflict conflict => Results.Conflict(ConflictProblem(conflict.Expected, conflict.Actual, conflict.Conflicts)),
            AddActorResult.IdempotencyConflict conflict => Results.Conflict(Problem("project.operation.conflict", $"Operation '{conflict.OperationId}' was already used for different content.")),
            _ => throw new UnreachableException(),
        };
    }

    private static async Task<IResult> AddOutcomeAsync(
        string projectId,
        [FromBody] AddOutcomeRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? operationId,
        [FromHeader(Name = "If-Match")] string? revision,
        AddOutcomeHandler handler,
        CancellationToken cancellationToken)
    {
        var headerProblem = RequiredEditHeaders(operationId, revision);
        if (headerProblem is not null) return headerProblem;

        var result = await handler.HandleAsync(new AddOutcomeCommand(
            projectId, RevisionText(revision!), operationId!, request.Name, request.Statement,
            request.SuccessSignals, request.BeneficiaryActorId, request.Reason, request.KnowledgeStatus),
            new ProjectActor(LocalDevelopmentProjectAccess.ActorSubject), cancellationToken);
        return result switch
        {
            AddOutcomeResult.Added added => Results.Created(
                $"/api/v1/projects/{projectId}/outcomes/{added.Outcome.Id}",
                new AddOutcomeResponse(ToResponse(added.Outcome), added.Revision, added.AllowedNextAction)),
            AddOutcomeResult.Invalid invalid => Invalid(invalid.Errors),
            AddOutcomeResult.Denied denied => Results.Json(Problem("project.denied", denied.Reason), statusCode: 403),
            AddOutcomeResult.ProjectNotFound => Results.NotFound(),
            AddOutcomeResult.BeneficiaryNotFound missing => Results.UnprocessableEntity(Problem("outcome.beneficiary.not_found", $"Actor '{missing.ActorId}' is not available in this project.")),
            AddOutcomeResult.Conflict conflict => Results.Conflict(ConflictProblem(conflict.Expected, conflict.Actual, conflict.Conflicts)),
            AddOutcomeResult.IdempotencyConflict conflict => Results.Conflict(Problem("project.operation.conflict", $"Operation '{conflict.OperationId}' was already used for different content.")),
            _ => throw new UnreachableException(),
        };
    }

    private static async Task<IResult> AddCapabilityAsync(
        string projectId, [FromBody] AddCapabilityRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? operationId,
        [FromHeader(Name = "If-Match")] string? revision,
        AddCapabilityHandler handler, CancellationToken cancellationToken)
    {
        var headerProblem = RequiredEditHeaders(operationId, revision);
        if (headerProblem is not null) return headerProblem;
        var result = await handler.HandleAsync(new(projectId, RevisionText(revision!), operationId!, request.Name,
            request.Ability, request.OutcomeIds, request.Priority, request.Reason, request.KnowledgeStatus),
            new ProjectActor(LocalDevelopmentProjectAccess.ActorSubject), cancellationToken);
        return result switch
        {
            AddCapabilityResult.Added added => Results.Created($"/api/v1/projects/{projectId}/capabilities/{added.Capability.Id}",
                new AddCapabilityResponse(ToResponse(added.Capability), added.Revision, added.AllowedNextAction)),
            AddCapabilityResult.Invalid invalid => Invalid(invalid.Errors),
            AddCapabilityResult.Denied denied => Results.Json(Problem("project.denied", denied.Reason), statusCode: 403),
            AddCapabilityResult.ProjectNotFound => Results.NotFound(),
            AddCapabilityResult.OutcomeNotFound missing => Results.UnprocessableEntity(
                Problem("capability.outcome.not_found", $"Outcome '{missing.OutcomeId}' is not available in this project.")),
            AddCapabilityResult.Conflict conflict => Results.Conflict(ConflictProblem(conflict.Expected, conflict.Actual, conflict.Conflicts)),
            AddCapabilityResult.IdempotencyConflict conflict => Results.Conflict(
                Problem("project.operation.conflict", $"Operation '{conflict.OperationId}' was already used for different content.")),
            _ => throw new UnreachableException(),
        };
    }

    private static async Task<IResult> UpdateActorAsync(
        string projectId, string actorId, [FromBody] UpdateActorRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? operationId,
        [FromHeader(Name = "If-Match")] string? revision,
        UpdateActorHandler handler, CancellationToken cancellationToken)
    {
        var headerProblem = RequiredEditHeaders(operationId, revision); if (headerProblem is not null) return headerProblem;
        var result = await handler.HandleAsync(new(projectId, actorId, RevisionText(revision!), operationId!, request.Name,
            request.ActorKind, request.ContextualRole, request.Goals, request.Responsibilities, request.Authority,
            request.Constraints, request.Reason, request.KnowledgeStatus), new ProjectActor(LocalDevelopmentProjectAccess.ActorSubject), cancellationToken);
        return result switch
        {
            UpdateActorResult.Updated updated => Results.Ok(new UpdateActorResponse(ToResponse(updated.Actor), updated.Revision, updated.AllowedNextAction)),
            UpdateActorResult.Invalid invalid => Invalid(invalid.Errors),
            UpdateActorResult.Denied denied => Results.Json(Problem("project.denied", denied.Reason), statusCode: 403),
            UpdateActorResult.ProjectNotFound or UpdateActorResult.ActorNotFound => Results.NotFound(),
            UpdateActorResult.Conflict conflict => Results.Conflict(ConflictProblem(conflict.Expected, conflict.Actual, conflict.Conflicts)),
            UpdateActorResult.IdempotencyConflict conflict => Results.Conflict(Problem("project.operation.conflict", $"Operation '{conflict.OperationId}' was already used for different content.")),
            _ => throw new UnreachableException(),
        };
    }

    private static async Task<IResult> UpdateOutcomeAsync(
        string projectId, string outcomeId, [FromBody] UpdateOutcomeRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? operationId,
        [FromHeader(Name = "If-Match")] string? revision,
        UpdateOutcomeHandler handler, CancellationToken cancellationToken)
    {
        var headerProblem = RequiredEditHeaders(operationId, revision); if (headerProblem is not null) return headerProblem;
        var result = await handler.HandleAsync(new(projectId, outcomeId, RevisionText(revision!), operationId!, request.Name,
            request.Statement, request.SuccessSignals, request.BeneficiaryActorId, request.Reason, request.KnowledgeStatus),
            new ProjectActor(LocalDevelopmentProjectAccess.ActorSubject), cancellationToken);
        return result switch
        {
            UpdateOutcomeResult.Updated updated => Results.Ok(new UpdateOutcomeResponse(ToResponse(updated.Outcome), updated.Revision, updated.AllowedNextAction)),
            UpdateOutcomeResult.Invalid invalid => Invalid(invalid.Errors),
            UpdateOutcomeResult.Denied denied => Results.Json(Problem("project.denied", denied.Reason), statusCode: 403),
            UpdateOutcomeResult.ProjectNotFound or UpdateOutcomeResult.OutcomeNotFound => Results.NotFound(),
            UpdateOutcomeResult.BeneficiaryNotFound missing => Results.UnprocessableEntity(Problem("outcome.beneficiary.not_found", $"Actor '{missing.ActorId}' is not available in this project.")),
            UpdateOutcomeResult.Conflict conflict => Results.Conflict(ConflictProblem(conflict.Expected, conflict.Actual, conflict.Conflicts)),
            UpdateOutcomeResult.IdempotencyConflict conflict => Results.Conflict(Problem("project.operation.conflict", $"Operation '{conflict.OperationId}' was already used for different content.")),
            _ => throw new UnreachableException(),
        };
    }

    private static IResult? RequiredEditHeaders(string? operationId, string? revision)
    {
        if (string.IsNullOrWhiteSpace(operationId))
            return Results.BadRequest(Problem("project.operation.required", "An Idempotency-Key header is required."));
        if (string.IsNullOrWhiteSpace(revision))
            return Results.BadRequest(Problem("project.revision.required", "An If-Match revision header is required."));
        return null;
    }

    private static string RevisionText(string revision) => revision.Trim().Trim('"');

    private static IResult Invalid(IReadOnlyList<ProjectBuilder.Domain.Modeling.Primitives.SemanticError> errors) =>
        Results.UnprocessableEntity(new ProjectProblemResponse(
            "model.invalid", "The model edit is invalid.",
            errors.GroupBy(x => x.Code).ToDictionary(x => x.Key, x => x.Select(y => y.Message).ToArray())));

    private static ProjectModelResponse ToResponse(ProjectModelOverview model) => new(
        ToResponse(model.Project, "Add or review actors and outcomes."),
        [.. model.Actors.Select(ToResponse)],
        [.. model.Outcomes.Select(ToResponse)],
        [.. model.Narratives.Select(ToResponse)],
        [.. model.StateLogic.Select(ToResponse)],
        [.. model.Paths.Select(ToResponse)],
        [.. model.Relations.Select(ToResponse)],
        [.. model.ChangeSets.Select(ToResponse)],
        [.. (model.Capabilities ?? []).Select(ToResponse)],
        [.. (model.SystemContexts ?? []).Select(ToResponse)]);

    private static ActorResponse ToResponse(ActorOverview actor) => new(
        actor.Id, actor.Name, actor.ActorKind, actor.ContextualRole, actor.Goals,
        actor.Responsibilities, actor.Authority, actor.Constraints, actor.KnowledgeStatus);

    private static OutcomeResponse ToResponse(OutcomeOverview outcome) => new(
        outcome.Id, outcome.Name, outcome.Statement, outcome.SuccessSignals,
        outcome.BeneficiaryActorId, outcome.BeneficiaryName, outcome.KnowledgeStatus);

    private static CapabilityResponse ToResponse(CapabilityOverview capability) => new(
        capability.Id, capability.Name, capability.Ability, capability.OutcomeIds,
        capability.Priority, capability.KnowledgeStatus);

    private static SystemContextResponse ToResponse(SystemContextOverview context) => new(
        context.OwnedSystemId, context.OwnedSystemName, context.OwnedSystemPurpose, context.OwnedSystemOwnerId,
        context.OwnedSystemOwnerName, context.OwnedResponsibilities, context.ExternalSystemId,
        context.ExternalSystemName, context.ExternalSystemPurpose, context.ExternalSystemOwnerId,
        context.ExternalSystemOwnerName, context.ExternalResponsibilities, context.ExternalKnowledgeStatus,
        context.InterfaceId, context.InterfaceName, context.InterfaceDescription, context.InterfaceKind,
        context.ParticipantIds, context.ParticipantNames, context.AcceptedIntents, context.Observations,
        context.AccessibilityConstraints, context.BoundaryId, context.BoundaryName, context.BoundaryDescription,
        context.BoundaryKinds, context.BoundaryOwnerIds, context.BoundaryOwnerNames,
        context.BoundaryKnowledgeStatus, context.CrossingEffectId, context.CrossingEffectName,
        context.ContractId, context.ContractName, context.ContractDescription, context.ContractKind,
        context.ContractVersion, context.ContractOwnerId, context.ContractOwnerName, context.SchemaReference,
        context.CompatibilityPolicy, context.RequestData, context.ResponseData, context.DataClassification,
        context.ContractKnowledgeStatus);

    private static TraceabilityResponse ToResponse(TraceabilitySnapshot snapshot) => new(
        [.. snapshot.Claims.Select(ToResponse)], [.. snapshot.Evidence.Select(ToResponse)]);
    private static ClaimResponse ToResponse(ClaimOverview claim) => new(claim.Id, claim.Kind, claim.Statement,
        claim.Status, claim.ElementIds, claim.OwnerId, claim.OwnerName, claim.Tags, claim.CreatedAt, claim.CreatedBy);
    private static EvidenceResponse ToResponse(EvidenceOverview evidence) => new(evidence.Id, evidence.Kind,
        evidence.Status, evidence.ClaimId, evidence.Producer, evidence.ProducedAt, evidence.ModelRevision,
        evidence.Environment, evidence.Summary, evidence.Limitations, evidence.CreatedBy);

    private static RelationResponse ToResponse(RelationOverview relation) => new(
        relation.Id, relation.Kind, relation.DisplayName,
        relation.SourceElementId, relation.SourceKind, relation.SourceName,
        relation.TargetElementId, relation.TargetKind, relation.TargetName,
        relation.Direction, relation.Cardinality, relation.IsUnique,
        relation.Ownership, relation.DeletionBehavior);

    private static ChangeSetResponse ToResponse(ChangeSetOverview changeSet) => new(
        changeSet.Id, changeSet.BaseRevision, changeSet.ResultRevision, changeSet.ChangeKind,
        changeSet.Reason, changeSet.CreatedBy, changeSet.OccurredAt, changeSet.OperationCount,
        changeSet.SemanticSummary,
        [.. changeSet.Operations.Select(operation => new ChangeOperationResponse(
            operation.Sequence, operation.Kind, operation.SubjectKind,
            operation.ElementId, operation.RelationId, operation.Summary))]);

    private static NarrativeResponse ToResponse(NarrativeOverview narrative) => new(
        narrative.EpisodeId, narrative.EpisodeName, narrative.Start, narrative.End, narrative.OutcomeName,
        narrative.ScenarioId, narrative.ScenarioName, narrative.Classification, narrative.StartingFacts, narrative.Trigger,
        narrative.ExpectedOutcome, narrative.SceneName, narrative.Setting, narrative.Responsibility,
        narrative.InteractionName, narrative.InitiatorName, narrative.ReceiverName, narrative.Intent,
        narrative.Step, narrative.Observation, narrative.SemanticResults,
        narrative.OutcomeId, narrative.SceneId, narrative.InitiatorId, narrative.ReceiverId,
        narrative.InteractionId, narrative.IntentId, narrative.StepId, narrative.ObservationId);

    private static StateLogicResponse ToResponse(StateLogicOverview definitions) => new(
        definitions.StateId, definitions.StateName, definitions.StateCategory, definitions.Structure,
        definitions.Values, definitions.OwnerName, definitions.FactId, definitions.FactName, definitions.FactValueType,
        definitions.FactAuthority, definitions.FactMutability, definitions.RuleId, definitions.RuleName, definitions.RuleKind,
        definitions.RuleStatement, definitions.InvariantName, definitions.InvariantStatement,
        definitions.FalsifyingExample, definitions.ProofExpectation,
        [.. definitions.Results.Select(x => new SemanticResultResponse(x.Id, x.Name, x.Kind, x.Meaning))],
        definitions.TransitionId, definitions.TransitionName, definitions.SourcePredicate, definitions.Trigger, definitions.TargetPredicate,
        definitions.OwnerId, definitions.FactAllowedKnowledge, definitions.RuleAuthorityOwnerId,
        definitions.InvariantId, definitions.InvariantScopeIds, definitions.ChangedFactIds,
        definitions.RuleIds, definitions.InvariantIds, definitions.ResultIds, definitions.RuleAuthorityOwnerName);

    private static PathResponse ToResponse(PathOverview path) => new(
        path.BranchPathId, path.BranchName, path.BranchClassification, path.ScenarioName,
        path.SourceTransitionName, path.BranchConditionName, path.BranchConditionKind,
        path.BranchCondition, path.BranchSegments, path.TerminalResultName, path.TerminalResultKind,
        path.BranchTerminalState, path.BranchObservation, path.OwnerName,
        path.EffectName, path.EffectKind, path.EffectStatement,
        path.RecoveryPathId, path.RecoveryName, path.RecoveryStrategy, path.RecoveryCondition,
        path.RecoverySegments, path.RecoveryResultName, path.RecoveryTerminalState,
        path.RecoveryObservation, path.RetryPolicy, path.IdempotencyAnalysis,
        path.ExitCondition, path.Reconciliation, path.ScenarioId, path.SourceTransitionId,
        path.BranchConditionId, path.TerminalResultId, path.OwnerId, path.EffectId,
        path.RecoveryConditionId, path.RecoveryResultId);

    private static async Task<IResult> CreateProjectAsync(
        string workspaceId,
        [FromBody] CreateProjectRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? operationId,
        CreateProjectHandler handler,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(operationId))
        {
            return Results.BadRequest(Problem(
                "project.operation.required",
                "An Idempotency-Key header is required.",
                ("operationId", "Supply one canonical GUID operation identifier.")));
        }

        var result = await handler.HandleAsync(
            new CreateProjectCommand(
                workspaceId,
                operationId,
                request.Name,
                request.Purpose,
                request.IntendedOutcome,
                request.Reason),
            new ProjectActor(LocalDevelopmentProjectAccess.ActorSubject),
            cancellationToken);

        return result switch
        {
            CreateProjectResult.Created created => Results.Created(
                $"/api/v1/projects/{created.Project.Id}",
                ToResponse(created.Project, created.AllowedNextAction)),
            CreateProjectResult.Invalid invalid => Results.UnprocessableEntity(new ProjectProblemResponse(
                "project.invalid",
                "The project definition is invalid.",
                invalid.Errors
                    .GroupBy(error => error.Code, StringComparer.Ordinal)
                    .ToDictionary(
                        group => group.Key,
                        group => group.Select(error => error.Message).ToArray(),
                        StringComparer.Ordinal))),
            CreateProjectResult.Denied denied => Results.Json(
                Problem("project.denied", denied.Reason),
                statusCode: StatusCodes.Status403Forbidden),
            CreateProjectResult.DuplicateName duplicate => Results.Conflict(
                Problem("project.name.duplicate", $"A project named '{duplicate.Name}' already exists in this workspace.")),
            CreateProjectResult.IdempotencyConflict conflict => Results.Conflict(
                Problem("project.operation.conflict", $"Operation '{conflict.OperationId}' was already used for different content.")),
            _ => throw new UnreachableException(),
        };
    }

    private static async Task<IResult> GetProjectAsync(
        string projectId,
        GetProjectHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(projectId, cancellationToken);
        return result switch
        {
            GetProjectResult.Found found => Results.Ok(ToResponse(
                found.Project,
                "Add the actors who participate in this outcome.")),
            GetProjectResult.Invalid invalid => Results.BadRequest(
                Problem(invalid.Error.Code, invalid.Error.Message)),
            GetProjectResult.NotFound => Results.NotFound(),
            _ => throw new UnreachableException(),
        };
    }

    internal static ProjectResponse ToResponse(ProjectOverview project, string nextAction) =>
        new(
            project.Id,
            project.WorkspaceId,
            project.Name,
            project.Purpose,
            project.IntendedOutcome,
            project.Revision,
            project.CreationReason,
            project.CreatedAt,
            nextAction);

    private static ProjectProblemResponse Problem(
        string code,
        string title,
        params (string Field, string Message)[] errors) =>
        new(
            code,
            title,
            errors.GroupBy(error => error.Field, StringComparer.Ordinal)
                .ToDictionary(
                    group => group.Key,
                    group => group.Select(error => error.Message).ToArray(),
                StringComparer.Ordinal));

    private static ProjectProblemResponse ConflictProblem(
        long expected,
        long actual,
        IReadOnlyList<ChangeSetConflictOverview> conflicts) =>
        new(
            "project.revision.conflict",
            $"Expected revision {expected}; actual revision is {actual}.",
            conflicts.GroupBy(conflict => conflict.Code, StringComparer.Ordinal)
                .ToDictionary(
                    group => group.Key,
                    group => group.Select(conflict => conflict.Message).ToArray(),
                    StringComparer.Ordinal));
}
