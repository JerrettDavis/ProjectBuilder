using System.Reflection;
using ProjectBuilder.Application.Collaboration.GetProjectWorkshop;
using ProjectBuilder.Application.Foundation;
using ProjectBuilder.Application.Guidance;
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
using ProjectBuilder.Application.Portability;
using ProjectBuilder.Application.Projects.CreateProject;
using ProjectBuilder.Application.Projects.GetProject;
using ProjectBuilder.Application.Traceability.DefineEvidencePacket;
using ProjectBuilder.Application.Validation.GetProjectFindings;
using ProjectBuilder.Application.Validation.GetProjectRecommendations;
using ProjectBuilder.Application.Validation.RecordGapDisposition;
using ProjectBuilder.Application.Views;
using ProjectBuilder.Contracts;
using ProjectBuilder.Domain.Modeling.Primitives;
using ProjectBuilder.Infrastructure.Persistence;
using ProjectBuilder.Web.Components;
using ProjectBuilder.Web.Projects;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddFoundationPersistence(builder.Configuration);
builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();
builder.Services.AddSingleton(CreateFoundationResponse());
var projectFeaturesAvailable = !string.IsNullOrWhiteSpace(
    builder.Configuration.GetConnectionString("projectbuilder"));
if (projectFeaturesAvailable)
{
    builder.Services.AddScoped<CreateProjectHandler>();
    builder.Services.AddScoped<ImportProjectHandler>();
    builder.Services.AddScoped<GetProjectHandler>();
    builder.Services.AddScoped<GetProjectModelHandler>();
    builder.Services.AddScoped<GetProjectFindingsHandler>();
    builder.Services.AddScoped<GetProjectRecommendationsHandler>();
    builder.Services.AddScoped<GetProjectWorkshopHandler>();
    builder.Services.AddScoped<RecordGapDispositionHandler>();
    builder.Services.AddSingleton<PromptRegistry>();
    builder.Services.AddScoped<GetProjectGuidanceHandler>();
    builder.Services.AddScoped<AddActorHandler>();
    builder.Services.AddScoped<AddOutcomeHandler>();
    builder.Services.AddScoped<AddCapabilityHandler>();
    builder.Services.AddScoped<ProjectBuilder.Application.Modeling.UpdateActor.UpdateActorHandler>();
    builder.Services.AddScoped<ProjectBuilder.Application.Modeling.UpdateOutcome.UpdateOutcomeHandler>();
    builder.Services.AddScoped<DefineNarrativeHandler>();
    builder.Services.AddScoped<DefineStateLogicHandler>();
    builder.Services.AddScoped<DefinePathHandler>();
    builder.Services.AddScoped<DefineSystemContextHandler>();
    builder.Services.AddScoped<DefineEvidencePacketHandler>();
    builder.Services.AddScoped<CanvasViewHandler>();
}
builder.Services.AddSingleton(CreateLocalDevelopmentAccess(builder.Environment));
builder.Services.AddSingleton<IProjectCreationAuthorizer>(services =>
    services.GetRequiredService<LocalDevelopmentProjectAccess>());
builder.Services.AddSingleton<IProjectEditAuthorizer>(services =>
    services.GetRequiredService<LocalDevelopmentProjectAccess>());

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    await app.Services.ApplyFoundationMigrationsAsync();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseWhen(
    context => !context.Request.Path.StartsWithSegments("/api"),
    branch => branch.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true));
app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(ProjectBuilder.Web.Client.ClientAssemblyMarker).Assembly);
app.MapGet("/api/foundation", (FoundationResponse response) => response)
    .WithName("GetFoundation");
if (projectFeaturesAvailable)
{
    app.MapProjectEndpoints();
}
app.MapDefaultEndpoints();
app.MapFallback(context =>
{
    var acceptsHtml = context.Request.Headers.Accept.Any(value =>
        value?.Contains("text/html", StringComparison.OrdinalIgnoreCase) == true);
    if (context.Request.Path.StartsWithSegments("/api") ||
        !HttpMethods.IsGet(context.Request.Method) && !HttpMethods.IsHead(context.Request.Method) ||
        !acceptsHtml)
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        return Task.CompletedTask;
    }

    context.Response.Redirect("/not-found");
    return Task.CompletedTask;
});

app.Run();

static FoundationResponse CreateFoundationResponse()
{
    var metadata = typeof(Program).Assembly
        .GetCustomAttributes<AssemblyMetadataAttribute>()
        .ToDictionary(attribute => attribute.Key, attribute => attribute.Value, StringComparer.Ordinal);

    return FoundationDefinition.Describe(
        metadata.GetValueOrDefault("BuildVersion") ?? "0.0.0",
        metadata.GetValueOrDefault("SourceRevision") ?? "unknown");
}

static LocalDevelopmentProjectAccess CreateLocalDevelopmentAccess(IHostEnvironment environment)
{
    var workspace = WorkspaceId.Parse("0198ad00-0000-7000-8000-000000000700");
    return new LocalDevelopmentProjectAccess(
        environment.IsDevelopment(),
        ((SemanticResult<WorkspaceId>.Accepted)workspace).Value);
}

public partial class Program;
