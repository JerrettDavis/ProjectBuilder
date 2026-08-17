using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Playwright;
using ProjectBuilder.Contracts.Projects;
using Testcontainers.PostgreSql;

namespace ProjectBuilder.EndToEnd.Tests;

[Category("EndToEnd")]
public sealed class CreateProjectScenarioTests
{
    private static readonly long[] SixRevisionHistory = [6, 5, 4, 3, 2, 1];
    private static readonly int[] SixOperationCounts = [5, 8, 7, 2, 1, 1];
    private static readonly string[] GuidedItemScanResults = ["Added", "ProductNotFound", "PriceUnavailable", "RestrictedProduct", "ServiceUnavailable"];

    private PostgreSqlContainer? database;
    private WebApplicationFactory<Program>? application;
    private IPlaywright? playwright;
    private IBrowser? browser;
    private HttpClient? api;
    private string baseUrl = string.Empty;
    private string evidenceDirectory = string.Empty;

    [OneTimeSetUp]
    public async Task StartRealBoundaries()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("PB_RUN_E2E"), "true", StringComparison.OrdinalIgnoreCase))
        {
            Assert.Ignore("Run through eng/e2e or eng/verify to enable real browser and PostgreSQL boundaries.");
        }

        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__projectbuilder");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            database = new PostgreSqlBuilder("postgres:18-alpine")
                .WithDatabase("projectbuilder_e2e")
                .WithUsername("projectbuilder")
                .WithPassword("local-e2e-only")
                .Build();
            await database.StartAsync();
            connectionString = database.GetConnectionString();
        }

        application = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");
                builder.UseSetting("ConnectionStrings:projectbuilder", connectionString);
            });
        application.UseKestrel(options => options.Listen(IPAddress.Loopback, 0));
        api = application.CreateClient();
        baseUrl = api.BaseAddress!.GetLeftPart(UriPartial.Authority);
        TestContext.Out.WriteLine($"E2E server: {baseUrl}");

        playwright = await Playwright.CreateAsync();
        browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true,
        });

        evidenceDirectory = Path.GetFullPath(
            Path.Combine(TestContext.CurrentContext.WorkDirectory, "..", "..", "..", "..", "..", "artifacts", "e2e", "foundation-journeys"));
        Directory.CreateDirectory(evidenceDirectory);
    }

    [OneTimeTearDown]
    public async Task StopRealBoundaries()
    {
        if (browser is not null)
        {
            await browser.DisposeAsync();
        }

        playwright?.Dispose();
        api?.Dispose();
        application?.Dispose();
        if (database is not null)
        {
            await database.DisposeAsync();
        }
    }

    [Test]
    [Property("ModelClaim", "0198ad00-0000-7000-8000-000000000233")]
    public async Task Given_an_internal_discovery_workshop_when_a_facilitator_runs_the_agenda_then_provisional_truth_is_recoverable_participant_safe_and_exportable()
    {
        using var createRequest = CreateApiRequest(
            "0198ad00-0000-7000-8000-000000000700",
            "0198ad00-0000-7000-8000-000000000740",
            new CreateProjectRequest("Project Builder discovery workshop", "Reach shared understanding from explicit model truth.",
                "The team leaves with an owned next slice and visible uncertainty.", "Create the D06 workshop evidence project."));
        using var createResponse = await api!.SendAsync(createRequest);
        var project = await createResponse.Content.ReadFromJsonAsync<ProjectResponse>();
        Assert.That(createResponse.StatusCode, Is.EqualTo(HttpStatusCode.Created));

        var actors = new[]
        {
            new AddActorRequest("Facilitator", "humanRole", "Guides collaborative modeling while keeping uncertainty visible.", "Reach shared understanding", "Protect the agenda", "May frame and summarize discussion", "Cannot commit provisional notes as truth", "Model the facilitator.", "known"),
            new AddActorRequest("Contributor", "humanRole", "Builds and verifies the repository.", "Deliver coherent slices", "Explain implementation evidence", "May propose modeled changes", "Cannot approve their own unsupported claim", "Model the contributor.", "known"),
            new AddActorRequest("Reviewer", "humanRole", "Challenges claims and evidence.", "Reach a reviewable decision", "Expose missing proof", "May dispute unsupported claims", "Cannot silently rewrite authored meaning", "Model the reviewer.", "known"),
        };
        AddActorResponse? facilitator = null;
        for (var index = 0; index < actors.Length; index++)
        {
            using var request = CreateEditApiRequest($"/api/v1/projects/{project!.Id}/actors", (index + 1).ToString(CultureInfo.InvariantCulture),
                $"0198ad00-0000-7000-8000-00000000074{index + 1}", actors[index]);
            using var response = await api.SendAsync(request);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
            facilitator ??= await response.Content.ReadFromJsonAsync<AddActorResponse>();
        }
        using var outcomeRequest = CreateEditApiRequest($"/api/v1/projects/{project!.Id}/outcomes", "4",
            "0198ad00-0000-7000-8000-000000000744",
            new AddOutcomeRequest("Workshop closes with owned direction", "The team can name its next coherent slice and unresolved uncertainty.",
                "One next action is named\nEvery provisional note retains an owner and status", facilitator!.Actor.Id,
                "Make the workshop outcome observable.", "known"));
        using var outcomeResponse = await api.SendAsync(outcomeRequest);
        Assert.That(outcomeResponse.StatusCode, Is.EqualTo(HttpStatusCode.Created));

        var firstBrief = await api.GetStringAsync($"/api/v1/projects/{project.Id}/workshop");
        var secondBrief = await api.GetStringAsync($"/api/v1/projects/{project.Id}/workshop");
        Assert.That(secondBrief, Is.EqualTo(firstBrief));

        var page = await NewPageAsync();
        await page.GotoAsync($"{baseUrl}/projects/{project.Id}/workshop");
        var studio = page.GetByTestId("workshop-studio");
        await Assertions.Expect(studio.GetByRole(AriaRole.Heading, new() { Name = "Make the room think together" })).ToBeVisibleAsync();
        await Assertions.Expect(studio).ToContainTextAsync("workshop/1");
        await Assertions.Expect(studio).ToContainTextAsync("65 minutes · 6 movements");
        await Assertions.Expect(studio.GetByRole(AriaRole.Heading, new() { Name = "Participant view" })).ToBeVisibleAsync();
        await CaptureAsync(page, "129-workshop-facilitator-ready.png");

        await studio.GetByRole(AriaRole.Button, new() { Name = "Start workshop" }).ClickAsync();
        await Assertions.Expect(studio.GetByText("Workshop live", new() { Exact = true })).ToBeVisibleAsync();
        await CaptureAsync(page, "130-workshop-live-roster.png");

        var movements = new[] { "Align on the outcome", "Hear the modeled participants", "Walk one ordinary scenario", "Examine material tensions", "Choose the next coherent slice", "Confirm owners and unresolved questions" };
        foreach (var movement in movements)
        {
            await Assertions.Expect(studio.GetByTestId("workshop-active-movement").GetByRole(AriaRole.Heading, new() { Name = movement })).ToBeVisibleAsync();
            await studio.GetByRole(AriaRole.Button, new() { Name = "Mark discussed" }).ClickAsync();
            if (movement != movements[^1]) await studio.GetByRole(AriaRole.Button, new() { Name = "Next →" }).ClickAsync();
        }
        await Assertions.Expect(studio.GetByText("6 discussed", new() { Exact = true })).ToBeVisibleAsync();
        await CaptureAsync(page, "131-workshop-agenda-complete.png");

        await studio.GetByLabel("Capture kind").SelectOptionAsync("Decision");
        await studio.GetByLabel("Capture statement").FillAsync("D06 remains a query and provisional workshop boundary.");
        await studio.GetByLabel("Capture owner").FillAsync("Facilitator");
        await studio.GetByRole(AriaRole.Button, new() { Name = "Capture note" }).ClickAsync();
        await studio.GetByLabel("Capture kind").SelectOptionAsync("Assumption");
        await studio.GetByLabel("Capture statement").FillAsync("Workshop export is sufficient until canonical decision commands exist.");
        await studio.GetByLabel("Capture owner").FillAsync("Reviewer");
        await studio.GetByRole(AriaRole.Button, new() { Name = "Capture note" }).ClickAsync();
        await studio.GetByLabel("Capture kind").SelectOptionAsync("Question");
        await studio.GetByLabel("Capture statement").FillAsync("Which authority may accept a workshop decision?");
        await studio.GetByLabel("Capture owner").FillAsync("Unknown");
        await studio.GetByRole(AriaRole.Button, new() { Name = "Capture note" }).ClickAsync();
        await studio.GetByLabel("Parking lot item").FillAsync("Define canonical Decision lifecycle in a future owned slice.");
        await studio.GetByRole(AriaRole.Button, new() { Name = "Park item" }).ClickAsync();
        await Assertions.Expect(studio).ToContainTextAsync("Not canonical yet.");
        await Assertions.Expect(studio.GetByTestId("workshop-parking")).ToContainTextAsync("Define canonical Decision lifecycle");
        await CaptureAsync(page, "132-workshop-provisional-record.png");

        var exportHref = await studio.GetByTestId("workshop-export").GetAttributeAsync("href");
        Assert.Multiple(() =>
        {
            Assert.That(exportHref, Does.StartWith("data:application/json"));
            Assert.That(Uri.UnescapeDataString(exportHref!), Does.Contain("Which authority may accept a workshop decision?"));
            Assert.That(Uri.UnescapeDataString(exportHref!), Does.Contain("\"modelRevision\": 5"));
        });

        await page.ReloadAsync();
        await Assertions.Expect(studio.GetByText("6 discussed", new() { Exact = true })).ToBeVisibleAsync();
        await Assertions.Expect(studio.GetByTestId("workshop-captures")).ToContainTextAsync("D06 remains a query");

        await studio.GetByRole(AriaRole.Button, new() { Name = "Participant view" }).ClickAsync();
        await Assertions.Expect(studio.Locator(".participant-board").GetByRole(AriaRole.Heading, new() { Name = "Confirm owners and unresolved questions" })).ToBeVisibleAsync();
        await Assertions.Expect(studio.GetByText("Facilitator notes and controls are intentionally hidden in this view.")).ToBeVisibleAsync();
        await Assertions.Expect(studio.GetByRole(AriaRole.Button, new() { Name = "Mark discussed" })).ToHaveCountAsync(0);
        await Assertions.Expect(studio.GetByRole(AriaRole.Button, new() { Name = "Pause" })).ToHaveCountAsync(0);
        await CaptureAsync(page, "133-workshop-participant-view.png");

        var darkPage = await NewPageAsync(ColorScheme.Dark);
        await darkPage.GotoAsync(page.Url);
        await Assertions.Expect(darkPage.GetByTestId("workshop-studio").GetByRole(AriaRole.Heading, new() { Name = "Make the room think together" })).ToBeVisibleAsync();
        await CaptureAsync(darkPage, "134-workshop-dark-recovered.png");
        await darkPage.Context.CloseAsync();

        var narrowPage = await NewPageAsync(ColorScheme.Light, 620, 900);
        await narrowPage.GotoAsync(page.Url);
        await Assertions.Expect(narrowPage.GetByTestId("workshop-studio").GetByRole(AriaRole.Heading, new() { Name = "Agenda from model truth" })).ToBeVisibleAsync();
        await CaptureAsync(narrowPage, "135-workshop-responsive-recovered.png");
        await narrowPage.Context.CloseAsync();
    }

    [Test]
    [Property("ModelClaim", "0198ad00-0000-7000-8000-000000000215")]
    public async Task Given_a_contributor_enters_the_studio_then_navigation_theme_and_route_recovery_are_accessible()
    {
        var page = await NewPageAsync();
        await page.GotoAsync(baseUrl);

        await ExpectHeadingAsync(page, "Project Builder");
        await Assertions.Expect(page.GetByRole(AriaRole.Banner)).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Navigation, new() { Name = "Global navigation" })).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Complementary, new() { Name = "Studio navigation" })).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Complementary, new() { Name = "Model the outcome" })).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Contentinfo, new() { Name = "Studio workbench" })).ToBeVisibleAsync();

        var skipLink = page.GetByRole(AriaRole.Link, new() { Name = "Skip to work surface" });
        await page.Keyboard.PressAsync("Tab");
        await Assertions.Expect(skipLink).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Enter");
        await Assertions.Expect(page.Locator("#studio-work-surface")).ToBeFocusedAsync();
        await CaptureAsync(page, "27-studio-shell-light.png");

        var darkPage = await NewPageAsync(ColorScheme.Dark);
        await darkPage.GotoAsync(baseUrl);
        await Assertions.Expect(darkPage.GetByText("Definition studio", new() { Exact = true })).ToBeVisibleAsync();
        await CaptureAsync(darkPage, "28-studio-shell-dark.png");
        await darkPage.Context.CloseAsync();

        var narrowPage = await NewPageAsync(ColorScheme.Light, 760, 900);
        await narrowPage.GotoAsync(baseUrl);
        await Assertions.Expect(narrowPage.Locator("summary").GetByText("Studio navigation", new() { Exact = true })).ToBeVisibleAsync();
        await Assertions.Expect(narrowPage.GetByRole(AriaRole.Complementary, new() { Name = "Studio navigation" })).ToBeHiddenAsync();
        await CaptureAsync(narrowPage, "29-studio-shell-responsive.png");
        await narrowPage.Context.CloseAsync();

        await page.GotoAsync($"{baseUrl}/model/location-that-does-not-exist");
        await ExpectHeadingAsync(page, "We couldn't find that studio location");
        await Assertions.Expect(page.GetByText("No model state was changed.")).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Link, new() { Name = "Return to studio home" })).ToBeVisibleAsync();
        await CaptureAsync(page, "30-studio-route-recovery.png");
    }

    [Test]
    [Property("ModelClaim", "0198ad00-0000-7000-8000-000000000216")]
    public async Task Given_a_project_changes_when_the_outcome_cockpit_opens_then_gaps_and_next_action_follow_model_truth()
    {
        var page = await NewPageAsync();
        await page.GotoAsync($"{baseUrl}/projects/new");
        await page.GetByLabel("Project name").FillAsync("Outcome cockpit journey");
        await page.GetByLabel("Purpose").FillAsync("Orient a modeler around semantic progress without a completeness score.");
        await page.GetByLabel("Intended outcome").FillAsync("A modeler can choose the next definition from current model truth.");
        await page.GetByLabel("Change-set reason").FillAsync("Start the C02 cockpit journey.");
        await page.GetByRole(AriaRole.Button, new() { Name = "Create project" }).ClickAsync();
        await page.GetByRole(AriaRole.Link, new() { Name = "Open project overview" }).ClickAsync();

        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Identify the first actor" })).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Semantic topology" })).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByText("Evidence records are not exposed by the current runtime model query.", new() { Exact = true })).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByText("Explicit, not scored")).ToBeVisibleAsync();
        await CaptureAsync(page, "31-outcome-cockpit-empty.png");

        await page.GetByRole(AriaRole.Link, new() { Name = "Add an actor", Exact = true }).ClickAsync();
        await page.GetByLabel("Actor name").FillAsync("Modeler");
        await page.GetByLabel("Actor kind").SelectOptionAsync("humanRole");
        await page.GetByLabel("Contextual role").FillAsync("A person choosing and reviewing semantic definitions.");
        await page.GetByLabel("Goals").FillAsync("Choose the next definition from visible gaps");
        await page.GetByLabel("Responsibilities").FillAsync("Preserve explicit model truth");
        await page.GetByLabel("Change reason").FillAsync("Define the cockpit beneficiary.");
        await page.GetByRole(AriaRole.Button, new() { Name = "Commit actor" }).ClickAsync();
        await page.GetByRole(AriaRole.Link, new() { Name = "Add an outcome" }).ClickAsync();

        await page.GetByLabel("Outcome name").FillAsync("Next definition is evident");
        await page.GetByLabel("Beneficiary").SelectOptionAsync(new SelectOptionValue { Label = "Modeler" });
        await page.GetByLabel("Observable outcome statement").FillAsync("A modeler can choose the next definition from current model truth.");
        await page.GetByLabel("Success signals").FillAsync("Recommended action follows the first missing packet\nGaps are explicit and not scored");
        await page.GetByLabel("Change reason").FillAsync("Make the cockpit outcome observable.");
        await page.GetByRole(AriaRole.Button, new() { Name = "Commit outcome" }).ClickAsync();
        await page.GetByRole(AriaRole.Link, new() { Name = "Review project model" }).ClickAsync();

        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Define the complete scenario" })).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByText("1 actor(s) define participation.")).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByText("1 observable outcome(s) are linked to beneficiaries.")).ToBeVisibleAsync();
        await CaptureAsync(page, "32-outcome-cockpit-populated.png");
        var modelUrl = page.Url;

        var darkPage = await NewPageAsync(ColorScheme.Dark);
        await darkPage.GotoAsync(modelUrl);
        await Assertions.Expect(darkPage.GetByRole(AriaRole.Heading, new() { Name = "Semantic topology" })).ToBeVisibleAsync();
        await CaptureAsync(darkPage, "33-outcome-cockpit-dark.png");
        await darkPage.Context.CloseAsync();

        var narrowPage = await NewPageAsync(ColorScheme.Light, 760, 900);
        await narrowPage.GotoAsync(modelUrl);
        await Assertions.Expect(narrowPage.GetByRole(AriaRole.Heading, new() { Name = "Define the complete scenario" })).ToBeVisibleAsync();
        await CaptureAsync(narrowPage, "34-outcome-cockpit-responsive.png");
        await narrowPage.Context.CloseAsync();

        await page.GotoAsync($"{baseUrl}/projects/0198ad00-0000-7000-8000-000000009999");
        await ExpectHeadingAsync(page, "Project not found");
        await Assertions.Expect(page.GetByText("No model state was changed.")).ToBeVisibleAsync();
        await CaptureAsync(page, "35-project-cockpit-unavailable.png");
    }

    [Test]
    [Property("ModelClaim", "0198ad00-0000-7000-8000-000000000217")]
    public async Task Given_typed_definitions_when_a_modeler_uses_the_explorer_then_selection_and_view_order_remain_non_semantic()
    {
        var page = await NewPageAsync();
        await page.GotoAsync($"{baseUrl}/projects/new");
        await page.GetByLabel("Project name").FillAsync("Semantic Explorer journey");
        await page.GetByLabel("Purpose").FillAsync("Navigate a typed project model without creating another source of truth.");
        await page.GetByLabel("Intended outcome").FillAsync("A modeler can find, select, organize, and open a definition without drag.");
        await page.GetByLabel("Change-set reason").FillAsync("Start the C03 Explorer journey.");
        await page.GetByRole(AriaRole.Button, new() { Name = "Create project" }).ClickAsync();
        await page.GetByRole(AriaRole.Link, new() { Name = "Open project overview" }).ClickAsync();

        await page.GetByRole(AriaRole.Link, new() { Name = "Add an actor", Exact = true }).ClickAsync();
        await page.GetByLabel("Actor name").FillAsync("Modeler");
        await page.GetByLabel("Actor kind").SelectOptionAsync("humanRole");
        await page.GetByLabel("Contextual role").FillAsync("Finds and organizes definitions in the Studio view.");
        await page.GetByLabel("Goals").FillAsync("Navigate without losing semantic context");
        await page.GetByLabel("Responsibilities").FillAsync("Keep view state separate from model truth");
        await page.GetByLabel("Change reason").FillAsync("Add the Explorer operator.");
        await page.GetByRole(AriaRole.Button, new() { Name = "Commit actor" }).ClickAsync();
        await page.GetByRole(AriaRole.Link, new() { Name = "Project overview" }).ClickAsync();

        await page.GetByLabel("Actors").GetByRole(AriaRole.Link, new() { Name = "Add actor" }).ClickAsync();
        await page.GetByLabel("Actor name").FillAsync("Reviewer");
        await page.GetByLabel("Actor kind").SelectOptionAsync("humanRole");
        await page.GetByLabel("Contextual role").FillAsync("Opens a stable selected definition for review.");
        await page.GetByLabel("Goals").FillAsync("Review the selected semantic definition");
        await page.GetByLabel("Responsibilities").FillAsync("Verify navigation context");
        await page.GetByLabel("Change reason").FillAsync("Add a second definition for view-only ordering proof.");
        await page.GetByRole(AriaRole.Button, new() { Name = "Commit actor" }).ClickAsync();
        await page.GetByRole(AriaRole.Link, new() { Name = "Project overview" }).ClickAsync();

        await ExpectHeadingAsync(page, "Semantic Explorer");
        var tree = page.GetByTestId("semantic-tree");
        await Assertions.Expect(tree).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByText("Virtualized · client-safe contracts · no second model", new() { Exact = true })).ToBeVisibleAsync();
        await CaptureAsync(page, "37-semantic-explorer-populated.png");

        var search = page.GetByTestId("semantic-search");
        await search.FocusAsync();
        await search.FillAsync("Reviewer");
        await Assertions.Expect(search).ToBeFocusedAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Treeitem).Filter(new() { HasText = "Reviewer" })).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Treeitem).Filter(new() { HasText = "Modeler" })).ToHaveCountAsync(0);
        await CaptureAsync(page, "38-semantic-explorer-filter-focus.png");

        await search.FillAsync(string.Empty);
        await tree.FocusAsync();
        await tree.PressAsync("Home");
        await tree.PressAsync("ArrowDown");
        await tree.PressAsync("ArrowDown");
        await tree.PressAsync("ArrowDown");
        await tree.PressAsync("Enter");
        await Assertions.Expect(tree).ToBeFocusedAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Modeler", Exact = true })).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Link, new() { Name = "Define related outcome" })).ToBeVisibleAsync();
        await Assertions.Expect(page).ToHaveURLAsync(new Regex(@"\?selected=[0-9a-f-]+$", RegexOptions.IgnoreCase));
        await CaptureAsync(page, "39-semantic-explorer-stable-selection.png");

        await page.GetByRole(AriaRole.Button, new() { Name = "Move later in this view" }).ClickAsync();
        await Assertions.Expect(page.GetByText("Moved Modeler later in the Context view. Model order unchanged.", new() { Exact = true })).ToBeVisibleAsync();
        await tree.FocusAsync();
        await tree.PressAsync("Alt+ArrowUp");
        await Assertions.Expect(tree).ToBeFocusedAsync();
        await Assertions.Expect(page.GetByText("Moved Modeler earlier in the Context view. Model order unchanged.", new() { Exact = true })).ToBeVisibleAsync();

        var selectedUrl = page.Url;
        await page.GetByRole(AriaRole.Link, new() { Name = "Open definition" }).ClickAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Actors", Exact = true })).ToBeVisibleAsync();

        var darkPage = await NewPageAsync(ColorScheme.Dark);
        await darkPage.GotoAsync(selectedUrl);
        await ExpectHeadingAsync(darkPage, "Semantic Explorer");
        await Assertions.Expect(darkPage.GetByRole(AriaRole.Heading, new() { Name = "Modeler", Exact = true })).ToBeVisibleAsync();
        await CaptureAsync(darkPage, "40-semantic-explorer-dark.png");
        await darkPage.Context.CloseAsync();

        var narrowPage = await NewPageAsync(ColorScheme.Light, 760, 900);
        await narrowPage.GotoAsync(selectedUrl);
        await ExpectHeadingAsync(narrowPage, "Semantic Explorer");
        await Assertions.Expect(narrowPage.GetByTestId("semantic-tree")).ToBeVisibleAsync();
        await CaptureAsync(narrowPage, "41-semantic-explorer-responsive.png");
        await narrowPage.Context.CloseAsync();
    }

    [Test]
    [Property("ModelClaim", "0198ad00-0000-7000-8000-000000000214")]
    public async Task Given_a_canonical_document_when_imported_then_it_is_reviewable_exportable_and_unsafe_content_is_rejected()
    {
        var page = await NewPageAsync();
        var fixture = await File.ReadAllTextAsync(Path.Combine(
            TestContext.CurrentContext.TestDirectory, "Fixtures", "example-importable-project.project-builder.json"));

        await page.GotoAsync($"{baseUrl}/projects/import");
        await ExpectHeadingAsync(page, "Import a project");
        await CaptureAsync(page, "22-canonical-import-definition.png");
        await page.GetByLabel("Project JSON").FillAsync(fixture);
        await page.GetByRole(AriaRole.Button, new() { Name = "Validate and import" }).ClickAsync();

        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Portable checkout model is ready" })).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByText("2 elements and 1 relations were committed in one change set.")).ToBeVisibleAsync();
        await CaptureAsync(page, "23-canonical-import-committed.png");

        const string projectId = "0198ad00-0000-7000-8000-000000000900";
        using var exportedResponse = await api!.GetAsync($"/api/v1/projects/{projectId}/export");
        var canonical = await exportedResponse.Content.ReadAsStringAsync();
        using var modelResponse = await api.GetAsync($"/api/v1/projects/{projectId}/model");
        var model = await modelResponse.Content.ReadFromJsonAsync<ProjectModelResponse>();
        Assert.Multiple(() =>
        {
            Assert.That(exportedResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(exportedResponse.Content.Headers.ContentType!.MediaType,
                Is.EqualTo("application/vnd.projectbuilder.project+json"));
            Assert.That(canonical, Does.EndWith("\n"));
            Assert.That(modelResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(model!.Actors, Has.Count.EqualTo(1));
            Assert.That(model.Outcomes, Has.Count.EqualTo(1));
            Assert.That(model.Relations, Has.Count.EqualTo(1));
            Assert.That(model.ChangeSets, Has.Count.EqualTo(1));
            Assert.That(model.ChangeSets[0].OperationCount, Is.EqualTo(4));
        });

        var unsafeDocument = fixture.Replace(
            "\"extensions\": {}",
            "\"extensions\": {\"attacker.payload\": {\"version\": \"1\", \"schema\": \"javascript:alert(1)\", \"data\": {}}}",
            StringComparison.Ordinal);
        await page.GotoAsync($"{baseUrl}/projects/import");
        await page.GetByLabel("Project JSON").FillAsync(unsafeDocument);
        await page.GetByRole(AriaRole.Button, new() { Name = "Validate and import" }).ClickAsync();

        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Name = "The project document was not imported" })).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByText("import.uri.unsafe", new() { Exact = false })).ToBeVisibleAsync();
        await CaptureAsync(page, "24-unsafe-import-rejected.png");
    }

    [Test]
    [Property("ModelClaim", "0198ad00-0000-7000-8000-000000000205")]
    public async Task Given_an_authorized_modeler_when_they_create_a_project_then_revision_one_is_visible()
    {
        var page = await NewPageAsync();

        await page.GotoAsync($"{baseUrl}/projects/new");
        await ExpectHeadingAsync(page, "Create a project");
        Assert.That(await page.Locator("input[name='__RequestVerificationToken']").CountAsync(), Is.EqualTo(1));
        await CaptureAsync(page, "01-empty-project-definition.png");

        await page.GetByLabel("Project name").FillAsync("Checkout discovery");
        await page.GetByLabel("Purpose").FillAsync("Understand the staffed checkout domain before selecting implementation structure.");
        await page.GetByLabel("Intended outcome").FillAsync("A modeler can explain how a sale reaches an observable completion.");
        await page.GetByLabel("Change-set reason").FillAsync("Create the checkout discovery project.");
        await page.GetByRole(AriaRole.Button, new() { Name = "Create project" }).ClickAsync();

        await ExpectHeadingAsync(page, "Checkout discovery is ready");
        await CaptureAsync(page, "02-project-created.png");
        await page.GetByRole(AriaRole.Link, new() { Name = "Open project overview" }).ClickAsync();

        await ExpectHeadingAsync(page, "Checkout discovery");
        await Assertions.Expect(page.GetByText("Semantic model · Revision 1")).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByText("Create the checkout discovery project.", new() { Exact = true })).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Actors" })).ToBeVisibleAsync();
        await CaptureAsync(page, "03-project-overview.png");
    }

    [Test]
    [Property("ModelClaim", "0198ad00-0000-7000-8000-000000000208")]
    [Property("ModelClaim", "0198ad00-0000-7000-8000-000000000212")]
    public async Task Given_a_project_when_an_actor_and_outcome_are_committed_then_the_model_is_reviewable_end_to_end()
    {
        var page = await NewPageAsync();
        await page.GotoAsync($"{baseUrl}/projects/new");
        await page.GetByLabel("Project name").FillAsync("Repository verification journey");
        await page.GetByLabel("Purpose").FillAsync("Model the human outcome delivered by the repository foundation.");
        await page.GetByLabel("Intended outcome").FillAsync("A contributor can build, run, and verify Project Builder from a clean clone.");
        await page.GetByLabel("Change-set reason").FillAsync("Start the executable dogfood journey.");
        await page.GetByRole(AriaRole.Button, new() { Name = "Create project" }).ClickAsync();
        await page.GetByRole(AriaRole.Link, new() { Name = "Open project overview" }).ClickAsync();
        await CaptureAsync(page, "05-model-revision-one.png");

        await page.GetByRole(AriaRole.Link, new() { Name = "Add actor" }).ClickAsync();
        await ExpectHeadingAsync(page, "Add an actor");
        await page.GetByLabel("Actor name").FillAsync("Contributor");
        await page.GetByLabel("Actor kind").SelectOptionAsync("humanRole");
        await page.GetByLabel("Contextual role").FillAsync("A person changing or verifying the repository.");
        await page.GetByLabel("Goals").FillAsync("Run one documented verification command");
        await page.GetByLabel("Responsibilities").FillAsync("Preserve architecture invariants");
        await page.GetByLabel("Change reason").FillAsync("Identify the contributor beneficiary.");
        await CaptureAsync(page, "06-actor-definition.png");
        await page.GetByRole(AriaRole.Button, new() { Name = "Commit actor" }).ClickAsync();
        await ExpectHeadingAsync(page, "Contributor added");
        await Assertions.Expect(page.GetByText("Revision 2 committed")).ToBeVisibleAsync();
        await CaptureAsync(page, "07-actor-committed.png");

        await page.GetByRole(AriaRole.Link, new() { Name = "Add an outcome" }).ClickAsync();
        await ExpectHeadingAsync(page, "Add an outcome");
        await page.GetByLabel("Outcome name").FillAsync("Repository can be verified");
        await page.GetByLabel("Beneficiary").SelectOptionAsync(new SelectOptionValue { Label = "Contributor" });
        await page.GetByLabel("Observable outcome statement").FillAsync("A contributor can build, run, and verify Project Builder from a clean clone.");
        await page.GetByLabel("Success signals").FillAsync("Verification exits successfully\nHealth endpoint reports Healthy\nArchitecture rules pass");
        await page.GetByLabel("Change reason").FillAsync("Define the observable foundation outcome.");
        await CaptureAsync(page, "08-outcome-definition.png");
        await page.GetByRole(AriaRole.Button, new() { Name = "Commit outcome" }).ClickAsync();
        await ExpectHeadingAsync(page, "Repository can be verified added");
        await Assertions.Expect(page.GetByText("Revision 3 committed")).ToBeVisibleAsync();
        await page.GetByRole(AriaRole.Link, new() { Name = "Review project model" }).ClickAsync();

        await Assertions.Expect(page.GetByText("Semantic model · Revision 3")).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByText("Contributor", new() { Exact = true })).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Repository can be verified", Exact = true })).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByText("Beneficiary: Contributor")).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Contributor benefits from Repository can be verified" })).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByText("Allowed direction: actor → outcome")).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByText("Cardinality: one source to many targets · Unique source/target pair: yes")).ToBeVisibleAsync();
        var exportLink = page.GetByRole(AriaRole.Link, new() { Name = "Export canonical JSON" });
        await Assertions.Expect(exportLink).ToBeVisibleAsync();
        var exportPath = await exportLink.GetAttributeAsync("href");
        using var exportResponse = await api!.GetAsync(exportPath);
        var exported = await exportResponse.Content.ReadAsStringAsync();
        Assert.Multiple(() =>
        {
            Assert.That(exportResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(exportResponse.Content.Headers.ContentType!.MediaType,
                Is.EqualTo("application/vnd.projectbuilder.project+json"));
            Assert.That(exported, Does.Contain("\"revision\": 3"));
            Assert.That(exported, Does.EndWith("\n"));
        });
        await CaptureAsync(page, "09-actor-outcome-model.png");
        await CaptureAsync(page, "20-typed-relation-registry.png");
        await CaptureAsync(page, "25-native-canonical-export.png");

        var darkPage = await NewPageAsync(ColorScheme.Dark);
        await darkPage.GotoAsync(page.Url);
        await Assertions.Expect(darkPage.GetByRole(AriaRole.Navigation, new() { Name = "Model definition stages" })).ToBeVisibleAsync();
        await CaptureAsync(darkPage, "26-studio-dark-theme.png");
        await darkPage.Context.CloseAsync();
    }

    [Test]
    [Property("ModelClaim", "0198ad00-0000-7000-8000-000000000209")]
    [Property("ModelClaim", "0198ad00-0000-7000-8000-000000000210")]
    [Property("ModelClaim", "0198ad00-0000-7000-8000-000000000213")]
    [Property("ModelClaim", "0198ad00-0000-7000-8000-000000000216")]
    [Property("ModelClaim", "0198ad00-0000-7000-8000-000000000217")]
    public async Task Given_participants_and_an_outcome_when_a_complete_scenario_is_defined_then_ordered_narrative_is_reviewable()
    {
        var page = await NewPageAsync();
        await page.GotoAsync($"{baseUrl}/projects/new");
        await page.GetByLabel("Project name").FillAsync("Create Project narrative journey");
        await page.GetByLabel("Purpose").FillAsync("Prove that Project Builder can model its own project creation behavior.");
        await page.GetByLabel("Intended outcome").FillAsync("A modeler can review the complete ordered Create Project narrative.");
        await page.GetByLabel("Change-set reason").FillAsync("Start the narrative dogfood journey.");
        await page.GetByRole(AriaRole.Button, new() { Name = "Create project" }).ClickAsync();
        await page.GetByRole(AriaRole.Link, new() { Name = "Open project overview" }).ClickAsync();

        await page.GetByRole(AriaRole.Link, new() { Name = "Add actor" }).ClickAsync();
        await page.GetByLabel("Actor name").FillAsync("Modeler");
        await page.GetByLabel("Contextual role").FillAsync("A person defining the project purpose.");
        await page.GetByLabel("Goals").FillAsync("Create a purpose-led project");
        await page.GetByLabel("Responsibilities").FillAsync("Provide the project definition");
        await page.GetByLabel("Change reason").FillAsync("Add the initiating participant.");
        await page.GetByRole(AriaRole.Button, new() { Name = "Commit actor" }).ClickAsync();
        await Assertions.Expect(page.GetByTestId("actor-committed")).ToContainTextAsync("Modeler added");
        await page.GetByTestId("actor-committed").GetByRole(AriaRole.Link, new() { Name = "Project overview" }).ClickAsync();

        await page.GetByRole(AriaRole.Link, new() { Name = "Add actor" }).ClickAsync();
        await page.GetByLabel("Actor name").FillAsync("Project Builder");
        await page.GetByLabel("Actor kind").SelectOptionAsync("systemRole");
        await page.GetByLabel("Contextual role").FillAsync("The modeled system receiving and validating project intent.");
        await page.GetByLabel("Responsibilities").FillAsync("Validate and persist the accepted project definition");
        await page.GetByLabel("Change reason").FillAsync("Add the receiving participant.");
        await page.GetByRole(AriaRole.Button, new() { Name = "Commit actor" }).ClickAsync();
        await Assertions.Expect(page.GetByTestId("actor-committed")).ToContainTextAsync("Project Builder added");
        await page.GetByRole(AriaRole.Link, new() { Name = "Add an outcome" }).ClickAsync();

        await page.GetByLabel("Outcome name").FillAsync("Project purpose is reviewable");
        await page.GetByLabel("Beneficiary").SelectOptionAsync(new SelectOptionValue { Label = "Modeler" });
        await page.GetByLabel("Observable outcome statement").FillAsync("A modeler can reopen the accepted project definition at revision 1.");
        await page.GetByLabel("Success signals").FillAsync("Purpose and intended outcome are visible\nCreation reason is visible");
        await page.GetByLabel("Change reason").FillAsync("Define the narrative outcome.");
        await page.GetByRole(AriaRole.Button, new() { Name = "Commit outcome" }).ClickAsync();
        await page.GetByRole(AriaRole.Link, new() { Name = "Review project model" }).ClickAsync();
        await CaptureAsync(page, "10-narrative-prerequisites.png");

        await page.GetByRole(AriaRole.Link, new() { Name = "Define narrative" }).ClickAsync();
        await ExpectHeadingAsync(page, "Define a complete scenario narrative");
        await Assertions.Expect(page.GetByTestId("narrative-composer")).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Complementary, new() { Name = "Narrative structure" })).ToBeVisibleAsync();
        await CaptureAsync(page, "59-narrative-composer-light-clean.png");

        var narrativeUrl = page.Url;
        var darkNarrativePage = await NewPageAsync(ColorScheme.Dark);
        await darkNarrativePage.GotoAsync(narrativeUrl);
        await Assertions.Expect(darkNarrativePage.GetByTestId("participant-lane")).ToBeVisibleAsync();
        await CaptureAsync(darkNarrativePage, "60-narrative-composer-dark.png");
        await darkNarrativePage.Context.CloseAsync();

        var narrowNarrativePage = await NewPageAsync(ColorScheme.Light, 620, 900);
        await narrowNarrativePage.GotoAsync(narrativeUrl);
        await Assertions.Expect(narrowNarrativePage.GetByRole(AriaRole.Heading, new() { Name = "Narrative readiness" })).ToBeVisibleAsync();
        await CaptureAsync(narrowNarrativePage, "61-narrative-composer-responsive.png");
        await narrowNarrativePage.Context.CloseAsync();

        await page.GetByLabel("Episode name").FillAsync("Create Project Definition");
        await page.GetByLabel("Initiating situation").FillAsync("An authorized modeler has an unmodeled project intention.");
        await page.GetByLabel("Completion criterion").FillAsync("The purpose-led project is persisted and reviewable at revision 1.");
        await page.GetByLabel("Scenario name").FillAsync("Authorized modeler creates project");
        await page.GetByLabel("Starting facts").FillAsync("The local development workspace is available\nThe modeler is authorized");
        await page.GetByLabel("Trigger").FillAsync("The modeler submits a name, purpose, intended outcome, and change reason.");
        await page.GetByLabel("Expected outcome").FillAsync("The project overview shows the accepted definition at revision 1.");
        await page.GetByLabel("Scene name").FillAsync("Capture project definition");
        await page.GetByLabel("Setting").FillAsync("The accessible Project Builder creation form.");
        await page.GetByLabel("Responsibility").FillAsync("Capture meaning and commit it through the project change-set pipeline.");
        await page.GetByLabel("Interaction name").FillAsync("Submit project definition");
        await page.GetByLabel("Initiator").SelectOptionAsync(new SelectOptionValue { Label = "Modeler" });
        await page.GetByLabel("Receiver").SelectOptionAsync(new SelectOptionValue { Label = "Project Builder" });
        await page.GetByLabel("Intent").FillAsync("Create a purpose-led project.");
        await page.GetByLabel("Step").FillAsync("Validate authorization and meaning, then atomically persist revision 1.");
        await page.GetByLabel("Observation").FillAsync("The modeler sees the project purpose, outcome, revision, and allowed next action.");
        await page.GetByLabel("Semantic results").FillAsync("Created\nInvalid\nDenied\nDuplicateName\nIdempotencyConflict");
        await page.GetByLabel("Change reason").FillAsync("Model the complete Create Project scenario.");
        await Assertions.Expect(page.GetByText("7/7", new() { Exact = true })).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByText("Modeler → Project Builder", new() { Exact = true })).ToBeVisibleAsync();
        await CaptureAsync(page, "11-complete-narrative-definition.png");
        await CaptureAsync(page, "62-narrative-composer-authored-flow.png");
        await page.Keyboard.PressAsync("Control+z");
        await Assertions.Expect(page.GetByLabel("Change reason")).ToHaveValueAsync("Define a complete scenario narrative.");
        await page.Keyboard.PressAsync("Control+y");
        await Assertions.Expect(page.GetByLabel("Change reason")).ToHaveValueAsync("Model the complete Create Project scenario.");
        await CaptureAsync(page, "90-narrative-studio-keyboard-redo.png");
        await page.ReloadAsync();
        await Assertions.Expect(page.GetByTestId("narrative-draft-recovered")).ToContainTextAsync("Narrative draft restored after refresh");
        await Assertions.Expect(page.GetByLabel("Episode name")).ToHaveValueAsync("Create Project Definition");
        await CaptureAsync(page, "91-narrative-studio-refresh-recovered.png");
        await page.GetByLabel("Change reason").FocusAsync();
        await page.Keyboard.PressAsync("Control+s");

        await ExpectHeadingAsync(page, "Create Project Definition defined");
        await Assertions.Expect(page.GetByText("Revision 5 committed")).ToBeVisibleAsync();
        await CaptureAsync(page, "12-narrative-committed.png");
        await CaptureAsync(page, "63-narrative-composer-committed.png");
        await page.GetByRole(AriaRole.Link, new() { Name = "Review narrative" }).ClickAsync();
        await Assertions.Expect(page.GetByText("Semantic model · Revision 5")).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Create Project Definition" })).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByText("Scenario: Authorized modeler creates project")).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByText("Intent: Create a purpose-led project.")).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByText("Observation: The modeler sees the project purpose, outcome, revision, and allowed next action.")).ToBeVisibleAsync();
        await CaptureAsync(page, "13-complete-narrative-review.png");

        await page.GetByLabel("State and logic").GetByRole(AriaRole.Link, new() { Name = "Define state and logic" }).ClickAsync();
        await ExpectHeadingAsync(page, "Define state and logic");
        await page.GetByLabel("State name").FillAsync("Project definition state");
        await page.GetByLabel("State category").SelectOptionAsync("Domain");
        await page.GetByLabel("State owner").SelectOptionAsync(new SelectOptionValue { Label = "Modeler" });
        await page.GetByLabel("State structure").FillAsync("DefinitionStatus\nRevision\nPurpose");
        await page.GetByLabel("State values").FillAsync("Unmodeled\nDefined");
        await page.GetByLabel("Fact name").FillAsync("Project purpose recorded");
        await page.GetByLabel("Fact value type").FillAsync("boolean");
        await page.GetByLabel("Fact authority").FillAsync("The Project aggregate owns accepted purpose truth.");
        await page.GetByLabel("Rule name").FillAsync("Project definition validity");
        await page.GetByLabel("Rule kind").SelectOptionAsync("Validation");
        await page.GetByLabel("Rule authority owner").SelectOptionAsync(new SelectOptionValue { Label = "Modeler" });
        await page.GetByLabel("Rule statement").FillAsync("Name, purpose, intended outcome, and change reason must be valid.");
        await page.GetByLabel("Invariant name").FillAsync("Project revision advances once");
        await page.GetByLabel("Invariant statement").FillAsync("An accepted creation advances the project to revision 1 exactly once.");
        await page.GetByLabel("Falsifying example").FillAsync("One accepted operation creates two projects or advances more than one revision.");
        await page.GetByLabel("Proof expectation").FillAsync("Transition example\nIdempotent retry property\nPostgreSQL concurrency test");
        await page.GetByLabel("Transition name").FillAsync("Create project definition");
        await page.GetByLabel("Source state predicate").FillAsync("No project definition exists for the accepted operation.");
        await page.GetByLabel("Trigger").FillAsync("An authorized create-project intent passes semantic validation.");
        await page.GetByLabel("Target state predicate").FillAsync("A purpose-led project exists at revision 1.");
        await page.GetByLabel("Semantic results").FillAsync("Created | Success | The project was durably created.\nInvalid | Invalid | Meaning was rejected without mutation.\nDenied | Denied | The actor lacked authority.\nConflict | Conflict | Current state was not overwritten.");
        await page.GetByLabel("Change reason").FillAsync("Model explicit project creation state and logic.");
        await CaptureAsync(page, "14-state-logic-definition.png");
        await page.Keyboard.PressAsync("Control+z");
        await Assertions.Expect(page.GetByLabel("Change reason")).ToHaveValueAsync("Define explicit state, logic, and semantic results.");
        await page.Keyboard.PressAsync("Control+y");
        await Assertions.Expect(page.GetByLabel("Change reason")).ToHaveValueAsync("Model explicit project creation state and logic.");
        await CaptureAsync(page, "92-state-studio-keyboard-redo.png");
        await page.ReloadAsync();
        await Assertions.Expect(page.GetByTestId("state-draft-recovered")).ToContainTextAsync("State draft restored after refresh");
        await Assertions.Expect(page.GetByLabel("State name")).ToHaveValueAsync("Project definition state");
        await CaptureAsync(page, "93-state-studio-refresh-recovered.png");
        await page.GetByLabel("Change reason").FocusAsync();
        await page.Keyboard.PressAsync("Control+s");

        await ExpectHeadingAsync(page, "Project definition state defined");
        await Assertions.Expect(page.GetByText("Revision 6 committed")).ToBeVisibleAsync();
        await CaptureAsync(page, "15-state-logic-committed.png");
        await page.GetByRole(AriaRole.Link, new() { Name = "Review state and logic" }).ClickAsync();
        await Assertions.Expect(page.GetByText("Semantic model · Revision 6")).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Project definition state (domain)" })).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByText("Invariant: Project revision advances once — An accepted creation advances the project to revision 1 exactly once.")).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByText("Semantic results: Created (success), Invalid (invalid), Denied (denied), Conflict (conflict)")).ToBeVisibleAsync();
        await CaptureAsync(page, "16-state-logic-review.png");

        await page.GetByLabel("Paths and recovery").GetByRole(AriaRole.Link, new() { Name = "Define branch and recovery" }).ClickAsync();
        await ExpectHeadingAsync(page, "Define a branch and recovery path");
        await page.GetByLabel("Branch name").FillAsync("Invalid project definition");
        await page.GetByLabel("Classification").SelectOptionAsync("Exceptional");
        await page.GetByLabel("Condition name *", new() { Exact = true }).FillAsync("Definition is invalid");
        await page.GetByLabel("Condition kind").SelectOptionAsync("Branch");
        await page.GetByLabel("Condition statement").FillAsync("One or more purpose-led project fields fail semantic validation.");
        await page.GetByLabel("Ordered branch segments").FillAsync("Validate submitted meaning\nReturn field-level findings\nPreserve entered values");
        await page.GetByLabel("State at branch termination").FillAsync("No project definition exists and the revision is unchanged.");
        await page.GetByLabel("Participant observation").FillAsync("The modeler sees an error summary with entered values preserved.");
        await page.GetByLabel("Effect name").FillAsync("Present validation findings");
        await page.GetByLabel("Effect kind").SelectOptionAsync("Observation");
        await page.GetByLabel("Intended effect").FillAsync("Present actionable findings without changing domain state.");
        await page.GetByLabel("Recovery path name").FillAsync("Correct and resubmit");
        await page.GetByLabel("Recovery strategy").SelectOptionAsync("CorrectAndRetry");
        await page.GetByLabel("Recovery condition name").FillAsync("Modeler chooses to correct");
        await page.GetByLabel("Recovery entry condition").FillAsync("The modeler retains authority and corrects the rejected meaning.");
        await page.GetByLabel("Ordered recovery segments").FillAsync("Correct invalid fields\nResubmit with a new operation identity");
        await page.GetByLabel("State at recovery termination").FillAsync("The corrected definition is eligible for the Create Project transition.");
        await page.GetByLabel("Recovery observation").FillAsync("The modeler can submit the corrected definition.");
        await page.GetByLabel("Retry policy").FillAsync("Retry only after correction; stop when the modeler cancels.");
        await page.GetByLabel("Idempotency analysis").FillAsync("A rejected operation never commits; corrected intent uses a new operation identity.");
        await page.GetByLabel("Exit condition").FillAsync("Exit when the project is created or the modeler cancels.");
        await page.GetByLabel("Reconciliation").FillAsync("None required because rejection produced no domain mutation.");
        await page.GetByLabel("Change reason").FillAsync("Model the invalid-definition branch and recovery.");
        await Assertions.Expect(page.GetByTestId("path-topology")).ToBeVisibleAsync();
        await CaptureAsync(page, "17-path-recovery-definition.png");
        await page.Keyboard.PressAsync("Control+z");
        await Assertions.Expect(page.GetByLabel("Change reason")).ToHaveValueAsync("Define an explicit branch and recovery path.");
        await page.Keyboard.PressAsync("Control+y");
        await Assertions.Expect(page.GetByLabel("Change reason")).ToHaveValueAsync("Model the invalid-definition branch and recovery.");
        await CaptureAsync(page, "84-path-studio-keyboard-redo.png");
        await page.ReloadAsync();
        await Assertions.Expect(page.GetByTestId("path-draft-recovered")).ToContainTextAsync("restored after refresh");
        await Assertions.Expect(page.GetByLabel("Branch name")).ToHaveValueAsync("Invalid project definition");
        await CaptureAsync(page, "85-path-studio-refresh-recovered.png");
        await page.Keyboard.PressAsync("Control+s");

        await ExpectHeadingAsync(page, "Invalid project definition defined");
        await Assertions.Expect(page.GetByText("Revision 7 committed")).ToBeVisibleAsync();
        await CaptureAsync(page, "18-path-recovery-committed.png");
        var pathUrl = page.Url;
        var darkPathPage = await NewPageAsync(ColorScheme.Dark);
        await darkPathPage.GotoAsync(pathUrl);
        await Assertions.Expect(darkPathPage.GetByTestId("path-topology")).ToBeVisibleAsync();
        await CaptureAsync(darkPathPage, "86-path-studio-dark.png");
        await darkPathPage.Context.CloseAsync();
        var narrowPathPage = await NewPageAsync(ColorScheme.Light, 620, 900);
        await narrowPathPage.GotoAsync(pathUrl);
        await Assertions.Expect(narrowPathPage.GetByRole(AriaRole.Heading, new() { Name = "Branch packet" })).ToBeVisibleAsync();
        await CaptureAsync(narrowPathPage, "87-path-studio-responsive.png");
        await narrowPathPage.Context.CloseAsync();
        await page.GetByRole(AriaRole.Link, new() { Name = "Review paths" }).ClickAsync();
        await Assertions.Expect(page.GetByText("Semantic model · Revision 7")).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Invalid project definition (exceptional)" })).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByText("Terminal result: Invalid (invalid)")).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Recovery: Correct and resubmit (correctAndRetry)" })).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByText("Idempotency: A rejected operation never commits; corrected intent uses a new operation identity.")).ToBeVisibleAsync();
        await CaptureAsync(page, "19-path-recovery-review.png");
        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Revision 7 · path.defined" })).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByText("Author: local-modeler · Operations: 5")).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByText("element.added · Added path 'Invalid project definition'.")).ToBeVisibleAsync();
        await CaptureAsync(page, "21-typed-change-set-history.png");
        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Plan proof for the material invariant" })).ToBeVisibleAsync();
        await CaptureAsync(page, "36-outcome-cockpit-current-profile.png");
        await ExpectHeadingAsync(page, "Semantic Explorer");
        var completeProfileSearch = page.GetByTestId("semantic-search");
        await completeProfileSearch.FillAsync("Invalid project definition");
        await Assertions.Expect(page.GetByRole(AriaRole.Treeitem).Filter(new() { HasText = "Invalid project definition" })).ToBeVisibleAsync();
        await completeProfileSearch.FillAsync("Project definition state");
        await Assertions.Expect(page.GetByRole(AriaRole.Treeitem).Filter(new() { HasText = "Project definition state" })).ToBeVisibleAsync();
        await completeProfileSearch.FillAsync(string.Empty);
        await CaptureAsync(page, "42-semantic-explorer-complete-profile.png");
    }

    [Test]
    [Property("ModelClaim", "0198ad00-0000-7000-8000-000000000221")]
    public async Task Given_a_pos_state_model_when_logic_is_authored_then_the_transition_and_result_matrix_are_reviewable_without_raw_json()
    {
        var page = await NewPageAsync();
        await page.GotoAsync($"{baseUrl}/projects/new");
        await page.GetByLabel("Project name").FillAsync("Point of Sale state lens");
        await page.GetByLabel("Purpose").FillAsync("Model transaction state, rules, invariants, and observable results.");
        await page.GetByLabel("Intended outcome").FillAsync("A reviewer can inspect how adding a product changes transaction state.");
        await page.GetByLabel("Change-set reason").FillAsync("Start the C07 reference state model.");
        await page.GetByRole(AriaRole.Button, new() { Name = "Create project" }).ClickAsync();
        await page.GetByRole(AriaRole.Link, new() { Name = "Open project overview" }).ClickAsync();

        await page.GetByRole(AriaRole.Link, new() { Name = "Add actor" }).ClickAsync();
        await page.GetByLabel("Actor name").FillAsync("Clerk");
        await page.GetByLabel("Contextual role").FillAsync("Initiates product entry for an open transaction.");
        await page.GetByLabel("Goals").FillAsync("Add a recognized product");
        await page.GetByLabel("Responsibilities").FillAsync("Provide product identity and observe the result");
        await page.GetByLabel("Change reason").FillAsync("Add the state authority participant.");
        await page.GetByRole(AriaRole.Button, new() { Name = "Commit actor" }).ClickAsync();
        await page.GetByRole(AriaRole.Link, new() { Name = "Project overview" }).ClickAsync();
        await page.GetByLabel("State and logic").GetByRole(AriaRole.Link, new() { Name = "Define state and logic" }).ClickAsync();

        await ExpectHeadingAsync(page, "Define state and logic");
        await Assertions.Expect(page.GetByTestId("state-logic-studio")).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Complementary, new() { Name = "State definition catalog" })).ToBeVisibleAsync();
        await CaptureAsync(page, "64-state-studio-light-clean.png");

        var stateUrl = page.Url;
        var darkPage = await NewPageAsync(ColorScheme.Dark);
        await darkPage.GotoAsync(stateUrl);
        await Assertions.Expect(darkPage.GetByTestId("transition-flow")).ToBeVisibleAsync();
        await CaptureAsync(darkPage, "65-state-studio-dark.png");
        await darkPage.Context.CloseAsync();

        var narrowPage = await NewPageAsync(ColorScheme.Light, 620, 900);
        await narrowPage.GotoAsync(stateUrl);
        await Assertions.Expect(narrowPage.GetByRole(AriaRole.Heading, new() { Name = "Definition readiness" })).ToBeVisibleAsync();
        await CaptureAsync(narrowPage, "66-state-studio-responsive.png");
        await narrowPage.Context.CloseAsync();

        await page.GetByLabel("State name").FillAsync("Transaction");
        await page.GetByLabel("State category").SelectOptionAsync("Domain");
        await page.GetByLabel("State owner").SelectOptionAsync(new SelectOptionValue { Label = "Clerk" });
        await page.GetByLabel("State structure").FillAsync("Status\nLines\nPendingProduct");
        await page.GetByLabel("State values").FillAsync("Open\nOpenWithLine\nOpenPending\nCompleted");
        await page.GetByLabel("Fact name").FillAsync("Transaction status");
        await page.GetByLabel("Fact value type").FillAsync("TransactionStatus");
        await page.GetByLabel("Fact authority").FillAsync("The Transaction aggregate owns accepted transaction status.");
        await page.GetByLabel("Fact mutability").SelectOptionAsync("Transitioned");
        await page.GetByLabel("Rule name").FillAsync("Product may be added");
        await page.GetByLabel("Rule kind").SelectOptionAsync("Eligibility");
        await page.GetByLabel("Rule authority owner").SelectOptionAsync(new SelectOptionValue { Label = "Clerk" });
        await page.GetByLabel("Rule statement").FillAsync("A product may be added only while the transaction is open.");
        await page.GetByLabel("Invariant name").FillAsync("Completed transaction rejects lines");
        await page.GetByLabel("Invariant statement").FillAsync("A completed transaction cannot accept another line.");
        await page.GetByLabel("Falsifying example").FillAsync("A line is added after the transaction status becomes Completed.");
        await page.GetByLabel("Proof expectation").FillAsync("Transition example\nCompleted-state rejection property\nPostgreSQL persistence test");
        await page.GetByLabel("Transition name").FillAsync("Add product");
        await page.GetByLabel("Source state predicate").FillAsync("Transaction status is Open.");
        await page.GetByLabel("Trigger").FillAsync("The clerk submits a product identifier.");
        await page.GetByLabel("Target state predicate").FillAsync("Transaction remains open with an added, pending, or unchanged line set.");
        await page.GetByLabel("Semantic results").FillAsync("Added | Success | The recognized product line is added.\nNotFound | Invalid | The product is unknown and state is unchanged.\nUnavailable | Unavailable | Resolution is unavailable and the product remains pending.\nClosed | Conflict | A completed transaction rejects the request.");
        await page.GetByLabel("Change reason").FillAsync("Model the reference POS Add Product state transition.");

        await Assertions.Expect(page.GetByText("5/5", new() { Exact = true })).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByTestId("result-path-matrix").GetByText("NotFound", new() { Exact = true })).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByTestId("result-path-matrix").GetByText("Unmodeled · next slice", new() { Exact = true }).First).ToBeVisibleAsync();
        await CaptureAsync(page, "67-state-studio-pos-authored.png");

        await page.GetByRole(AriaRole.Button, new() { Name = "Commit state and logic" }).ClickAsync();
        await Assertions.Expect(page.GetByTestId("state-logic-committed")).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Transaction defined" })).ToBeVisibleAsync();
        await CaptureAsync(page, "68-state-studio-pos-committed.png");
    }

    [Test]
    [Property("ModelClaim", "0198ad00-0000-7000-8000-000000000222")]
    public async Task Given_an_intentionally_incomplete_model_when_problems_are_reviewed_then_the_catalog_explains_and_navigates_exactly()
    {
        var page = await NewPageAsync();
        await page.GotoAsync($"{baseUrl}/projects/new");
        await page.GetByLabel("Project name").FillAsync("Incomplete review model");
        await page.GetByLabel("Purpose").FillAsync("Prove deterministic finding review and safe repair navigation.");
        await page.GetByLabel("Intended outcome").FillAsync("A reviewer can understand and navigate every material gap.");
        await page.GetByLabel("Change-set reason").FillAsync("Create an intentionally incomplete C08 model.");
        await page.GetByRole(AriaRole.Button, new() { Name = "Create project" }).ClickAsync();
        await page.GetByRole(AriaRole.Link, new() { Name = "Open project overview" }).ClickAsync();
        await page.GetByRole(AriaRole.Link, new() { Name = "Add actor" }).ClickAsync();
        await page.GetByLabel("Actor name").FillAsync("Reviewer");
        await page.GetByLabel("Contextual role").FillAsync("Reviews model completeness and evidence requirements.");
        await page.GetByLabel("Goals").FillAsync("Locate material model gaps");
        await page.GetByLabel("Responsibilities").FillAsync("Review rule explanations and choose safe repairs");
        await page.GetByLabel("Change reason").FillAsync("Add an explicit finding owner.");
        await page.GetByRole(AriaRole.Button, new() { Name = "Commit actor" }).ClickAsync();
        await page.GetByRole(AriaRole.Link, new() { Name = "Project overview" }).ClickAsync();

        await page.GetByRole(AriaRole.Link, new() { Name = "Review problems" }).ClickAsync();
        using (var invalidProfile = await api!.GetAsync($"/api/v1/projects/{page.Url.Split('/')[4]}/findings?profile=release-ready"))
        {
            Assert.That(invalidProfile.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(await invalidProfile.Content.ReadAsStringAsync(), Does.Contain("purpose-profile.invalid"));
        }
        await ExpectHeadingAsync(page, "Purpose and gap map");
        await Assertions.Expect(page.GetByTestId("problems-workbench")).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByTestId("purpose-profile-deck")).ToContainTextAsync("Discovery");
        await Assertions.Expect(page.GetByTestId("gap-map")).ToContainTextAsync("Discovery gap map");
        await Assertions.Expect(page.GetByText("Profile overlay · revision 2", new() { Exact = true })).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByText("3", new() { Exact = true }).First).ToBeVisibleAsync();
        var findingList = page.GetByRole(AriaRole.List, new() { Name = "Model findings" });
        await Assertions.Expect(findingList.GetByText("PB-OUTCOME-001", new() { Exact = true })).ToBeVisibleAsync();
        await Assertions.Expect(findingList.GetByText("PB-NARR-001", new() { Exact = true })).ToBeVisibleAsync();
        await Assertions.Expect(findingList.GetByText("PB-STATE-011", new() { Exact = true })).ToBeVisibleAsync();
        await CaptureAsync(page, "69-problems-workbench-incomplete.png");
        await CaptureAsync(page, "94-purpose-gap-map-discovery.png");

        await page.GetByRole(AriaRole.Button, new() { Name = "Implementation Ready" }).ClickAsync();
        await Assertions.Expect(page.GetByTestId("gap-map")).ToContainTextAsync("Implementation Ready gap map");
        await Assertions.Expect(page.GetByText("4/6", new() { Exact = true })).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByText("Profile overlay · revision 2", new() { Exact = true })).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByText("Implementation Ready requires explicit state-changing semantics.")).ToBeVisibleAsync();
        await Assertions.Expect(findingList.GetByText("PB-STATE-011", new() { Exact = true })).ToBeVisibleAsync();
        await Assertions.Expect(findingList.GetByText("Error", new() { Exact = true }).Last).ToBeVisibleAsync();
        await CaptureAsync(page, "95-purpose-gap-map-implementation-ready.png");

        var profileUrl = page.Url;
        var profileDarkPage = await NewPageAsync(ColorScheme.Dark);
        await profileDarkPage.GotoAsync(profileUrl);
        await Assertions.Expect(profileDarkPage.GetByTestId("gap-map")).ToContainTextAsync("Implementation Ready gap map");
        await CaptureAsync(profileDarkPage, "96-purpose-gap-map-dark.png");
        await profileDarkPage.Context.CloseAsync();
        var profileNarrowPage = await NewPageAsync(ColorScheme.Light, 620, 900);
        await profileNarrowPage.GotoAsync(profileUrl);
        await Assertions.Expect(profileNarrowPage.GetByTestId("purpose-profile-deck")).ToBeVisibleAsync();
        await CaptureAsync(profileNarrowPage, "97-purpose-gap-map-responsive.png");
        await profileNarrowPage.Context.CloseAsync();

        await page.GetByLabel("Search findings").FillAsync("PB-STATE-011");
        await Assertions.Expect(findingList.GetByRole(AriaRole.Listitem)).ToHaveCountAsync(1);
        await CaptureAsync(page, "70-problems-workbench-filtered.png");
        await findingList.GetByRole(AriaRole.Listitem).ClickAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Behavior has no explicit state and logic model" })).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByText("Deterministically evaluated against model revision 2.")).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByText("Reviewer", new() { Exact = true }).Last).ToBeVisibleAsync();
        await CaptureAsync(page, "71-problems-workbench-rule-detail.png");

        await Assertions.Expect(page.GetByTestId("governance-studio")).ToBeVisibleAsync();
        await page.GetByLabel("Accountable authority").SelectOptionAsync(new SelectOptionValue { Label = "Reviewer · Reviews model completeness and evidence requirements." });
        await page.GetByLabel("Rationale").FillAsync("State semantics are deferred to the next bounded modeling slice.");
        await page.GetByLabel("Material consequence").FillAsync("Implementation remains blocked until facts, rules, invariants, and results are explicit.");
        await page.GetByLabel("Review / expiration").FillAsync("2026-09-30");
        await page.GetByLabel("Target milestone").FillAsync("C11");
        await page.GetByLabel("Audit reason").FillAsync("Record an accountable deferral without claiming semantic repair.");
        await CaptureAsync(page, "98-gap-governance-staged.png");
        await page.GetByRole(AriaRole.Button, new() { Name = "Commit disposition at revision 2" }).ClickAsync();
        await Assertions.Expect(page.GetByTestId("governance-receipt")).ToContainTextAsync("Deferred");
        await Assertions.Expect(page.GetByTestId("governance-receipt")).ToContainTextAsync("Reviewer");
        await Assertions.Expect(page.GetByTestId("governance-receipt")).ToContainTextAsync("2026-09-30");
        await Assertions.Expect(findingList.GetByText("PB-STATE-011", new() { Exact = true })).ToBeVisibleAsync();
        await CaptureAsync(page, "99-gap-governance-committed.png");

        var governedUrl = page.Url;
        var governedDarkPage = await NewPageAsync(ColorScheme.Dark);
        await governedDarkPage.GotoAsync(governedUrl);
        await governedDarkPage.GetByLabel("Search findings").FillAsync("PB-STATE-011");
        await governedDarkPage.GetByRole(AriaRole.List, new() { Name = "Model findings" }).GetByRole(AriaRole.Listitem).ClickAsync();
        await Assertions.Expect(governedDarkPage.GetByTestId("governance-receipt")).ToBeVisibleAsync();
        await CaptureAsync(governedDarkPage, "100-gap-governance-dark.png");
        await governedDarkPage.Context.CloseAsync();
        var governedNarrowPage = await NewPageAsync(ColorScheme.Light, 620, 900);
        await governedNarrowPage.GotoAsync(governedUrl);
        await governedNarrowPage.GetByLabel("Search findings").FillAsync("PB-STATE-011");
        await governedNarrowPage.GetByRole(AriaRole.List, new() { Name = "Model findings" }).GetByRole(AriaRole.Listitem).ClickAsync();
        await Assertions.Expect(governedNarrowPage.GetByTestId("governance-receipt")).ToBeVisibleAsync();
        await CaptureAsync(governedNarrowPage, "101-gap-governance-responsive.png");
        await governedNarrowPage.Context.CloseAsync();

        await page.GetByRole(AriaRole.Button, new() { Name = "Evidence 1" }).ClickAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Evidence requirements" })).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByText("Artifact linkage is not exposed")).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByText("Unknown", new() { Exact = true }).Last).ToBeVisibleAsync();
        await CaptureAsync(page, "72-evidence-workbench-unknown.png");

        var evidenceUrl = page.Url;
        var darkPage = await NewPageAsync(ColorScheme.Dark);
        await darkPage.GotoAsync(evidenceUrl);
        await Assertions.Expect(darkPage.GetByRole(AriaRole.Heading, new() { Name = "Evidence requirements" })).ToBeVisibleAsync();
        await CaptureAsync(darkPage, "73-problems-evidence-dark.png");
        await darkPage.Context.CloseAsync();

        var narrowPage = await NewPageAsync(ColorScheme.Light, 620, 900);
        await narrowPage.GotoAsync(evidenceUrl.Replace("view=evidence", "view=problems"));
        await Assertions.Expect(narrowPage.GetByText("PB-STATE-011", new() { Exact = true })).ToBeVisibleAsync();
        await CaptureAsync(narrowPage, "74-problems-workbench-responsive.png");
        await narrowPage.Context.CloseAsync();

        await page.GotoAsync(evidenceUrl.Replace("view=evidence", "view=problems"));
        await page.GetByLabel("Search findings").FillAsync("PB-STATE-011");
        await page.GetByRole(AriaRole.List, new() { Name = "Model findings" }).GetByRole(AriaRole.Listitem).ClickAsync();
        await page.GetByRole(AriaRole.Link, new() { Name = "Define state and logic" }).ClickAsync();
        await ExpectHeadingAsync(page, "Define state and logic");
        await CaptureAsync(page, "75-problems-exact-repair-navigation.png");
    }

    [Test]
    [Property("ModelClaim", "0198ad00-0000-7000-8000-000000000228")]
    public async Task Given_current_model_facts_when_guidance_is_opened_then_applicable_prompts_explain_their_trigger_and_changes()
    {
        var page = await NewPageAsync();
        await page.GotoAsync($"{baseUrl}/projects/new");
        await page.GetByLabel("Project name").FillAsync("Guidance registry journey");
        await page.GetByLabel("Purpose").FillAsync("Prove deterministic contextual guidance without inventing answers.");
        await page.GetByLabel("Intended outcome").FillAsync("A contributor can inspect why the next modeling question applies.");
        await page.GetByLabel("Change-set reason").FillAsync("Create the D01 guidance evidence project.");
        await page.GetByRole(AriaRole.Button, new() { Name = "Create project" }).ClickAsync();
        await page.GetByRole(AriaRole.Link, new() { Name = "Open project overview" }).ClickAsync();
        await page.GetByRole(AriaRole.Link, new() { Name = "Open Guide Rail" }).ClickAsync();

        await Assertions.Expect(page.GetByTestId("guidance-map")).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Model the next truth" })).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByText("builtin/1", new() { Exact = true })).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByText("guide.frame.observable-outcome", new() { Exact = true })).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Who receives value, and what would they observe when this works?" })).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByText("D01 remains inspectable; D02 adds recoverable local orchestration.")).ToBeVisibleAsync();
        await CaptureAsync(page, "102-guidance-map-outcome-prompt.png");

        await page.GetByRole(AriaRole.Listitem).Filter(new() { HasText = "Participants" }).ClickAsync();
        await page.GetByText("guide.participants.accountable-actor", new() { Exact = true }).ClickAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Who can act, decide, or authoritatively answer questions in this situation?" })).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByText("Unknown", new() { Exact = true }).Last).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByText("Deferred", new() { Exact = true }).Last).ToBeVisibleAsync();
        await CaptureAsync(page, "103-guidance-map-participant-prompt.png");

        var guidanceUrl = page.Url;
        var darkPage = await NewPageAsync(ColorScheme.Dark);
        await darkPage.GotoAsync(guidanceUrl);
        await Assertions.Expect(darkPage.GetByTestId("guidance-map")).ToBeVisibleAsync();
        await CaptureAsync(darkPage, "104-guidance-map-dark.png");
        await darkPage.Context.CloseAsync();
        var narrowPage = await NewPageAsync(ColorScheme.Light, 620, 900);
        await narrowPage.GotoAsync(guidanceUrl);
        await Assertions.Expect(narrowPage.GetByRole(AriaRole.Heading, new() { Name = "Definition guidance topology" })).ToBeVisibleAsync();
        await CaptureAsync(narrowPage, "105-guidance-map-responsive.png");
        await narrowPage.Context.CloseAsync();
    }

    [Test]
    [Property("ModelClaim", "0198ad00-0000-7000-8000-000000000229")]
    public async Task Given_a_contextual_guide_session_when_the_rail_is_navigated_closed_and_reopened_then_local_progress_and_focus_are_preserved()
    {
        var page = await NewPageAsync();
        await page.GotoAsync($"{baseUrl}/projects/new");
        await page.GetByLabel("Project name").FillAsync("Guide Rail session journey");
        await page.GetByLabel("Purpose").FillAsync("Prove focus-safe recoverable modeling guidance.");
        await page.GetByLabel("Intended outcome").FillAsync("A contributor can leave guidance and return without losing place.");
        await page.GetByLabel("Change-set reason").FillAsync("Create the D02 Guide Rail evidence project.");
        await page.GetByRole(AriaRole.Button, new() { Name = "Create project" }).ClickAsync();
        await page.GetByRole(AriaRole.Link, new() { Name = "Open project overview" }).ClickAsync();
        var revisionBefore = await page.GetByText("Semantic model · Revision 1", new() { Exact = true }).TextContentAsync();
        await page.GetByRole(AriaRole.Link, new() { Name = "Open Guide Rail" }).ClickAsync();

        var drawer = page.GetByTestId("guide-contextual-drawer");
        await Assertions.Expect(drawer).ToBeVisibleAsync();
        await drawer.Locator(".guide-answer-grid button").Filter(new() { HasText = "Assumed" }).ClickAsync();
        await Assertions.Expect(drawer.GetByRole(AriaRole.Heading, new() { Name = "Assumed" })).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByText("1 / 2 considered", new() { Exact = true })).ToBeVisibleAsync();
        await CaptureAsync(page, "106-guide-rail-local-answer.png");

        var participantStage = page.Locator(".guidance-stage").Filter(new() { HasText = "Participants" });
        await participantStage.ClickAsync();
        await Assertions.Expect(drawer.GetByRole(AriaRole.Heading, new() { Name = "Who can act, decide, or authoritatively answer questions in this situation?" })).ToBeVisibleAsync();
        await participantStage.FocusAsync();
        await page.Keyboard.PressAsync("Control+Shift+g");
        await Assertions.Expect(drawer).ToBeHiddenAsync();
        await Assertions.Expect(participantStage).ToBeFocusedAsync();
        await CaptureAsync(page, "107-guide-rail-closed-workspace.png");

        await page.Keyboard.PressAsync("Control+Shift+g");
        await Assertions.Expect(drawer).ToBeVisibleAsync();
        await Assertions.Expect(participantStage).ToBeFocusedAsync();
        await drawer.Locator(".guide-answer-grid button").Filter(new() { HasText = "Deferred" }).ClickAsync();
        await Assertions.Expect(page.GetByText("2 / 2 considered", new() { Exact = true })).ToBeVisibleAsync();
        await CaptureAsync(page, "108-guide-rail-reopened-progress.png");

        await page.ReloadAsync();
        await Assertions.Expect(drawer).ToBeVisibleAsync();
        await Assertions.Expect(drawer.GetByRole(AriaRole.Heading, new() { Name = "Who can act, decide, or authoritatively answer questions in this situation?" })).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByText("2 / 2 considered", new() { Exact = true })).ToBeVisibleAsync();
        await Assertions.Expect(drawer.GetByRole(AriaRole.Heading, new() { Name = "Deferred" })).ToBeVisibleAsync();
        await page.Keyboard.PressAsync("Escape");
        await Assertions.Expect(drawer).ToBeHiddenAsync();

        await page.GetByRole(AriaRole.Button, new() { Name = "Reopen Guide Rail Ctrl Shift G" }).ClickAsync();
        await Assertions.Expect(drawer).ToBeVisibleAsync();
        var darkPage = await NewPageAsync(ColorScheme.Dark);
        await darkPage.GotoAsync(page.Url);
        await Assertions.Expect(darkPage.GetByTestId("guide-contextual-drawer")).ToBeVisibleAsync();
        await CaptureAsync(darkPage, "109-guide-rail-dark.png");
        await darkPage.Context.CloseAsync();

        var narrowPage = await NewPageAsync(ColorScheme.Light, 620, 900);
        await narrowPage.GotoAsync(page.Url);
        await Assertions.Expect(narrowPage.GetByRole(AriaRole.Button, new() { Name = "Close Guide Rail Ctrl Shift G" })).ToBeVisibleAsync();
        await CaptureAsync(narrowPage, "110-guide-rail-responsive.png");
        await narrowPage.Context.CloseAsync();

        await page.EvaluateAsync("localStorage.setItem(Object.keys(localStorage).find(key => key.startsWith('projectbuilder:guidance:v1:')), '{invalid-json')");
        await page.ReloadAsync();
        await Assertions.Expect(page.GetByTestId("guide-contextual-drawer")).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByText("0 / 2 considered", new() { Exact = true })).ToBeVisibleAsync();
        await page.GetByRole(AriaRole.Link, new() { Name = "Project overview" }).ClickAsync();
        await Assertions.Expect(page.GetByText(revisionBefore!, new() { Exact = true })).ToBeVisibleAsync();
    }

    [Test]
    [Property("ModelClaim", "0198ad00-0000-7000-8000-000000000230")]
    public async Task Given_a_blank_project_when_a_novice_uses_plain_language_guidance_then_an_actor_and_beneficiary_outcome_are_committed_as_typed_operations()
    {
        var page = await NewPageAsync();
        await page.GotoAsync($"{baseUrl}/projects/new");
        await page.GetByLabel("Project name").FillAsync("Guided framing journey");
        await page.GetByLabel("Purpose").FillAsync("Help a neighborhood pantry coordinate food collection.");
        await page.GetByLabel("Intended outcome").FillAsync("A volunteer can tell what collection work is ready.");
        await page.GetByLabel("Change-set reason").FillAsync("Create the D03 novice framing evidence project.");
        await page.GetByRole(AriaRole.Button, new() { Name = "Create project" }).ClickAsync();
        await page.GetByRole(AriaRole.Link, new() { Name = "Open project overview" }).ClickAsync();
        await page.GetByRole(AriaRole.Link, new() { Name = "Open Guide Rail" }).ClickAsync();

        var drawer = page.GetByTestId("guide-contextual-drawer");
        await page.Locator(".guidance-stage").Filter(new() { HasText = "Participants" }).ClickAsync();
        await drawer.Locator(".guide-answer-grid button").Filter(new() { HasText = "Author definition" }).ClickAsync();
        var guided = page.GetByTestId("guided-framing-studio");
        await Assertions.Expect(guided.GetByRole(AriaRole.Heading, new() { Name = "Describe one accountable participant" })).ToBeVisibleAsync();
        await guided.GetByLabel("Who or what is involved?").FillAsync("Pantry volunteer");
        await guided.GetByLabel("What part do they play here?").FillAsync("Coordinates incoming food collection work for the neighborhood pantry.");
        await guided.GetByLabel("What are they trying to achieve?").FillAsync("Know which collection work is ready");
        await guided.GetByLabel("What work do they own?").FillAsync("Review collection readiness\nCoordinate pickup");
        await guided.GetByLabel("What can they decide or approve?").FillAsync("Can assign a ready collection to a volunteer");
        await guided.GetByLabel("What limits or constrains them?").FillAsync("Cannot promise collection before donor confirmation");

        await drawer.GetByRole(AriaRole.Button, new() { Name = "Close Guide Rail and return focus" }).ClickAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Reopen Guide Rail Ctrl Shift G" }).ClickAsync();
        await Assertions.Expect(page.GetByTestId("guided-draft-recovered")).ToContainTextAsync("Your guided answer survived closing the rail");
        await Assertions.Expect(guided.GetByLabel("Who or what is involved?")).ToHaveValueAsync("Pantry volunteer");
        await page.EvaluateAsync("""
            () => {
                const drawer = document.querySelector('#guide-contextual-drawer');
                const target = drawer?.querySelector('[data-testid="guided-draft-recovered"]');
                if (drawer && target) {
                    drawer.scrollTop += target.getBoundingClientRect().top - drawer.getBoundingClientRect().top - 120;
                }
            }
            """);
        await CaptureViewportAsync(page, "111-guided-participant-recovered.png");

        await guided.GetByRole(AriaRole.Button, new() { Name = "Finish participant step" }).ClickAsync();
        var result = page.GetByTestId("guided-commit-result");
        await Assertions.Expect(result).ToContainTextAsync("Revision 2 committed");
        await Assertions.Expect(result).ToContainTextAsync("actor.added");
        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Who receives value, and what would they observe when this works?" })).ToBeVisibleAsync();
        await CaptureAsync(page, "112-guided-participant-committed.png");

        await drawer.Locator(".guide-answer-grid button").Filter(new() { HasText = "Author definition" }).ClickAsync();
        await Assertions.Expect(guided.GetByRole(AriaRole.Heading, new() { Name = "Describe the value they receive" })).ToBeVisibleAsync();
        await Assertions.Expect(guided.GetByRole(AriaRole.Group, new() { Name = "Who receives the value? Required" })).ToContainTextAsync("Pantry volunteer");
        await guided.GetByLabel("Name this changed condition").FillAsync("Collection work is ready");
        await guided.GetByLabel("What becomes possible or true?").FillAsync("The pantry volunteer can identify collection work that is ready to assign.");
        await guided.GetByLabel("What would show that it worked?").FillAsync("Ready work names the confirmed donor\nReady work can be assigned without another investigation");
        await Assertions.Expect(guided.GetByLabel("Outcome relationship and operations")).ToContainTextAsync("benefits from");
        await page.EvaluateAsync("""
            () => {
                const drawer = document.querySelector('#guide-contextual-drawer');
                const target = drawer?.querySelector('[aria-label="Outcome relationship and operations"]');
                if (drawer && target) {
                    drawer.scrollTop += target.getBoundingClientRect().top - drawer.getBoundingClientRect().top - 160;
                }
            }
            """);
        await CaptureViewportAsync(page, "113-guided-outcome-relation.png");

        var narrowPage = await page.Context.NewPageAsync();
        await narrowPage.SetViewportSizeAsync(620, 900);
        await narrowPage.GotoAsync(page.Url);
        await Assertions.Expect(narrowPage.GetByTestId("guided-draft-recovered")).ToBeVisibleAsync();
        await Assertions.Expect(narrowPage.GetByLabel("Name this changed condition")).ToHaveValueAsync("Collection work is ready");
        await CaptureAsync(narrowPage, "114-guided-framing-responsive.png");
        await narrowPage.CloseAsync();

        await guided.GetByRole(AriaRole.Button, new() { Name = "Finish outcome step" }).ClickAsync();
        await Assertions.Expect(result).ToContainTextAsync("Revision 3 committed");
        await Assertions.Expect(result).ToContainTextAsync("outcome.added + relation.added");
        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Describe one end-to-end situation in which a participant obtains the outcome." })).ToBeVisibleAsync();
        await Assertions.Expect(page.Locator(".guidance-stage").Filter(new() { HasText = "Frame" })).ToContainTextAsync("Established");
        await Assertions.Expect(page.Locator(".guidance-stage").Filter(new() { HasText = "Participants" })).ToContainTextAsync("Established");

        var darkPage = await NewPageAsync(ColorScheme.Dark);
        await darkPage.GotoAsync(page.Url);
        await Assertions.Expect(darkPage.GetByRole(AriaRole.Heading, new() { Name = "Describe one end-to-end situation in which a participant obtains the outcome." })).ToBeVisibleAsync();
        await CaptureAsync(darkPage, "115-guided-framing-complete-dark.png");
        await darkPage.Context.CloseAsync();

        await result.GetByRole(AriaRole.Link, new() { Name = "Inspect operations" }).ClickAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Name = "r2 r3", Exact = true })).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByText("relation.added · benefitsFrom", new() { Exact = true })).ToBeVisibleAsync();
        await CaptureAsync(page, "116-guided-framing-history.png");
    }

    [Test]
    [Property("ModelClaim", "0198ad00-0000-7000-8000-000000000231")]
    public async Task Given_a_new_pos_project_when_a_novice_guides_one_item_scan_then_the_complete_ordered_scenario_is_committed_and_reviewable()
    {
        using var createRequest = CreateApiRequest(
            "0198ad00-0000-7000-8000-000000000700",
            "0198ad00-0000-7000-8000-000000000731",
            new CreateProjectRequest("Guided item scan", "Model one ordinary staffed sale before selecting implementation structure.",
                "A clerk can add one recognized unrestricted item to the current sale.", "Create the blank D04 POS evidence project."));
        using var createResponse = await api!.SendAsync(createRequest);
        var project = await createResponse.Content.ReadFromJsonAsync<ProjectResponse>();
        Assert.That(createResponse.StatusCode, Is.EqualTo(HttpStatusCode.Created));

        using var clerkRequest = CreateEditApiRequest($"/api/v1/projects/{project!.Id}/actors", "1",
            "0198ad00-0000-7000-8000-000000000732",
            new AddActorRequest("Clerk", "humanRole", "Operates a staffed point of sale.", "Complete an accurate sale", "Scan merchandise and present results", "May initiate item entry", "Cannot invent product identity or price", "Establish the initiating participant.", "known"));
        using var clerkResponse = await api.SendAsync(clerkRequest);
        var clerk = await clerkResponse.Content.ReadFromJsonAsync<AddActorResponse>();
        Assert.That(clerkResponse.StatusCode, Is.EqualTo(HttpStatusCode.Created));

        using var catalogRequest = CreateEditApiRequest($"/api/v1/projects/{project.Id}/actors", "2",
            "0198ad00-0000-7000-8000-000000000733",
            new AddActorRequest("Product catalog", "systemRole", "Answers product identity and restriction questions.", "Provide authoritative product facts", "Resolve scanned identifiers", "Authoritative for product recognition", "Does not own the current sale", "Establish the responding participant.", "known"));
        using var catalogResponse = await api.SendAsync(catalogRequest);
        var catalog = await catalogResponse.Content.ReadFromJsonAsync<AddActorResponse>();
        Assert.That(catalogResponse.StatusCode, Is.EqualTo(HttpStatusCode.Created));

        using var outcomeRequest = CreateEditApiRequest($"/api/v1/projects/{project.Id}/outcomes", "3",
            "0198ad00-0000-7000-8000-000000000734",
            new AddOutcomeRequest("Recognized item joins the sale", "The clerk can see one recognized unrestricted item in the current sale.",
                "The item description is visible\nThe effective selling price is visible\nThe running total includes the item", clerk!.Actor.Id,
                "Establish the desired item-scan outcome.", "known"));
        using var outcomeResponse = await api.SendAsync(outcomeRequest);
        Assert.That(outcomeResponse.StatusCode, Is.EqualTo(HttpStatusCode.Created));

        var page = await NewPageAsync();
        await page.GotoAsync($"{baseUrl}/projects/{project.Id}/guide");
        var drawer = page.GetByTestId("guide-contextual-drawer");
        await Assertions.Expect(drawer.GetByRole(AriaRole.Heading, new() { Name = "Describe one end-to-end situation in which a participant obtains the outcome." })).ToBeVisibleAsync();
        await drawer.Locator(".guide-answer-grid button").Filter(new() { HasText = "Author definition" }).ClickAsync();

        var guided = page.GetByTestId("guided-scenario-studio");
        await Assertions.Expect(guided.GetByRole(AriaRole.Heading, new() { Name = "Tell one complete, ordinary story" })).ToBeVisibleAsync();
        await Assertions.Expect(guided.GetByTestId("guided-flowboard")).ToContainTextAsync("0/7 nodes");
        await page.EvaluateAsync("""
            () => {
                const drawer = document.querySelector('#guide-contextual-drawer');
                const target = drawer?.querySelector('[data-testid="guided-scenario-studio"]');
                if (drawer && target) drawer.scrollTop += target.getBoundingClientRect().top - drawer.getBoundingClientRect().top - 80;
            }
            """);
        await CaptureViewportAsync(page, "117-guided-scenario-light-empty.png");

        await guided.GetByLabel("Name the whole stretch of work").FillAsync("Sell recognized merchandise");
        await guided.GetByLabel("What is true before it begins?").FillAsync("A staffed sale is open and ready to accept merchandise.");
        await guided.GetByLabel("How do we know the whole stretch is complete?").FillAsync("The sale contains the recognized item and presents its updated total.");
        await guided.GetByLabel("Name this example").FillAsync("Scan one recognized unrestricted item");
        await guided.GetByLabel("What facts are already true?").FillAsync("A sale is open\nThe barcode identifies a known product\nThe product is unrestricted\nRequired catalog service is available");
        await guided.GetByLabel("What starts this example?").FillAsync("The clerk scans the item's barcode.");
        await guided.GetByLabel("What should be true when this example succeeds?").FillAsync("The recognized item is part of the sale at its effective price.");
        await guided.GetByLabel("Name this part of the story").FillAsync("Resolve and add scanned product");
        await guided.GetByLabel("Where or through what interface?").FillAsync("The staffed point-of-sale item-entry surface.");
        await guided.GetByLabel("What must this part accomplish?").FillAsync("Resolve product truth and add one valid sale line.");
        await guided.GetByLabel("Who starts it?").SelectOptionAsync(clerk.Actor.Id);
        await guided.GetByLabel("Who responds?").SelectOptionAsync(catalog!.Actor.Id);
        await guided.GetByLabel("Name their exchange").FillAsync("Add scanned product");
        await guided.GetByLabel("What are they trying to do?").FillAsync("Add the identified product to the open sale.");
        await guided.GetByLabel("What meaningful work occurs?").FillAsync("Resolve the product and effective price, then append a sale line.");
        await guided.GetByRole(AriaRole.Textbox, new() { Name = "What do they observe?", Exact = true }).FillAsync("The clerk sees the product description, price, and updated running total.");
        await guided.GetByLabel("What distinct results matter?").FillAsync("Added\nProductNotFound\nPriceUnavailable\nRestrictedProduct\nServiceUnavailable");
        await guided.GetByLabel("Why are you adding this story now?").FillAsync("Model the narrow ordinary item-scan path before expanding failures and recovery.");
        await Assertions.Expect(guided.GetByTestId("guided-flowboard")).ToContainTextAsync("7/7 nodes");

        await drawer.GetByRole(AriaRole.Button, new() { Name = "Close Guide Rail and return focus" }).ClickAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Reopen Guide Rail Ctrl Shift G" }).ClickAsync();
        await Assertions.Expect(page.GetByTestId("guided-scenario-recovered")).ToContainTextAsync("Your end-to-end story survived closing the rail");
        await Assertions.Expect(guided.GetByLabel("Name this example")).ToHaveValueAsync("Scan one recognized unrestricted item");
        await page.EvaluateAsync("""
            () => {
                const drawer = document.querySelector('#guide-contextual-drawer');
                const target = drawer?.querySelector('[data-testid="guided-scenario-recovered"]');
                if (drawer && target) drawer.scrollTop += target.getBoundingClientRect().top - drawer.getBoundingClientRect().top - 110;
            }
            """);
        await CaptureViewportAsync(page, "118-guided-scenario-recovered.png");

        await page.EvaluateAsync("""
            () => {
                const drawer = document.querySelector('#guide-contextual-drawer');
                const target = drawer?.querySelector('[data-testid="guided-flowboard"]');
                if (drawer && target) drawer.scrollTop += target.getBoundingClientRect().top - drawer.getBoundingClientRect().top - 90;
            }
            """);
        await CaptureViewportAsync(page, "119-guided-scenario-topology.png");

        var darkPage = await page.Context.NewPageAsync();
        await darkPage.EmulateMediaAsync(new() { ColorScheme = ColorScheme.Dark });
        await darkPage.GotoAsync(page.Url);
        await Assertions.Expect(darkPage.GetByTestId("guided-scenario-recovered")).ToBeVisibleAsync();
        await Assertions.Expect(darkPage.GetByTestId("guided-flowboard")).ToContainTextAsync("7/7 nodes");
        await darkPage.EvaluateAsync("""
            () => {
                const drawer = document.querySelector('#guide-contextual-drawer');
                const target = drawer?.querySelector('[data-testid="guided-flowboard"]');
                if (drawer && target) drawer.scrollTop += target.getBoundingClientRect().top - drawer.getBoundingClientRect().top - 90;
            }
            """);
        await CaptureViewportAsync(darkPage, "120-guided-scenario-dark.png");
        await darkPage.CloseAsync();

        var narrowPage = await page.Context.NewPageAsync();
        await narrowPage.SetViewportSizeAsync(620, 900);
        await narrowPage.GotoAsync(page.Url);
        await Assertions.Expect(narrowPage.GetByTestId("guided-flowboard")).ToContainTextAsync("7/7 nodes");
        await CaptureAsync(narrowPage, "121-guided-scenario-responsive.png");
        await narrowPage.CloseAsync();

        await guided.GetByRole(AriaRole.Button, new() { Name = "Finish scenario step" }).ClickAsync();
        var result = page.GetByTestId("guided-commit-result");
        await Assertions.Expect(result).ToContainTextAsync("Revision 5 committed");
        await Assertions.Expect(result).ToContainTextAsync("narrative.defined");
        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Which facts, rules, invariants, and semantic results govern this behavior?" })).ToBeVisibleAsync();

        var model = await api.GetFromJsonAsync<ProjectModelResponse>($"/api/v1/projects/{project.Id}/model");
        var narrative = model!.Narratives.Single();
        Assert.Multiple(() =>
        {
            Assert.That(model.Project.Revision, Is.EqualTo(5));
            Assert.That(narrative.EpisodeName, Is.EqualTo("Sell recognized merchandise"));
            Assert.That(narrative.ScenarioName, Is.EqualTo("Scan one recognized unrestricted item"));
            Assert.That(narrative.InitiatorName, Is.EqualTo("Clerk"));
            Assert.That(narrative.ReceiverName, Is.EqualTo("Product catalog"));
            Assert.That(narrative.SemanticResults, Is.EquivalentTo(GuidedItemScanResults));
        });

        await result.GetByRole(AriaRole.Link, new() { Name = "Inspect operations" }).ClickAsync();
        await Assertions.Expect(page.Locator(".change-kind").Filter(new() { HasText = "narrative.defined" })).ToBeVisibleAsync();
        await CaptureAsync(page, "122-guided-scenario-history.png");
    }

    [Test]
    [Property("ModelClaim", "0198ad00-0000-7000-8000-000000000232")]
    public async Task Given_the_same_revision_when_next_work_is_evaluated_then_the_recommendation_and_rationale_are_stable_explainable_and_purpose_relative()
    {
        using var createRequest = CreateApiRequest(
            "0198ad00-0000-7000-8000-000000000700",
            "0198ad00-0000-7000-8000-000000000735",
            new CreateProjectRequest("Recommendation decision journey", "Choose the next modeled truth without hiding why.",
                "A modeler can follow a stable purpose-relative recommendation.", "Create the D05 recommendation evidence project."));
        using var createResponse = await api!.SendAsync(createRequest);
        var project = await createResponse.Content.ReadFromJsonAsync<ProjectResponse>();
        Assert.That(createResponse.StatusCode, Is.EqualTo(HttpStatusCode.Created));

        using var modelerRequest = CreateEditApiRequest($"/api/v1/projects/{project!.Id}/actors", "1",
            "0198ad00-0000-7000-8000-000000000736",
            new AddActorRequest("Modeler", "humanRole", "Authors and reviews the definition.", "Reach a reviewable model", "Author semantic truth", "May accept modeled definitions", "Cannot invent missing authority", "Establish the initiating actor.", "known"));
        using var modelerResponse = await api.SendAsync(modelerRequest);
        var modeler = await modelerResponse.Content.ReadFromJsonAsync<AddActorResponse>();
        Assert.That(modelerResponse.StatusCode, Is.EqualTo(HttpStatusCode.Created));

        using var reviewerRequest = CreateEditApiRequest($"/api/v1/projects/{project.Id}/actors", "2",
            "0198ad00-0000-7000-8000-000000000737",
            new AddActorRequest("Reviewer", "humanRole", "Reviews observable completeness.", "Understand accepted truth", "Review scenarios and evidence", "May challenge unsupported claims", "Cannot rewrite authored facts", "Establish the receiving actor.", "known"));
        using var reviewerResponse = await api.SendAsync(reviewerRequest);
        var reviewer = await reviewerResponse.Content.ReadFromJsonAsync<AddActorResponse>();
        Assert.That(reviewerResponse.StatusCode, Is.EqualTo(HttpStatusCode.Created));

        using var outcomeRequest = CreateEditApiRequest($"/api/v1/projects/{project.Id}/outcomes", "3",
            "0198ad00-0000-7000-8000-000000000738",
            new AddOutcomeRequest("Next work is explainable", "The modeler can explain why one action precedes another.",
                "Primary action names its pressure\nBlocked alternatives name missing dependencies", modeler!.Actor.Id,
                "Establish the recommendation outcome.", "known"));
        using var outcomeResponse = await api.SendAsync(outcomeRequest);
        var outcome = await outcomeResponse.Content.ReadFromJsonAsync<AddOutcomeResponse>();
        Assert.That(outcomeResponse.StatusCode, Is.EqualTo(HttpStatusCode.Created));

        var narrativeContract = new DefineNarrativeRequest(outcome!.Outcome.Id, [modeler.Actor.Id, reviewer!.Actor.Id], modeler.Actor.Id, reviewer.Actor.Id,
            "Choose next modeled truth", "A framed model has no explicit state packet.", "The modeler can enter the next owned editor with rationale visible.",
            "Review recommendation after framing", "Happy", "Participants and outcome exist\nOne complete narrative exists",
            "The modeler asks what to define next.", "State definition is selected without changing canonical truth.",
            "Inspect readiness decision", "Project Builder Decision Lens", "Explain recommendation order",
            "Select next modeling action", "Understand current completeness pressure", "Evaluate purpose, findings, dependencies, and recent work.",
            "The modeler sees State recommended and Recovery blocked by missing state logic.", "RecommendationShown\nNoActionAvailable",
            "Model the D05 recommendation scenario.");
        using var narrativeRequest = CreateEditApiRequest($"/api/v1/projects/{project.Id}/narratives", "4",
            "0198ad00-0000-7000-8000-000000000739", narrativeContract);
        using var narrativeResponse = await api.SendAsync(narrativeRequest);
        Assert.That(narrativeResponse.StatusCode, Is.EqualTo(HttpStatusCode.Created));

        var discoveryUrl = $"/api/v1/projects/{project.Id}/recommendations?profile=discovery";
        var firstContract = await api.GetStringAsync(discoveryUrl);
        var secondContract = await api.GetStringAsync(discoveryUrl);
        Assert.That(secondContract, Is.EqualTo(firstContract));
        var discovery = await api.GetFromJsonAsync<ProjectRecommendationsResponse>(discoveryUrl);
        var implementation = await api.GetFromJsonAsync<ProjectRecommendationsResponse>($"/api/v1/projects/{project.Id}/recommendations?profile=implementation-ready");
        Assert.Multiple(() =>
        {
            Assert.That(discovery!.Revision, Is.EqualTo(5));
            Assert.That(discovery.PrimaryRecommendationId, Is.EqualTo("recommend.state"));
            Assert.That(discovery.Candidates.Single(item => item.Id == "recommend.state").Priority, Is.EqualTo("Advisory for profile"));
            Assert.That(implementation!.Revision, Is.EqualTo(discovery.Revision));
            Assert.That(implementation.Candidates.Single(item => item.Id == "recommend.state").Priority, Is.EqualTo("Required for profile"));
        });

        var page = await NewPageAsync();
        await page.GotoAsync($"{baseUrl}/projects/{project.Id}/recommendations");
        var studio = page.GetByTestId("recommendation-studio");
        await Assertions.Expect(studio.GetByRole(AriaRole.Heading, new() { Name = "Choose work without guessing" })).ToBeVisibleAsync();
        await Assertions.Expect(studio.GetByTestId("primary-recommendation")).ToContainTextAsync("Make facts, rules, and results explicit");
        await Assertions.Expect(studio.GetByTestId("primary-recommendation")).ToContainTextAsync("Advisory for profile");
        var graph = studio.GetByTestId("recommendation-decision-graph");
        await Assertions.Expect(graph).ToContainTextAsync("Purpose pressure");
        await Assertions.Expect(graph).ToContainTextAsync("PB-STATE-011");
        await Assertions.Expect(graph).ToContainTextAsync("Dependency gate");
        await Assertions.Expect(graph).ToContainTextAsync("Recent-work continuity");
        await CaptureAsync(page, "123-recommendation-discovery-light.png");

        await studio.GetByRole(AriaRole.Button, new() { Name = "Implementation Ready" }).ClickAsync();
        await Assertions.Expect(studio.GetByTestId("primary-recommendation")).ToContainTextAsync("Required for profile");
        await Assertions.Expect(page).ToHaveURLAsync(new Regex("profile=implementation-ready"));
        await Assertions.Expect(studio.GetByTestId("recommendation-model-revision")).ToHaveTextAsync("r5");
        await CaptureAsync(page, "124-recommendation-implementation-ready.png");

        var pathCandidate = studio.GetByTestId("recommendation-candidate").Filter(new() { HasText = "Close the most material unmodeled result" });
        await Assertions.Expect(pathCandidate).ToContainTextAsync("Blocked");
        await Assertions.Expect(pathCandidate).ToContainTextAsync("Needs a complete scenario + state logic with semantic results");
        await Assertions.Expect(studio.GetByText("No completeness score", new() { Exact = true })).ToBeVisibleAsync();
        await CaptureAsync(page, "125-recommendation-ranked-alternatives.png");

        var darkPage = await NewPageAsync(ColorScheme.Dark);
        await darkPage.GotoAsync(page.Url);
        await Assertions.Expect(darkPage.GetByTestId("primary-recommendation")).ToContainTextAsync("Required for profile");
        await Assertions.Expect(darkPage.GetByTestId("recommendation-decision-graph")).ToBeVisibleAsync();
        await CaptureAsync(darkPage, "126-recommendation-dark.png");
        await darkPage.Context.CloseAsync();

        var narrowPage = await NewPageAsync(ColorScheme.Light, 620, 900);
        await narrowPage.GotoAsync(page.Url);
        await Assertions.Expect(narrowPage.GetByRole(AriaRole.List, new() { Name = "Structured recommendation rationale" })).ToBeVisibleAsync();
        await CaptureAsync(narrowPage, "127-recommendation-responsive.png");
        await narrowPage.Context.CloseAsync();

        await studio.GetByTestId("primary-recommendation").GetByRole(AriaRole.Link, new() { Name = "Define state and logic" }).ClickAsync();
        await ExpectHeadingAsync(page, "Define state and logic");
        await Assertions.Expect(page.GetByText("projectbuilder.state-logic.define", new() { Exact = true })).ToBeVisibleAsync();
        await CaptureAsync(page, "128-recommendation-action-continuity.png");
    }

    [Test]
    [Property("ModelClaim", "0198ad00-0000-7000-8000-000000000206")]
    public async Task Given_invalid_meaning_when_creation_is_attempted_then_recovery_is_visible_and_input_remains()
    {
        var page = await NewPageAsync();
        await page.GotoAsync($"{baseUrl}/projects/new");
        await page.GetByLabel("Project name").FillAsync("Invalid project example");
        await page.GetByLabel("Change-set reason").FillAsync("x");

        await page.GetByRole(AriaRole.Button, new() { Name = "Create project" }).ClickAsync();

        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Name = "The project could not be created" })).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByText("Project purpose is required.")).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByText("Project intended outcome is required.")).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByLabel("Project name")).ToHaveValueAsync("Invalid project example");
        await CaptureAsync(page, "04-semantic-errors-preserve-input.png");
    }

    [Test]
    [Property("ModelClaim", "0198ad00-0000-7000-8000-000000000207")]
    public async Task Given_a_create_project_contract_when_posted_then_the_same_revision_is_queryable()
    {
        const string workspaceId = "0198ad00-0000-7000-8000-000000000700";
        var contract = new CreateProjectRequest(
            "API contract project",
            "Prove the stable server boundary.",
            "A client can create and query revision 1.",
            "Exercise the project API contract.");
        using var request = CreateApiRequest(workspaceId, "0198ad00-0000-7000-8000-000000000799", contract);

        using var createdResponse = await api!.SendAsync(request);
        var created = await createdResponse.Content.ReadFromJsonAsync<ProjectResponse>();
        using var queriedResponse = await api.GetAsync($"/api/v1/projects/{created!.Id}");
        var queried = await queriedResponse.Content.ReadFromJsonAsync<ProjectResponse>();
        using var retryRequest = CreateApiRequest(workspaceId, "0198ad00-0000-7000-8000-000000000799", contract);
        using var retryResponse = await api.SendAsync(retryRequest);
        var retried = await retryResponse.Content.ReadFromJsonAsync<ProjectResponse>();
        using var duplicateRequest = CreateApiRequest(
            workspaceId,
            "0198ad00-0000-7000-8000-000000000798",
            contract with { Name = "API CONTRACT PROJECT" });
        using var duplicateResponse = await api.SendAsync(duplicateRequest);

        Assert.Multiple(() =>
        {
            Assert.That(createdResponse.StatusCode, Is.EqualTo(HttpStatusCode.Created));
            Assert.That(queriedResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(queried, Is.EqualTo(created));
            Assert.That(retryResponse.StatusCode, Is.EqualTo(HttpStatusCode.Created));
            Assert.That(retried, Is.EqualTo(created));
            Assert.That(duplicateResponse.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
            Assert.That(created.Revision, Is.EqualTo(1));
        });
    }

    [Test]
    [Property("ModelClaim", "0198ad00-0000-7000-8000-000000000210")]
    [Property("ModelClaim", "0198ad00-0000-7000-8000-000000000211")]
    [Property("ModelClaim", "0198ad00-0000-7000-8000-000000000213")]
    public async Task Given_a_modeled_scenario_when_state_logic_and_paths_are_posted_then_both_are_queryable_and_retryable()
    {
        const string workspaceId = "0198ad00-0000-7000-8000-000000000700";
        var projectContract = new CreateProjectRequest(
            "State logic API contract project",
            "Prove the state and logic server boundary.",
            "A client can commit and query typed state and logic.",
            "Create the state and logic API fixture.");
        using var deniedRequest = CreateApiRequest(
            "0198ad00-0000-7000-8000-000000000710",
            "0198ad00-0000-7000-8000-000000000714",
            projectContract);
        using var deniedResponse = await api!.SendAsync(deniedRequest);
        var denied = await deniedResponse.Content.ReadFromJsonAsync<ProjectProblemResponse>();
        Assert.Multiple(() =>
        {
            Assert.That(deniedResponse.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
            Assert.That(denied!.Code, Is.EqualTo("project.denied"));
        });

        using var projectRequest = CreateApiRequest(
            workspaceId,
            "0198ad00-0000-7000-8000-000000000711",
            projectContract);
        using var projectResponse = await api!.SendAsync(projectRequest);
        var projectBody = await projectResponse.Content.ReadAsStringAsync();
        Assert.That(projectResponse.StatusCode, Is.EqualTo(HttpStatusCode.Created), projectBody);
        var project = await projectResponse.Content.ReadFromJsonAsync<ProjectResponse>();

        var actorContract = new AddActorRequest(
            "Modeler", "humanRole", "Defines project meaning.", "Create a valid project",
            "Provide authoritative intent", "Define project purpose", "Must preserve invariants",
            "Add the state owner.");
        using var actorRequest = CreateEditApiRequest(
            $"/api/v1/projects/{project!.Id}/actors", "1",
            "0198ad00-0000-7000-8000-000000000712", actorContract);
        using var actorResponse = await api.SendAsync(actorRequest);
        var actorBody = await actorResponse.Content.ReadAsStringAsync();
        Assert.That(actorResponse.StatusCode, Is.EqualTo(HttpStatusCode.Created), actorBody);
        var actorEnvelope = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(actorBody);
        var actorId = actorEnvelope.GetProperty("actor").GetProperty("id").GetString()!;

        var outcomeContract = new AddOutcomeRequest(
            "Project definition is reviewable", "A modeler can reopen the accepted project definition.",
            "Purpose is visible\nRevision is visible", actorId, "Add the path outcome.");
        using var outcomeRequest = CreateEditApiRequest(
            $"/api/v1/projects/{project.Id}/outcomes", "2",
            "0198ad00-0000-7000-8000-000000000715", outcomeContract);
        using var outcomeResponse = await api.SendAsync(outcomeRequest);
        var outcomeBody = await outcomeResponse.Content.ReadAsStringAsync();
        Assert.That(outcomeResponse.StatusCode, Is.EqualTo(HttpStatusCode.Created), outcomeBody);
        var outcomeEnvelope = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(outcomeBody);
        var outcomeId = outcomeEnvelope.GetProperty("outcome").GetProperty("id").GetString()!;

        var narrativeContract = new DefineNarrativeRequest(
            outcomeId, [actorId], actorId, actorId,
            "Create Project Definition", "A modeler has an unmodeled intention.", "The project is reviewable.",
            "Authorized modeler creates project", "Happy", "The workspace exists", "The modeler submits a definition.",
            "The accepted definition is visible.", "Capture project definition", "Accessible form", "Capture meaning",
            "Submit project definition", "Create a purpose-led project", "Validate and commit", "The project is visible",
            "Created\nInvalid", "Add the source scenario.");
        using var narrativeRequest = CreateEditApiRequest(
            $"/api/v1/projects/{project.Id}/narratives", "3",
            "0198ad00-0000-7000-8000-000000000716", narrativeContract);
        using var narrativeResponse = await api.SendAsync(narrativeRequest);
        var narrativeBody = await narrativeResponse.Content.ReadAsStringAsync();
        Assert.That(narrativeResponse.StatusCode, Is.EqualTo(HttpStatusCode.Created), narrativeBody);

        var stateLogicContract = new DefineStateLogicRequest(
            "Project definition state", "Domain", "DefinitionStatus\nRevision", "Unmodeled\nDefined", actorId,
            "Project purpose recorded", "boolean", "The Project aggregate owns accepted purpose truth.", "Transitioned",
            "Project definition validity", "Validation", "Name, purpose, outcome, and reason must be valid.", actorId,
            "Project revision advances once", "Accepted creation advances revision exactly once.",
            "One accepted operation advances two revisions.", "Transition example\nIdempotent retry property",
            "Created | Success | The project was durably created.\nInvalid | Invalid | Meaning was rejected without mutation.\nConflict | Conflict | Current state was not overwritten.",
            "Create project definition", "No definition exists for the accepted operation.",
            "Authorized project intent passes validation.", "A project exists at revision 1.",
            "Define the state transition contract.");
        const string stateOperationId = "0198ad00-0000-7000-8000-000000000713";
        using var stateRequest = CreateEditApiRequest(
            $"/api/v1/projects/{project.Id}/state-logic", "4", stateOperationId, stateLogicContract);
        using var stateResponse = await api.SendAsync(stateRequest);
        using var retryRequest = CreateEditApiRequest(
            $"/api/v1/projects/{project.Id}/state-logic", "4", stateOperationId, stateLogicContract);
        using var retryResponse = await api.SendAsync(retryRequest);
        var beforePath = await api.GetFromJsonAsync<ProjectModelResponse>($"/api/v1/projects/{project.Id}/model");
        var state = beforePath!.StateLogic.Single();
        var invalidResultId = state.Results.Single(result => result.Kind == "invalid").Id;
        var successResultId = state.Results.Single(result => result.Kind == "success").Id;
        var pathContract = new DefinePathRequest(
            beforePath.Narratives.Single().ScenarioId, state.TransitionId, invalidResultId, successResultId,
            actorId, "Invalid project definition", "Exceptional", "Definition is invalid", "Branch",
            "One or more fields fail semantic validation.", state.FactId, state.RuleId,
            "Validate submitted meaning\nReturn field-level findings", "No definition exists and revision is unchanged.",
            "The modeler sees actionable findings.", "Present validation findings", "Observation",
            "Present findings without domain mutation.", "Correct and resubmit", "CorrectAndRetry",
            "Modeler chooses to correct", "The modeler corrects the rejected meaning.",
            "Correct invalid fields\nResubmit with a new operation identity", "Corrected meaning is eligible for transition.",
            "The modeler can resubmit.", "Retry only after correction.",
            "Rejected intent never commits; corrected intent has a new operation identity.",
            "Exit after creation or cancellation.", "No reconciliation is required after rejection.",
            "Define the API path contract.");
        const string pathOperationId = "0198ad00-0000-7000-8000-000000000717";
        using var pathRequest = CreateEditApiRequest(
            $"/api/v1/projects/{project.Id}/paths", "5", pathOperationId, pathContract);
        using var pathResponse = await api.SendAsync(pathRequest);
        using var pathRetryRequest = CreateEditApiRequest(
            $"/api/v1/projects/{project.Id}/paths", "5", pathOperationId, pathContract);
        using var pathRetryResponse = await api.SendAsync(pathRetryRequest);
        var staleActorContract = new AddActorRequest(
            "Stale reviewer", "humanRole", "Attempts an edit from an old revision.", "Review stale edits",
            "Avoid overwrites", "", "", "Prove structured revision conflict.");
        using var staleRequest = CreateEditApiRequest(
            $"/api/v1/projects/{project.Id}/actors", "5",
            "0198ad00-0000-7000-8000-000000000718", staleActorContract);
        using var staleResponse = await api.SendAsync(staleRequest);
        var staleProblem = await staleResponse.Content.ReadFromJsonAsync<ProjectProblemResponse>();
        var queried = await api.GetFromJsonAsync<ProjectModelResponse>($"/api/v1/projects/{project.Id}/model");

        Assert.Multiple(() =>
        {
            Assert.That(projectResponse.StatusCode, Is.EqualTo(HttpStatusCode.Created));
            Assert.That(actorResponse.StatusCode, Is.EqualTo(HttpStatusCode.Created));
            Assert.That(outcomeResponse.StatusCode, Is.EqualTo(HttpStatusCode.Created));
            Assert.That(narrativeResponse.StatusCode, Is.EqualTo(HttpStatusCode.Created));
            Assert.That(stateResponse.StatusCode, Is.EqualTo(HttpStatusCode.Created));
            Assert.That(retryResponse.StatusCode, Is.EqualTo(HttpStatusCode.Created));
            Assert.That(pathResponse.StatusCode, Is.EqualTo(HttpStatusCode.Created));
            Assert.That(pathRetryResponse.StatusCode, Is.EqualTo(HttpStatusCode.Created));
            Assert.That(staleResponse.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
            Assert.That(staleProblem!.Code, Is.EqualTo("project.revision.conflict"));
            Assert.That(staleProblem.Errors["project.revision.conflict"].Single(),
                Is.EqualTo("Expected revision 5; actual revision is 6."));
            Assert.That(queried!.Project.Revision, Is.EqualTo(6));
            Assert.That(queried.StateLogic, Has.Count.EqualTo(1));
            Assert.That(queried.StateLogic[0].StateCategory, Is.EqualTo("domain"));
            Assert.That(string.Join(',', queried.StateLogic[0].Results.Select(result => result.Kind)),
                Is.EqualTo("success,invalid,conflict"));
            Assert.That(queried.Paths, Has.Count.EqualTo(1));
            Assert.That(queried.Paths[0].BranchClassification, Is.EqualTo("exceptional"));
            Assert.That(queried.Paths[0].RecoveryStrategy, Is.EqualTo("correctAndRetry"));
            Assert.That(queried.Relations, Has.Count.EqualTo(1));
            Assert.That(queried.Relations[0].Kind, Is.EqualTo("benefitsFrom"));
            Assert.That(queried.Relations[0].SourceKind, Is.EqualTo("actor"));
            Assert.That(queried.Relations[0].TargetKind, Is.EqualTo("outcome"));
            Assert.That(queried.Relations[0].Cardinality, Is.EqualTo("oneToMany"));
            Assert.That(queried.Relations[0].DeletionBehavior, Is.EqualTo("restrict"));
            Assert.That(queried.ChangeSets, Has.Count.EqualTo(6));
            Assert.That(queried.ChangeSets.Select(changeSet => changeSet.ResultRevision),
                Is.EqualTo(SixRevisionHistory));
            Assert.That(queried.ChangeSets.Select(changeSet => changeSet.OperationCount),
                Is.EqualTo(SixOperationCounts));
        });
    }

    [Test]
    [Property("ModelClaim", "0198ad00-0000-7000-8000-000000000214")]
    public async Task Given_the_import_api_when_retried_then_it_is_idempotent_and_conflicting_reuse_is_rejected()
    {
        const string workspaceId = "0198ad00-0000-7000-8000-000000000700";
        const string operationId = "0198ad00-0000-7000-9000-000000000980";
        var fixture = await File.ReadAllTextAsync(Path.Combine(
            TestContext.CurrentContext.TestDirectory, "Fixtures", "example-importable-project.project-builder.json"));
        var apiFixture = fixture
            .Replace("000000000900", "000000000910", StringComparison.Ordinal)
            .Replace("000000000901", "000000000911", StringComparison.Ordinal)
            .Replace("000000000902", "000000000912", StringComparison.Ordinal)
            .Replace("000000000903", "000000000913", StringComparison.Ordinal)
            .Replace("Portable checkout model", "Portable API model", StringComparison.Ordinal);

        using var request = CreateImportApiRequest(workspaceId, operationId, apiFixture);
        using var response = await api!.SendAsync(request);
        var imported = await response.Content.ReadFromJsonAsync<ImportProjectResponse>();
        using var retryRequest = CreateImportApiRequest(workspaceId, operationId, apiFixture);
        using var retryResponse = await api.SendAsync(retryRequest);
        var retried = await retryResponse.Content.ReadFromJsonAsync<ImportProjectResponse>();
        using var conflictRequest = CreateImportApiRequest(
            workspaceId, operationId, apiFixture.Replace("Portable API model", "Changed portable API model", StringComparison.Ordinal));
        using var conflictResponse = await api.SendAsync(conflictRequest);
        var conflict = await conflictResponse.Content.ReadFromJsonAsync<PortableProjectProblemResponse>();

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
            Assert.That(retryResponse.StatusCode, Is.EqualTo(HttpStatusCode.Created));
            Assert.That(retried, Is.EqualTo(imported));
            Assert.That(conflictResponse.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
            Assert.That(conflict!.Code, Is.EqualTo("project.operation.conflict"));
        });
    }

    [Test]
    [Property("ModelClaim", "0198ad00-0000-7000-8000-000000000218")]
    public async Task Given_an_actor_definition_when_a_contributor_stages_and_commits_then_the_typed_command_is_visible_and_evidenced()
    {
        var projectContract = new CreateProjectRequest(
            "Actor editor evidence project", "Prove the typed editor framework.",
            "A contributor can stage and commit an actor with explicit uncertainty.",
            "Create the actor editor evidence fixture.");
        using var createRequest = CreateApiRequest(
            "0198ad00-0000-7000-8000-000000000700",
            "0198ad00-0000-7000-8000-000000000719", projectContract);
        using var createResponse = await api!.SendAsync(createRequest);
        var project = await createResponse.Content.ReadFromJsonAsync<ProjectResponse>();
        Assert.That(createResponse.StatusCode, Is.EqualTo(HttpStatusCode.Created));

        var page = await NewPageAsync();
        await page.GotoAsync($"{baseUrl}/projects/{project!.Id}/actors/new");
        await Assertions.Expect(page.GetByTestId("actor-editor")).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByTestId("draft-state")).ToContainTextAsync("Clean draft");
        await Assertions.Expect(page.GetByText("projectbuilder.element.create.actor", new() { Exact = true })).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByTestId("source-gap")).ToContainTextAsync("Unknown · not exposed");
        await CaptureAsync(page, "43-actor-editor-light-clean.png");

        await page.Locator("#actor-name").FillAsync("Reviewer");
        await Assertions.Expect(page.GetByTestId("draft-state")).ToContainTextAsync("Staged draft");
        await Assertions.Expect(page.GetByText("Contextual role is required.", new() { Exact = true })).ToBeVisibleAsync();
        await page.Locator("#actor-knowledge").SelectOptionAsync("assumed");
        await page.Locator("#actor-role").FillAsync("Reviews proposed model meaning before acceptance.");
        await page.Locator("#actor-goals").FillAsync("Make review decisions traceable");
        await page.Locator("#actor-responsibilities").FillAsync("Challenge unsupported semantic claims");
        await page.Locator("#actor-authority").FillAsync("Approve or reject a proposed definition");
        await page.Locator("#actor-constraints").FillAsync("Cannot invent missing domain truth");
        await page.Locator("#actor-reason").FillAsync("Model an assumed reviewer role for editor evidence.");
        await Assertions.Expect(page.GetByText("Assumed is explicit.", new() { Exact = true })).ToBeVisibleAsync();
        await page.EvaluateAsync("window.scrollTo(0, 0)");
        await CaptureAsync(page, "44-actor-editor-light-staged.png");

        await page.GetByRole(AriaRole.Button, new() { Name = "Commit actor" }).ClickAsync();
        await Assertions.Expect(page.GetByTestId("actor-committed")).ToContainTextAsync("Reviewer added");
        await Assertions.Expect(page.GetByTestId("actor-committed")).ToContainTextAsync("Knowledge state: Assumed");
        var model = await api.GetFromJsonAsync<ProjectModelResponse>($"/api/v1/projects/{project.Id}/model");
        Assert.That(model!.Actors.Single().KnowledgeStatus, Is.EqualTo("assumed"));
        await CaptureAsync(page, "45-actor-editor-committed.png");
        await page.Context.CloseAsync();

        var darkPage = await NewPageAsync(ColorScheme.Dark);
        await darkPage.GotoAsync($"{baseUrl}/projects/{project.Id}/actors/new");
        await Assertions.Expect(darkPage.GetByTestId("actor-editor")).ToBeVisibleAsync();
        await CaptureAsync(darkPage, "46-actor-editor-dark.png");
        await darkPage.Context.CloseAsync();

        var narrowPage = await NewPageAsync(ColorScheme.Light, 620, 900);
        await narrowPage.GotoAsync($"{baseUrl}/projects/{project.Id}/actors/new");
        await Assertions.Expect(narrowPage.GetByRole(AriaRole.Heading, new() { Name = "Definition readiness" })).ToBeVisibleAsync();
        await CaptureAsync(narrowPage, "47-actor-editor-responsive.png");
        await narrowPage.Context.CloseAsync();
    }

    [Test]
    [Property("ModelClaim", "0198ad00-0000-7000-8000-000000000219")]
    public async Task Given_existing_participants_and_outcomes_when_a_contributor_defines_meaning_then_relations_duplicates_and_uncertainty_are_visible()
    {
        using var createRequest = CreateApiRequest(
            "0198ad00-0000-7000-8000-000000000700",
            "0198ad00-0000-7000-8000-000000000720",
            new CreateProjectRequest(
                "Typed outcome editor evidence", "Prove participant and outcome authoring guidance.",
                "A contributor can distinguish and relate observable outcomes.", "Create the C05 editor fixture."));
        using var createResponse = await api!.SendAsync(createRequest);
        var project = await createResponse.Content.ReadFromJsonAsync<ProjectResponse>();
        Assert.That(createResponse.StatusCode, Is.EqualTo(HttpStatusCode.Created));

        using var actorRequest = CreateEditApiRequest(
            $"/api/v1/projects/{project!.Id}/actors", "1", "0198ad00-0000-7000-8000-000000000721",
            new AddActorRequest("Contributor", "humanRole", "Builds and verifies the repository.",
                "Deliver usable model behavior", "Preserve model truth", "Commit reviewed changes",
                "Cannot silently merge definitions", "Seed the C05 participant."));
        using var actorResponse = await api.SendAsync(actorRequest);
        var actor = (await actorResponse.Content.ReadFromJsonAsync<AddActorResponse>())!.Actor;
        Assert.That(actorResponse.StatusCode, Is.EqualTo(HttpStatusCode.Created));

        using var outcomeRequest = CreateEditApiRequest(
            $"/api/v1/projects/{project.Id}/outcomes", "2", "0198ad00-0000-7000-8000-000000000722",
            new AddOutcomeRequest("Repository is verifiable", "A contributor can verify a clean repository clone.",
                "Build passes\nTests pass", actor.Id, "Seed the duplicate suggestion."));
        using var outcomeResponse = await api.SendAsync(outcomeRequest);
        var seededOutcome = (await outcomeResponse.Content.ReadFromJsonAsync<AddOutcomeResponse>())!.Outcome;
        Assert.That(outcomeResponse.StatusCode, Is.EqualTo(HttpStatusCode.Created));

        var actorPage = await NewPageAsync();
        await actorPage.GotoAsync($"{baseUrl}/projects/{project.Id}/actors/new");
        await actorPage.GetByLabel("Actor name").FillAsync("Contributor");
        await Assertions.Expect(actorPage.GetByTestId("actor-duplicates")).ToContainTextAsync("Check before creating another actor");
        await Assertions.Expect(actorPage.GetByTestId("actor-duplicates")).ToContainTextAsync("Builds and verifies the repository.");
        await CaptureAsync(actorPage, "48-actor-duplicate-suggestion.png");
        await actorPage.Context.CloseAsync();

        var page = await NewPageAsync();
        await page.GotoAsync($"{baseUrl}/projects/{project.Id}/outcomes/new");
        await Assertions.Expect(page.GetByTestId("outcome-editor")).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByTestId("beneficiary-relation")).ToContainTextAsync("Contributor");
        await Assertions.Expect(page.GetByText("projectbuilder.element.create.outcome", new() { Exact = true })).ToBeVisibleAsync();
        await CaptureAsync(page, "49-outcome-editor-light-clean.png");

        await page.GetByLabel("Outcome name").FillAsync("Repository is verifiable");
        await Assertions.Expect(page.GetByTestId("outcome-duplicates")).ToContainTextAsync("Compare existing outcomes");
        await Assertions.Expect(page.GetByTestId("outcome-duplicates")).ToContainTextAsync("A contributor can verify a clean repository clone.");
        await CaptureAsync(page, "50-outcome-duplicate-suggestion.png");

        await page.GetByLabel("Outcome name").FillAsync("Review decision is traceable");
        await page.GetByTestId("outcome-knowledge").SelectOptionAsync("disputed");
        await page.GetByLabel("Observable outcome statement").FillAsync("A contributor can trace why a model review was accepted or disputed.");
        await page.GetByLabel("Success signals").FillAsync("Change reason remains visible\nKnowledge state is explicit");
        await page.GetByLabel("Change reason").FillAsync("Capture a disputed outcome through the typed editor.");
        await Assertions.Expect(page.GetByText("Disputed is explicit.", new() { Exact = true })).ToBeVisibleAsync();
        await page.Keyboard.PressAsync("Control+z");
        await Assertions.Expect(page.GetByLabel("Change reason")).ToHaveValueAsync("Define an observable project outcome.");
        await page.Keyboard.PressAsync("Control+y");
        await Assertions.Expect(page.GetByLabel("Change reason")).ToHaveValueAsync("Capture a disputed outcome through the typed editor.");
        await CaptureAsync(page, "88-outcome-editor-keyboard-redo.png");
        await page.ReloadAsync();
        await Assertions.Expect(page.GetByTestId("outcome-draft-recovered")).ToContainTextAsync("Outcome draft restored after refresh");
        await Assertions.Expect(page.GetByLabel("Outcome name")).ToHaveValueAsync("Review decision is traceable");
        await CaptureAsync(page, "89-outcome-editor-refresh-recovered.png");
        await page.GetByLabel("Change reason").FocusAsync();
        await page.Keyboard.PressAsync("Control+s");
        await Assertions.Expect(page.GetByTestId("outcome-committed")).ToContainTextAsync("Review decision is traceable added");
        await Assertions.Expect(page.GetByTestId("outcome-committed")).ToContainTextAsync("Knowledge state: Disputed");
        var model = await api.GetFromJsonAsync<ProjectModelResponse>($"/api/v1/projects/{project.Id}/model");
        Assert.That(model!.Outcomes.Single(outcome => outcome.Name == "Review decision is traceable").KnowledgeStatus, Is.EqualTo("disputed"));
        await CaptureAsync(page, "51-outcome-editor-committed.png");
        await page.Context.CloseAsync();

        using var reviewerRequest = CreateEditApiRequest(
            $"/api/v1/projects/{project.Id}/actors", "4", "0198ad00-0000-7000-8000-000000000723",
            new AddActorRequest("Reviewer", "humanRole", "Reviews modeled outcomes before acceptance.",
                "Reach an evidence-based decision", "Review semantic claims", "Approve or dispute outcomes", "Cannot invent evidence", "Add the C05 reviewer."));
        using var reviewerResponse = await api.SendAsync(reviewerRequest);
        var reviewer = (await reviewerResponse.Content.ReadFromJsonAsync<AddActorResponse>())!.Actor;
        Assert.That(reviewerResponse.StatusCode, Is.EqualTo(HttpStatusCode.Created));

        var editOutcomePage = await NewPageAsync();
        var createdOutcome = model.Outcomes.Single(outcome => outcome.Name == "Review decision is traceable");
        await editOutcomePage.GotoAsync($"{baseUrl}/projects/{project.Id}/outcomes/{createdOutcome.Id}/edit");
        await Assertions.Expect(editOutcomePage.GetByRole(AriaRole.Heading, new() { Name = "Edit outcome" })).ToBeVisibleAsync();
        await Assertions.Expect(editOutcomePage.GetByText("projectbuilder.element.update.outcome", new() { Exact = true })).ToBeVisibleAsync();
        await editOutcomePage.GetByLabel("Outcome name").FillAsync("Review decision is auditable");
        await editOutcomePage.GetByLabel("Beneficiary").SelectOptionAsync(reviewer.Id);
        await editOutcomePage.GetByLabel("Change reason").FillAsync("Retarget the review outcome to its reviewing actor.");
        await Assertions.Expect(editOutcomePage.GetByTestId("beneficiary-relation")).ToContainTextAsync("Reviewer");
        await CaptureAsync(editOutcomePage, "54-outcome-update-staged.png");
        await editOutcomePage.GetByRole(AriaRole.Button, new() { Name = "Commit outcome update" }).ClickAsync();
        await Assertions.Expect(editOutcomePage.GetByTestId("outcome-committed")).ToContainTextAsync("Review decision is auditable updated");
        await CaptureAsync(editOutcomePage, "55-outcome-update-committed.png");
        await editOutcomePage.Context.CloseAsync();

        var editActorPage = await NewPageAsync();
        await editActorPage.GotoAsync($"{baseUrl}/projects/{project.Id}/actors/{reviewer.Id}/edit");
        await Assertions.Expect(editActorPage.GetByRole(AriaRole.Heading, new() { Name = "Edit actor" })).ToBeVisibleAsync();
        await editActorPage.GetByLabel("Contextual role").FillAsync("Reviews and disputes unsupported modeled outcomes before acceptance.");
        await editActorPage.GetByTestId("knowledge-status").SelectOptionAsync("assumed");
        await editActorPage.GetByLabel("Change reason").FillAsync("Clarify reviewer authority and uncertainty.");
        await CaptureAsync(editActorPage, "56-actor-update-staged.png");
        await editActorPage.GetByRole(AriaRole.Button, new() { Name = "Commit actor update" }).ClickAsync();
        await Assertions.Expect(editActorPage.GetByTestId("actor-committed")).ToContainTextAsync("Reviewer updated");
        await editActorPage.Context.CloseAsync();

        var conflictPage = await NewPageAsync();
        await conflictPage.GotoAsync($"{baseUrl}/projects/{project.Id}/outcomes/{seededOutcome.Id}/edit");
        await conflictPage.GetByLabel("Outcome name").FillAsync("Repository verification is repeatable");
        await conflictPage.GetByLabel("Change reason").FillAsync("Exercise visible revision conflict recovery.");
        using var concurrentUpdate = CreatePutApiRequest(
            $"/api/v1/projects/{project.Id}/actors/{actor.Id}", "7", "0198ad00-0000-7000-8000-000000000724",
            new UpdateActorRequest(actor.Name, actor.ActorKind, actor.ContextualRole, string.Join('\n', actor.Goals),
                string.Join('\n', actor.Responsibilities), string.Join('\n', actor.Authority), string.Join('\n', actor.Constraints),
                "Advance the revision for conflict evidence.", actor.KnowledgeStatus));
        using var concurrentResponse = await api.SendAsync(concurrentUpdate);
        Assert.That(concurrentResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        await conflictPage.GetByRole(AriaRole.Button, new() { Name = "Commit outcome update" }).ClickAsync();
        await Assertions.Expect(conflictPage.GetByRole(AriaRole.Button, new() { Name = "Refresh revision and keep draft" })).ToBeVisibleAsync();
        await Assertions.Expect(conflictPage.GetByLabel("Outcome name")).ToHaveValueAsync("Repository verification is repeatable");
        await CaptureAsync(conflictPage, "57-outcome-update-conflict.png");
        await conflictPage.GetByRole(AriaRole.Button, new() { Name = "Refresh revision and keep draft" }).ClickAsync();
        await Assertions.Expect(conflictPage.GetByLabel("Outcome name")).ToHaveValueAsync("Repository verification is repeatable");
        await conflictPage.GetByRole(AriaRole.Button, new() { Name = "Commit outcome update" }).ClickAsync();
        await Assertions.Expect(conflictPage.GetByTestId("outcome-committed")).ToContainTextAsync("Repository verification is repeatable updated");
        await CaptureAsync(conflictPage, "58-outcome-update-recovered.png");
        await conflictPage.Context.CloseAsync();

        var updatedModel = await api.GetFromJsonAsync<ProjectModelResponse>($"/api/v1/projects/{project.Id}/model");
        Assert.Multiple(() =>
        {
            Assert.That(updatedModel!.Project.Revision, Is.EqualTo(9));
            Assert.That(updatedModel.Outcomes.Single(outcome => outcome.Name == "Review decision is auditable").BeneficiaryActorId, Is.EqualTo(reviewer.Id));
            Assert.That(updatedModel.Actors.Single(value => value.Id == reviewer.Id).KnowledgeStatus, Is.EqualTo("assumed"));
            Assert.That(updatedModel.ChangeSets.SelectMany(change => change.Operations).Any(operation => operation.Kind == "element.updated"), Is.True);
            Assert.That(updatedModel.ChangeSets.SelectMany(change => change.Operations).Any(operation => operation.Kind == "relation.updated"), Is.True);
        });

        var darkPage = await NewPageAsync(ColorScheme.Dark);
        await darkPage.GotoAsync($"{baseUrl}/projects/{project.Id}/outcomes/new");
        await Assertions.Expect(darkPage.GetByTestId("beneficiary-relation")).ToBeVisibleAsync();
        await CaptureAsync(darkPage, "52-outcome-editor-dark.png");
        await darkPage.Context.CloseAsync();

        var narrowPage = await NewPageAsync(ColorScheme.Light, 620, 900);
        await narrowPage.GotoAsync($"{baseUrl}/projects/{project.Id}/outcomes/new");
        await Assertions.Expect(narrowPage.GetByRole(AriaRole.Heading, new() { Name = "Is the change observable?" })).ToBeVisibleAsync();
        await CaptureAsync(narrowPage, "53-outcome-editor-responsive.png");
        await narrowPage.Context.CloseAsync();
    }

    private static HttpRequestMessage CreateApiRequest(
        string workspaceId,
        string operationId,
        CreateProjectRequest contract)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/workspaces/{workspaceId}/projects")
        {
            Content = JsonContent.Create(contract),
        };
        request.Headers.Add("Idempotency-Key", operationId);
        return request;
    }

    private static HttpRequestMessage CreateImportApiRequest(string workspaceId, string operationId, string document)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/workspaces/{workspaceId}/projects/import")
        {
            Content = JsonContent.Create(new ImportProjectRequest(document, "Import the portable API fixture.")),
        };
        request.Headers.Add("Idempotency-Key", operationId);
        return request;
    }

    [Test]
    [Property("ModelClaim", "0198ad00-0000-7000-8000-000000000224")]
    public async Task Given_a_local_actor_draft_when_the_model_advances_then_recovery_and_committed_history_remain_explicit()
    {
        using var createRequest = CreateApiRequest(
            "0198ad00-0000-7000-8000-000000000700",
            "0198ad00-0000-7000-8000-000000000726",
            new CreateProjectRequest("Draft and history evidence", "Prove accountable authoring continuity.",
                "A contributor can recover local work and inspect committed operations.", "Create the C09 fixture."));
        using var createResponse = await api!.SendAsync(createRequest);
        var project = await createResponse.Content.ReadFromJsonAsync<ProjectResponse>();
        Assert.That(createResponse.StatusCode, Is.EqualTo(HttpStatusCode.Created));

        var page = await NewPageAsync();
        var actorUrl = $"{baseUrl}/projects/{project!.Id}/actors/new";
        await page.GotoAsync(actorUrl);
        await page.GetByLabel("Actor name").FillAsync("Evidence steward");
        await page.GetByLabel("Contextual role").FillAsync("Keeps executable proof connected to modeled claims.");
        await Assertions.Expect(page.GetByTestId("undo-draft")).ToBeEnabledAsync();
        await CaptureAsync(page, "76-draft-staged.png");

        await page.GetByTestId("undo-draft").ClickAsync();
        await Assertions.Expect(page.GetByLabel("Contextual role")).ToHaveValueAsync(string.Empty);
        await page.GetByTestId("redo-draft").ClickAsync();
        await Assertions.Expect(page.GetByLabel("Contextual role")).ToHaveValueAsync("Keeps executable proof connected to modeled claims.");
        await CaptureAsync(page, "77-draft-undo-redo.png");

        await page.ReloadAsync();
        await Assertions.Expect(page.GetByTestId("draft-recovered")).ToContainTextAsync("Draft recovered after refresh");
        await Assertions.Expect(page.GetByLabel("Actor name")).ToHaveValueAsync("Evidence steward");
        await CaptureAsync(page, "78-draft-refresh-recovered.png");

        using var advanceRequest = CreateEditApiRequest($"/api/v1/projects/{project.Id}/actors", "1",
            "0198ad00-0000-7000-8000-000000000727",
            new AddActorRequest("Reviewer", "humanRole", "Reviews committed evidence.", "Validate claims", "Review revision history", "Accept evidence", "Cannot rewrite authored truth", "Advance the fixture revision.", "known"));
        using var advanceResponse = await api.SendAsync(advanceRequest);
        Assert.That(advanceResponse.StatusCode, Is.EqualTo(HttpStatusCode.Created));

        await page.ReloadAsync();
        await Assertions.Expect(page.GetByTestId("draft-recovered")).ToContainTextAsync("earlier revision");
        await Assertions.Expect(page.GetByRole(AriaRole.Button, new() { Name = "Commit actor" })).ToBeDisabledAsync();
        await CaptureAsync(page, "79-draft-stale.png");
        await page.GetByRole(AriaRole.Button, new() { Name = "Use latest revision and keep draft" }).ClickAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Button, new() { Name = "Commit actor" })).ToBeEnabledAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Commit actor" }).ClickAsync();
        await Assertions.Expect(page.GetByTestId("actor-committed")).ToContainTextAsync("Evidence steward added");
        await page.Context.CloseAsync();

        var historyUrl = $"{baseUrl}/projects/{project.Id}/history";
        var historyPage = await NewPageAsync();
        await historyPage.GotoAsync(historyUrl);
        await Assertions.Expect(historyPage.GetByTestId("history-workbench")).ToBeVisibleAsync();
        await Assertions.Expect(historyPage.GetByTestId("operation-diff")).ToContainTextAsync("actor");
        await CaptureAsync(historyPage, "80-revision-history-light.png");
        await historyPage.GetByTestId("revision-card").Last.ClickAsync();
        await Assertions.Expect(historyPage.GetByRole(AriaRole.Heading, new() { NameRegex = new Regex("r0.*r1") })).ToBeVisibleAsync();
        await CaptureAsync(historyPage, "81-semantic-operation-diff.png");
        await historyPage.Context.CloseAsync();

        var darkPage = await NewPageAsync(ColorScheme.Dark);
        await darkPage.GotoAsync(historyUrl);
        await Assertions.Expect(darkPage.GetByTestId("operation-diff")).ToBeVisibleAsync();
        await CaptureAsync(darkPage, "82-revision-history-dark.png");
        await darkPage.Context.CloseAsync();

        var narrowPage = await NewPageAsync(ColorScheme.Light, 620, 900);
        await narrowPage.GotoAsync(historyUrl);
        await Assertions.Expect(narrowPage.GetByRole(AriaRole.Heading, new() { Name = "Timeline" })).ToBeVisibleAsync();
        await CaptureAsync(narrowPage, "83-revision-history-responsive.png");
        await narrowPage.Context.CloseAsync();
    }

    [Test]
    [Property("ModelClaim", "0198ad00-0000-7000-8000-000000000234")]
    [Property("ModelClaim", "0198ad00-0000-7000-8000-000000000240")]
    [Property("ModelClaim", "0198ad00-0000-7000-8000-000000000245")]
    [Property("ModelClaim", "0198ad00-0000-7000-8000-000000000246")]
    public async Task Given_a_project_revision_when_a_modeler_opens_the_lens_then_topology_is_deterministic_filterable_and_accessible()
    {
        using var createRequest = CreateApiRequest(
            "0198ad00-0000-7000-8000-000000000700", "0198ad00-0000-7000-8000-000000000750",
            new CreateProjectRequest("Lens evidence project", "Inspect one canonical model through a typed projection.",
                "A modeler can trace definitions and relations without changing truth.", "Create the E01 lens fixture."));
        using var createResponse = await api!.SendAsync(createRequest);
        var project = await createResponse.Content.ReadFromJsonAsync<ProjectResponse>();
        Assert.That(createResponse.StatusCode, Is.EqualTo(HttpStatusCode.Created));

        using var actorRequest = CreateEditApiRequest($"/api/v1/projects/{project!.Id}/actors", "1",
            "0198ad00-0000-7000-8000-000000000751", new AddActorRequest("Modeler", "humanRole",
                "Inspects semantic topology.", "Understand system truth", "Trace definitions", "May inspect projections",
                "Cannot change truth through layout", "Define the lens actor.", "known"));
        using var actorResponse = await api.SendAsync(actorRequest);
        var actor = (await actorResponse.Content.ReadFromJsonAsync<AddActorResponse>())!.Actor;
        Assert.That(actorResponse.StatusCode, Is.EqualTo(HttpStatusCode.Created));

        using var outcomeRequest = CreateEditApiRequest($"/api/v1/projects/{project.Id}/outcomes", "2",
            "0198ad00-0000-7000-8000-000000000752", new AddOutcomeRequest("Topology is traceable",
                "Every visible connector resolves to typed semantic endpoints.", "No dangling edges\nStructured equivalent exists",
                actor.Id, "Define the lens outcome.", "known"));
        using var outcomeResponse = await api.SendAsync(outcomeRequest);
        Assert.That(outcomeResponse.StatusCode, Is.EqualTo(HttpStatusCode.Created));

        var first = await api.GetStringAsync($"/api/v1/projects/{project.Id}/lens");
        var second = await api.GetStringAsync($"/api/v1/projects/{project.Id}/lens");
        Assert.That(second, Is.EqualTo(first));
        var contract = await api.GetFromJsonAsync<LensProjectionResponse>($"/api/v1/projects/{project.Id}/lens");
        Assert.Multiple(() => { Assert.That(contract!.Nodes, Has.Count.EqualTo(3)); Assert.That(contract.Edges, Has.Count.EqualTo(1)); Assert.That(contract.AccessibilityTree, Has.Count.EqualTo(3)); });

        var page = await NewPageAsync();
        var url = $"{baseUrl}/projects/{project.Id}/lens-lab";
        await page.GotoAsync(url);
        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Name = "See one model, many lenses" })).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByTestId("lens-canvas")).ToContainTextAsync("Topology is traceable");
        await CaptureAsync(page, "136-lens-lab-light.png");
        await page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("Modeler") }).First.ClickAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Modeler" })).ToBeVisibleAsync();
        await CaptureAsync(page, "137-lens-inspector-selected.png");
        await page.GetByRole(AriaRole.Button, new() { Name = "Outcomes" }).ClickAsync();
        await Assertions.Expect(page.GetByText("lens.filter.edge-suppressed", new() { Exact = true })).ToBeVisibleAsync();
        await CaptureAsync(page, "138-lens-filter-diagnostic.png");
        await page.GetByRole(AriaRole.Button, new() { Name = "All definitions" }).ClickAsync();
        var canvas = page.GetByTestId("model-canvas");
        await canvas.FocusAsync();
        await page.Keyboard.PressAsync("ArrowDown");
        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Modeler" })).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByText("Non-canvas equivalent", new() { Exact = true })).ToBeVisibleAsync();
        await CaptureAsync(page, "139-lens-keyboard-outline.png");

        await Assertions.Expect(page.GetByLabel("Canvas zoom")).ToHaveTextAsync("100%");
        await canvas.HoverAsync();
        await page.Mouse.WheelAsync(0, -120);
        await Assertions.Expect(page.GetByLabel("Canvas zoom")).ToHaveTextAsync("110%");
        await canvas.FocusAsync();
        await page.Keyboard.PressAsync("-");
        await Assertions.Expect(page.GetByLabel("Canvas zoom")).ToHaveTextAsync("95%");
        await page.GetByRole(AriaRole.Button, new() { Name = "Fit scope" }).ClickAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Zoom in" }).ClickAsync();
        await Assertions.Expect(page.GetByLabel("Canvas zoom")).ToHaveTextAsync("115%");
        var pointerTransform = await page.Locator(".canvas-world").GetAttributeAsync("transform");
        await canvas.FocusAsync();
        await page.Keyboard.PressAsync("Shift+ArrowRight");
        var keyboardTransform = await page.Locator(".canvas-world").GetAttributeAsync("transform");
        Assert.That(keyboardTransform, Is.Not.EqualTo(pointerTransform), "Keyboard pan must move the same viewport used by pointer pan.");
        await page.Keyboard.PressAsync("f");
        await Assertions.Expect(page.GetByLabel("Canvas zoom")).ToHaveTextAsync("145%");
        await CaptureAsync(page, "177-canvas-fit-selection.png");

        await page.GetByRole(AriaRole.Button, new() { Name = "Fit scope" }).ClickAsync();
        var beforePointerPan = await page.Locator(".canvas-world").GetAttributeAsync("transform");
        var bounds = await canvas.BoundingBoxAsync();
        Assert.That(bounds, Is.Not.Null);
        await page.Mouse.MoveAsync(bounds!.X + bounds.Width * .45f, bounds.Y + bounds.Height * .78f);
        await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync(bounds.X + bounds.Width * .45f + 60, bounds.Y + bounds.Height * .78f + 35, new() { Steps = 4 });
        await page.Mouse.UpAsync();
        var afterPointerPan = await page.Locator(".canvas-world").GetAttributeAsync("transform");
        Assert.That(afterPointerPan, Is.Not.EqualTo(beforePointerPan), "Pointer pan must update presentation viewport state.");
        await CaptureAsync(page, "178-canvas-pointer-and-keyboard-pan.png");

        await page.GetByRole(AriaRole.Button, new() { Name = "Down" }).ClickAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Button, new() { Name = "Down" })).ToHaveAttributeAsync("aria-pressed", "true");
        await Assertions.Expect(page.GetByText("View state only", new() { Exact = true })).ToBeVisibleAsync();
        await CaptureAsync(page, "179-canvas-alignment-down.png");

        await page.GetByRole(AriaRole.Button, new() { Name = "Save current view" }).ClickAsync();
        await Assertions.Expect(page.GetByText("Personal view v1 saved; semantic revision remains r3.", new() { Exact = true })).ToBeVisibleAsync();
        await CaptureAsync(page, "182-canvas-personal-view-saved.png");
        await page.ReloadAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Button, new() { Name = "Down" })).ToHaveAttributeAsync("aria-pressed", "true");
        await Assertions.Expect(page.GetByText("Saved · v1", new() { Exact = true })).ToBeVisibleAsync();
        await CaptureAsync(page, "183-canvas-personal-view-restored.png");

        await page.GetByRole(AriaRole.Button, new() { Name = "Team" }).ClickAsync();
        await Assertions.Expect(page.GetByText("Not saved", new() { Exact = true })).ToBeVisibleAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Save current view" }).ClickAsync();
        await Assertions.Expect(page.GetByText("Team view v1 saved; semantic revision remains r3.", new() { Exact = true })).ToBeVisibleAsync();
        await CaptureAsync(page, "184-canvas-team-view-isolated.png");
        await page.GetByRole(AriaRole.Button, new() { Name = "Personal" }).ClickAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Button, new() { Name = "Down" })).ToHaveAttributeAsync("aria-pressed", "true");
        await page.GetByRole(AriaRole.Button, new() { Name = "Reset" }).ClickAsync();
        await Assertions.Expect(page.GetByText("Personal view reset to deterministic auto-layout. Semantic revision is unchanged.", new() { Exact = true })).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByText("Not saved", new() { Exact = true })).ToBeVisibleAsync();
        var afterViewReset = await api.GetFromJsonAsync<ProjectModelResponse>($"/api/v1/projects/{project.Id}/model");
        Assert.That(afterViewReset!.Project.Revision, Is.EqualTo(3));
        await CaptureAsync(page, "185-canvas-personal-view-reset.png");

        await page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("Modeler") }).First.ClickAsync();
        await canvas.FocusAsync();
        await page.Keyboard.PressAsync("Enter");
        await Assertions.Expect(page.GetByRole(AriaRole.Navigation, new() { Name = "Lens scope breadcrumb" })).ToContainTextAsync("Modeler");
        await Assertions.Expect(page.GetByTestId("pinned-scope-context")).ToContainTextAsync("Lens evidence project");
        await Assertions.Expect(page.GetByTestId("cross-scope-dock")).ToBeVisibleAsync();
        await Assertions.Expect(page.Locator("[data-canvas-node]")).ToHaveCountAsync(1);
        Assert.That(page.Url, Does.Contain($"scope={actor.Id}"));
        await CaptureAsync(page, "188-lens-actor-scope.png");

        await page.GetByTestId("cross-scope-dock").GetByRole(AriaRole.Button, new() { NameRegex = new Regex("Topology is traceable") }).ClickAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Navigation, new() { Name = "Lens scope breadcrumb" })).ToContainTextAsync("Topology is traceable");
        await CaptureAsync(page, "189-lens-cross-scope-open.png");
        await page.GoBackAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Navigation, new() { Name = "Lens scope breadcrumb" })).ToContainTextAsync("Modeler");
        await CaptureAsync(page, "190-lens-browser-back.png");
        await page.GoForwardAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Navigation, new() { Name = "Lens scope breadcrumb" })).ToContainTextAsync("Topology is traceable");
        await CaptureAsync(page, "191-lens-browser-forward.png");
        await page.Context.CloseAsync();

        var darkPage = await NewPageAsync(ColorScheme.Dark);
        await darkPage.GotoAsync(url);
        await Assertions.Expect(darkPage.GetByTestId("lens-canvas")).ToBeVisibleAsync();
        await CaptureAsync(darkPage, "140-lens-lab-dark.png");
        await CaptureAsync(darkPage, "180-canvas-kernel-dark.png");
        await darkPage.GetByRole(AriaRole.Button, new() { Name = "Team" }).ClickAsync();
        await Assertions.Expect(darkPage.GetByText("Saved · v1", new() { Exact = true })).ToBeVisibleAsync();
        await CaptureAsync(darkPage, "186-canvas-team-view-dark.png");
        await darkPage.GotoAsync($"{url}?scope={actor.Id}");
        await Assertions.Expect(darkPage.GetByTestId("pinned-scope-context")).ToBeVisibleAsync();
        await CaptureAsync(darkPage, "192-lens-deep-link-dark.png");
        await darkPage.Context.CloseAsync();

        var narrowPage = await NewPageAsync(ColorScheme.Light, 620, 900);
        await narrowPage.GotoAsync(url);
        await Assertions.Expect(narrowPage.GetByRole(AriaRole.Heading, new() { Name = "Structured topology" })).ToBeVisibleAsync();
        await CaptureAsync(narrowPage, "141-lens-lab-responsive.png");
        await CaptureAsync(narrowPage, "181-canvas-kernel-responsive.png");
        await CaptureAsync(narrowPage, "187-canvas-view-memory-responsive.png");
        await narrowPage.GotoAsync($"{url}?scope={actor.Id}");
        await Assertions.Expect(narrowPage.GetByRole(AriaRole.Heading, new() { Name = "Cross-scope stubs" })).ToBeVisibleAsync();
        await CaptureAsync(narrowPage, "193-lens-deep-link-responsive.png");
        await narrowPage.GotoAsync($"{url}?scope=0198ad00-0000-7000-8000-000000009999");
        await Assertions.Expect(narrowPage.GetByRole(AriaRole.Alert)).ToContainTextAsync("That scope is not in this revision.");
        await CaptureAsync(narrowPage, "194-lens-invalid-scope-recovery.png");
        await narrowPage.GetByRole(AriaRole.Button, new() { Name = "Recover to root" }).ClickAsync();
        await Assertions.Expect(narrowPage.GetByRole(AriaRole.Navigation, new() { Name = "Lens scope breadcrumb" })).ToContainTextAsync("root scope");
        await narrowPage.Context.CloseAsync();
    }

    [Test]
    [Property("ModelClaim", "0198ad00-0000-7000-8000-000000000235")]
    public async Task Given_outcome_truth_when_a_modeler_shapes_an_ability_and_story_then_the_map_traces_value_to_scene_without_invented_structure()
    {
        using var createRequest = CreateApiRequest("0198ad00-0000-7000-8000-000000000700",
            "0198ad00-0000-7000-8000-000000000760", new CreateProjectRequest("Story Map evidence",
                "Trace repository value into explicit abilities and concrete behavior.",
                "A contributor can build, run, and verify the repository.", "Create the E02 evidence project."));
        using var createResponse = await api!.SendAsync(createRequest);
        var project = await createResponse.Content.ReadFromJsonAsync<ProjectResponse>();
        Assert.That(createResponse.StatusCode, Is.EqualTo(HttpStatusCode.Created));

        using var contributorRequest = CreateEditApiRequest($"/api/v1/projects/{project!.Id}/actors", "1",
            "0198ad00-0000-7000-8000-000000000761", new AddActorRequest("Contributor", "humanRole",
                "Builds and verifies the repository.", "Reach a usable foundation", "Run repository verification",
                "May execute local development commands", "Cannot bypass architecture checks", "Define the story initiator.", "known"));
        using var contributorResponse = await api.SendAsync(contributorRequest);
        var contributor = (await contributorResponse.Content.ReadFromJsonAsync<AddActorResponse>())!.Actor;
        Assert.That(contributorResponse.StatusCode, Is.EqualTo(HttpStatusCode.Created));

        using var studioRequest = CreateEditApiRequest($"/api/v1/projects/{project.Id}/actors", "2",
            "0198ad00-0000-7000-8000-000000000762", new AddActorRequest("Project Builder", "systemRole",
                "Presents model and evidence.", "Make modeled truth inspectable", "Project semantic observations",
                "May present canonical definitions", "Cannot treat layout as truth", "Define the receiving participant.", "known"));
        using var studioResponse = await api.SendAsync(studioRequest);
        var studio = (await studioResponse.Content.ReadFromJsonAsync<AddActorResponse>())!.Actor;
        Assert.That(studioResponse.StatusCode, Is.EqualTo(HttpStatusCode.Created));

        using var outcomeRequest = CreateEditApiRequest($"/api/v1/projects/{project.Id}/outcomes", "3",
            "0198ad00-0000-7000-8000-000000000763", new AddOutcomeRequest("Repository verification is repeatable",
                "A contributor can build, run, and verify a clean clone through one documented path.",
                "Build passes\nArchitecture tests pass\nHealth becomes ready", contributor.Id,
                "Anchor the Story Map in observable value.", "known"));
        using var outcomeResponse = await api.SendAsync(outcomeRequest);
        var outcome = (await outcomeResponse.Content.ReadFromJsonAsync<AddOutcomeResponse>())!.Outcome;
        Assert.That(outcomeResponse.StatusCode, Is.EqualTo(HttpStatusCode.Created));

        var page = await NewPageAsync();
        var url = $"{baseUrl}/projects/{project.Id}/story-map";
        await page.GotoAsync(url);
        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Follow value into behavior" })).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByText("story-map.capability.missing", new() { Exact = true })).ToBeVisibleAsync();
        await CaptureAsync(page, "142-story-map-explicit-gap.png");

        await page.GetByRole(AriaRole.Button, new() { Name = "+ Shape capability" }).ClickAsync();
        var deck = page.GetByTestId("capability-deck");
        await deck.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("Repository verification is repeatable") }).ClickAsync();
        await deck.GetByLabel("Capability name").FillAsync("Verify repository foundation");
        await deck.GetByLabel("Ability statement").FillAsync("Build, run, and verify one repository path with deterministic evidence.");
        await deck.GetByRole(AriaRole.Radio, new() { Name = "Critical" }).ClickAsync();
        await deck.GetByLabel("Audit reason").FillAsync("Connect contributor value to an explicit ability before mapping workflow.");
        await Assertions.Expect(deck.GetByRole(AriaRole.Button, new() { Name = "Commit capability" })).ToBeEnabledAsync();
        await CaptureAsync(page, "143-story-map-capability-staged.png");
        await deck.GetByRole(AriaRole.Button, new() { Name = "Commit capability" }).ClickAsync();
        await Assertions.Expect(page.GetByTestId("story-map-canvas")).ToContainTextAsync("Verify repository foundation");
        await Assertions.Expect(deck).ToBeHiddenAsync();

        using var narrativeRequest = CreateEditApiRequest($"/api/v1/projects/{project.Id}/narratives", "5",
            "0198ad00-0000-7000-8000-000000000764", new DefineNarrativeRequest(outcome.Id,
                [contributor.Id, studio.Id], contributor.Id, studio.Id, "Bootstrap Repository",
                "A contributor has a clean clone and required local prerequisites.",
                "The repository builds, tests, runs, and exposes healthy evidence.", "Clean clone is built and run", "Happy",
                "A clean clone exists\nDocker is available\nThe approved .NET SDK is installed",
                "The contributor runs the documented repository command.", "Build, tests, and health evidence are visible.",
                "Verify local foundation", "Contributor workstation and local runtime", "Execute and inspect repository verification",
                "Run repository verification", "Prove the foundation is usable", "Restore, build, test, start, and inspect health.",
                "The contributor sees passing checks and a ready application.", "BuildPassed\nArchitecturePassed\nHealthReady",
                "Map the E02 repository story."));
        using var narrativeResponse = await api.SendAsync(narrativeRequest);
        Assert.That(narrativeResponse.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        await page.ReloadAsync();
        await Assertions.Expect(page.GetByTestId("story-map-canvas")).ToContainTextAsync("Bootstrap Repository");
        await Assertions.Expect(page.GetByTestId("story-map-canvas")).ToContainTextAsync("Clean clone is built and run");
        await CaptureAsync(page, "144-story-map-complete-light.png");
        await page.Locator("[data-story-node]").Filter(new() { HasText = "Clean clone is built and run" }).ClickAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Clean clone is built and run" })).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByText("Trace ribbon", new() { Exact = true })).ToBeVisibleAsync();
        await CaptureAsync(page, "145-story-map-scenario-inspector.png");
        await page.GetByRole(AriaRole.Button, new() { Name = "◆ Priority" }).ClickAsync();
        await Assertions.Expect(page.GetByText("Priority · critical", new() { Exact = true })).ToHaveCountAsync(0);
        await page.GetByLabel("Visual Story Map").FocusAsync();
        await page.Keyboard.PressAsync("End");
        await CaptureAsync(page, "146-story-map-overlay-keyboard.png");
        await page.Context.CloseAsync();

        var first = await api.GetStringAsync($"/api/v1/projects/{project.Id}/lenses/story-map?overlay=priority,status");
        var second = await api.GetStringAsync($"/api/v1/projects/{project.Id}/lenses/story-map?overlay=status,priority");
        Assert.That(second, Is.EqualTo(first));

        var darkPage = await NewPageAsync(ColorScheme.Dark);
        await darkPage.GotoAsync(url);
        await Assertions.Expect(darkPage.GetByTestId("story-map-canvas")).ToContainTextAsync("Verify repository foundation");
        await CaptureAsync(darkPage, "147-story-map-dark.png");
        await darkPage.Context.CloseAsync();

        var narrowPage = await NewPageAsync(ColorScheme.Light, 620, 900);
        await narrowPage.GotoAsync(url);
        await Assertions.Expect(narrowPage.GetByRole(AriaRole.Heading, new() { Name = "Value-to-scene trace" })).ToBeVisibleAsync();
        await CaptureAsync(narrowPage, "148-story-map-responsive.png");
        await narrowPage.Context.CloseAsync();

        var model = await api.GetFromJsonAsync<ProjectModelResponse>($"/api/v1/projects/{project.Id}/model");
        Assert.Multiple(() =>
        {
            Assert.That(model!.Project.Revision, Is.EqualTo(6));
            Assert.That(model.Capabilities, Has.Count.EqualTo(1));
            Assert.That(model.Capabilities!.Single().OutcomeIds, Does.Contain(outcome.Id));
            Assert.That(model.Narratives.Single().OutcomeId, Is.EqualTo(outcome.Id));
        });
    }

    [Test]
    [Property("ModelClaim", "0198ad00-0000-7000-8000-000000000236")]
    [Property("ModelClaim", "0198ad00-0000-7000-8000-000000000247")]
    public async Task Given_a_typed_scenario_and_failure_path_when_flow_is_opened_then_playback_preserves_participants_branches_boundaries_and_results()
    {
        using var projectRequest = CreateApiRequest("0198ad00-0000-7000-8000-000000000700",
            "0198ad00-0000-7000-8000-000000000771", new CreateProjectRequest(
                "Scenario Flow evidence", "Explain authored behavior and recovery without executing production code.",
                "A reviewer can play every modeled path from trigger to semantic result.", "Create the E03 flow evidence project."));
        using var projectResponse = await api!.SendAsync(projectRequest);
        Assert.That(projectResponse.StatusCode, Is.EqualTo(HttpStatusCode.Created), await projectResponse.Content.ReadAsStringAsync());
        var project = (await projectResponse.Content.ReadFromJsonAsync<ProjectResponse>())!;

        using var modelerRequest = CreateEditApiRequest($"/api/v1/projects/{project.Id}/actors", "1",
            "0198ad00-0000-7000-8000-000000000772", new AddActorRequest("Modeler", "humanRole",
                "Requests repository verification.", "Verify the foundation", "Express verification intent",
                "May request verification", "Cannot bypass validation", "Create the initiating flow participant."));
        using var modelerResponse = await api.SendAsync(modelerRequest);
        var modeler = (await modelerResponse.Content.ReadFromJsonAsync<AddActorResponse>())!.Actor;
        using var studioRequest = CreateEditApiRequest($"/api/v1/projects/{project.Id}/actors", "2",
            "0198ad00-0000-7000-8000-000000000773", new AddActorRequest("Project Builder", "systemRole",
                "Validates and verifies the project.", "Produce inspectable evidence", "Run verification",
                "May validate definitions", "Must preserve current truth", "Create the receiving flow participant."));
        using var studioResponse = await api.SendAsync(studioRequest);
        var studio = (await studioResponse.Content.ReadFromJsonAsync<AddActorResponse>())!.Actor;
        using var outcomeRequest = CreateEditApiRequest($"/api/v1/projects/{project.Id}/outcomes", "3",
            "0198ad00-0000-7000-8000-000000000774", new AddOutcomeRequest("Repository evidence is visible",
                "A reviewer sees build, test, and health evidence for the current definition.",
                "Build passes\nTests pass\nHealth is ready", modeler.Id, "Anchor E03 playback in observable value."));
        using var outcomeResponse = await api.SendAsync(outcomeRequest);
        var outcome = (await outcomeResponse.Content.ReadFromJsonAsync<AddOutcomeResponse>())!.Outcome;
        using var narrativeRequest = CreateEditApiRequest($"/api/v1/projects/{project.Id}/narratives", "4",
            "0198ad00-0000-7000-8000-000000000775", new DefineNarrativeRequest(outcome.Id,
                [modeler.Id, studio.Id], modeler.Id, studio.Id, "Bootstrap Repository",
                "A clean clone and required local prerequisites exist.", "Repository evidence is visible.",
                "Clean clone is built and run", "Happy", "Repository cloned\nDocker available\nApproved SDK installed",
                "The modeler requests repository verification.", "Build, tests, and health evidence are visible.",
                "Verify local foundation", "Contributor workstation", "Execute and inspect repository verification",
                "Run repository verification", "Prove the foundation is usable", "Run the documented verification command.",
                "The modeler sees passing checks and ready health.", "Verified\nInvalid", "Define the E03 primary scenario."));
        using var narrativeResponse = await api.SendAsync(narrativeRequest);
        var narrative = (await narrativeResponse.Content.ReadFromJsonAsync<DefineNarrativeResponse>())!.Narrative;
        using var stateRequest = CreateEditApiRequest($"/api/v1/projects/{project.Id}/state-logic", "5",
            "0198ad00-0000-7000-8000-000000000776", new DefineStateLogicRequest(
                "Definition verification", "Domain", "DefinitionStatus\nEvidenceStatus", "Valid\nInvalid", studio.Id,
                "Definition is valid", "boolean", "The model owns validation truth.", "Transitioned",
                "Definition acceptance", "Validation", "Only a valid definition can produce verified evidence.", studio.Id,
                "Invalid definitions do not advance", "Rejected verification preserves current semantic state.",
                "An invalid definition advances revision.", "Invalid example\nRetry property",
                "Verified | Success | Evidence is available.\nInvalid | Invalid | Findings are returned without mutation.",
                "Verify project definition", "The definition awaits verification.", "Verification is requested.",
                "Evidence is visible or findings are returned.", "Define results used by Scenario Flow paths."));
        using var stateResponse = await api.SendAsync(stateRequest);
        var model = await api.GetFromJsonAsync<ProjectModelResponse>($"/api/v1/projects/{project.Id}/model");
        var state = model!.StateLogic.Single();
        using var pathRequest = CreateEditApiRequest($"/api/v1/projects/{project.Id}/paths", "6",
            "0198ad00-0000-7000-8000-000000000777", new DefinePathRequest(narrative.ScenarioId,
                state.TransitionId, state.Results.Single(result => result.Kind == "invalid").Id,
                state.Results.Single(result => result.Kind == "success").Id, modeler.Id,
                "Invalid project definition", "Exceptional", "Definition is invalid", "Branch",
                "One or more semantic fields fail validation.", state.FactId, state.RuleId,
                "Reject the invalid definition\nPreserve the contributor draft\nPresent actionable findings",
                "Current semantic state is unchanged.", "The modeler sees field-level findings.",
                "Publish validation findings", "ExternalInteraction", "Return safe findings across the application boundary.",
                "Correct and retry", "CorrectAndRetry", "Modeler chooses to correct",
                "The rejected meaning has been corrected.", "Correct invalid fields\nRetry with a new operation identity",
                "Valid meaning is eligible for verification.", "The modeler sees verified evidence.",
                "Retry only after correction.", "Operation identity prevents duplicate semantic effects.",
                "Stop after verification or explicit cancellation.", "No partial semantic write remains after rejection.",
                "Define the exceptional and recovery flow."));
        using var pathResponse = await api.SendAsync(pathRequest);
        Assert.Multiple(() =>
        {
            Assert.That(projectResponse.StatusCode, Is.EqualTo(HttpStatusCode.Created));
            Assert.That(narrativeResponse.StatusCode, Is.EqualTo(HttpStatusCode.Created));
            Assert.That(stateResponse.StatusCode, Is.EqualTo(HttpStatusCode.Created));
            Assert.That(pathResponse.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        });

        var url = $"{baseUrl}/projects/{project.Id}/scenarios/{narrative.ScenarioId}/flow";
        var page = await NewPageAsync();
        await page.GotoAsync(url);
        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Clean clone is built and run" })).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByTestId("scenario-flow-canvas")).ToContainTextAsync("Run repository verification");
        await Assertions.Expect(page.GetByTestId("scenario-overlay")).ToContainTextAsync("No invariant is explicitly linked to the primary narrative route.");
        await CaptureAsync(page, "195-scenario-overlay-primary.png");
        await CaptureAsync(page, "149-scenario-flow-primary.png");
        await page.GetByTestId("scenario-flow-canvas").FocusAsync();
        await page.Keyboard.PressAsync("ArrowRight");
        await page.Keyboard.PressAsync("ArrowRight");
        await Assertions.Expect(page.GetByTestId("scenario-flow-inspector")).ToContainTextAsync("Intent");
        await page.GetByRole(AriaRole.Button, new() { Name = "Play path" }).ClickAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Button, new() { Name = "Pause" })).ToBeVisibleAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Pause" }).ClickAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Button, new() { Name = "Play path" })).ToBeVisibleAsync();
        await CaptureAsync(page, "150-scenario-flow-keyboard-playback.png");

        await page.GetByRole(AriaRole.Tab, new() { NameRegex = new Regex("Invalid project definition") }).ClickAsync();
        await Assertions.Expect(page.GetByTestId("scenario-flow-canvas")).ToContainTextAsync("Explicit external-interaction boundary");
        await Assertions.Expect(page.GetByTestId("scenario-flow-canvas")).ToContainTextAsync("Publish validation findings");
        await Assertions.Expect(page.GetByTestId("scenario-overlay")).ToContainTextAsync("Rejected verification preserves current semantic state.");
        await Assertions.Expect(page.GetByTestId("scenario-overlay")).ToContainTextAsync("The modeler sees field-level findings.");
        await CaptureAsync(page, "151-scenario-flow-exception-boundary.png");
        await page.GetByRole(AriaRole.Button, new() { Name = "Play path" }).ClickAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Alert)).ToContainTextAsync("Playback stopped at the modeled invariant");
        await CaptureAsync(page, "196-scenario-overlay-invariant-stop.png");
        await page.GetByRole(AriaRole.Button, new() { Name = "Continue after review" }).ClickAsync();
        await Assertions.Expect(page.Locator(".playback-transcript li.current")).ToContainTextAsync("Invalid");
        await Assertions.Expect(page.GetByTestId("scenario-overlay")).ToContainTextAsync("Current semantic state is unchanged.");
        await CaptureAsync(page, "152-scenario-flow-terminal-playback.png");
        await CaptureAsync(page, "197-scenario-overlay-terminal-state.png");

        await page.GetByRole(AriaRole.Tab, new() { NameRegex = new Regex("Correct and retry") }).ClickAsync();
        await Assertions.Expect(page.GetByTestId("scenario-flow-canvas")).ToContainTextAsync("Retry with a new operation identity");
        await Assertions.Expect(page.GetByTestId("scenario-overlay")).ToContainTextAsync("Valid meaning is eligible for verification.");
        await CaptureAsync(page, "153-scenario-flow-recovery.png");
        await CaptureAsync(page, "198-scenario-overlay-recovery.png");
        var first = await api.GetStringAsync($"/api/v1/projects/{project.Id}/lenses/scenario-flow/{narrative.ScenarioId}");
        var second = await api.GetStringAsync($"/api/v1/projects/{project.Id}/lenses/scenario-flow/{narrative.ScenarioId}");
        Assert.That(second, Is.EqualTo(first));

        var darkPage = await NewPageAsync(ColorScheme.Dark);
        await darkPage.GotoAsync(url);
        await Assertions.Expect(darkPage.GetByTestId("scenario-flow-canvas")).ToBeVisibleAsync();
        await CaptureAsync(darkPage, "154-scenario-flow-dark.png");
        await CaptureAsync(darkPage, "199-scenario-overlay-dark.png");
        await darkPage.Context.CloseAsync();
        var narrowPage = await NewPageAsync(ColorScheme.Light, 620, 900);
        await narrowPage.GotoAsync(url);
        await Assertions.Expect(narrowPage.GetByRole(AriaRole.Heading, new() { Name = "All nodes in deterministic order" })).ToBeVisibleAsync();
        await CaptureAsync(narrowPage, "155-scenario-flow-responsive.png");
        await CaptureAsync(narrowPage, "200-scenario-overlay-responsive.png");
        await narrowPage.Context.CloseAsync();

        var stored = await api.GetFromJsonAsync<ProjectModelResponse>($"/api/v1/projects/{project.Id}/model");
        Assert.Multiple(() =>
        {
            Assert.That(stored!.Project.Revision, Is.EqualTo(7));
            Assert.That(stored.Narratives.Single().InteractionId, Is.Not.Empty);
            Assert.That(stored.Paths.Single().ScenarioId, Is.EqualTo(narrative.ScenarioId));
            Assert.That(stored.Paths.Single().EffectKind, Is.EqualTo("externalInteraction"));
        });
    }

    [Test]
    [Property("ModelClaim", "0198ad00-0000-7000-8000-000000000237")]
    public async Task Given_typed_state_and_logic_when_the_lens_is_opened_then_graph_matrices_invariants_unknowns_and_themes_stay_synchronized()
    {
        using var projectRequest = CreateApiRequest("0198ad00-0000-7000-8000-000000000700",
            "0198ad00-0000-7000-8000-000000000781", new CreateProjectRequest(
                "State and Rule evidence", "Explain why modeled transitions are valid without reading raw records.",
                "A reviewer can trace state, facts, rules, invariants, and results in equivalent visual forms.",
                "Create the E04 State and Rule evidence project."));
        using var projectResponse = await api!.SendAsync(projectRequest);
        Assert.That(projectResponse.StatusCode, Is.EqualTo(HttpStatusCode.Created), await projectResponse.Content.ReadAsStringAsync());
        var project = (await projectResponse.Content.ReadFromJsonAsync<ProjectResponse>())!;
        using var actorRequest = CreateEditApiRequest($"/api/v1/projects/{project.Id}/actors", "1",
            "0198ad00-0000-7000-8000-000000000782", new AddActorRequest("Domain reviewer", "humanRole",
                "Owns acceptance truth for project definitions.", "Protect accepted model truth", "Review definition transitions",
                "May approve modeled acceptance rules", "Cannot bypass revision invariants", "Add the E04 state authority."));
        using var actorResponse = await api.SendAsync(actorRequest);
        Assert.That(actorResponse.StatusCode, Is.EqualTo(HttpStatusCode.Created), await actorResponse.Content.ReadAsStringAsync());
        var actor = (await actorResponse.Content.ReadFromJsonAsync<AddActorResponse>())!.Actor;
        using var stateRequest = CreateEditApiRequest($"/api/v1/projects/{project.Id}/state-logic", "2",
            "0198ad00-0000-7000-8000-000000000783", new DefineStateLogicRequest(
                "Project definition lifecycle", "Domain", "DefinitionStatus\nRevision\nEvidenceStatus", "Unmodeled\nDefined\nVerified", actor.Id,
                "Definition acceptance", "DefinitionStatus", "The project aggregate owns accepted definition status.", "Transitioned",
                "Definition is acceptable", "Validation", "Required semantic fields are valid before acceptance.", actor.Id,
                "Accepted revision advances exactly once", "One accepted operation advances exactly one canonical revision.",
                "One accepted operation advances two revisions.", "Transition example\nIdempotent retry property\nPostgreSQL concurrency proof",
                "Accepted | Success | The definition becomes canonical.\nInvalid | Invalid | Findings are returned and state remains unchanged.\nConflict | Conflict | Stale truth is never overwritten.",
                "Accept project definition", "No accepted definition exists at the expected revision.",
                "The reviewer submits a valid definition.", "One accepted definition exists at the next revision.",
                "Define the E04 transition and assurance packet."));
        using var stateResponse = await api.SendAsync(stateRequest);
        Assert.That(stateResponse.StatusCode, Is.EqualTo(HttpStatusCode.Created), await stateResponse.Content.ReadAsStringAsync());
        var state = (await stateResponse.Content.ReadFromJsonAsync<DefineStateLogicResponse>())!.Definitions;
        Assert.Multiple(() =>
        {
            Assert.That(projectResponse.StatusCode, Is.EqualTo(HttpStatusCode.Created));
            Assert.That(actorResponse.StatusCode, Is.EqualTo(HttpStatusCode.Created));
            Assert.That(stateResponse.StatusCode, Is.EqualTo(HttpStatusCode.Created));
            Assert.That(state.InvariantId, Is.Not.Empty);
            Assert.That(state.FactAllowedKnowledge, Does.Contain("unknown"));
        });

        var url = $"{baseUrl}/projects/{project.Id}/states/{state.StateId}/rules";
        var page = await NewPageAsync();
        await page.GotoAsync(url);
        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Project definition lifecycle" })).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByTestId("state-rule-graph")).ToContainTextAsync("Accept project definition");
        await Assertions.Expect(page.GetByTestId("state-rule-studio")).ToContainTextAsync("unknown-capable");
        await CaptureAsync(page, "156-state-rule-causal-graph.png");
        await page.GetByTestId("state-rule-studio").FocusAsync();
        await page.Keyboard.PressAsync("ArrowRight");
        await Assertions.Expect(page.GetByTestId("state-rule-inspector")).ToContainTextAsync("Accept project definition");
        await CaptureAsync(page, "157-state-rule-keyboard-inspector.png");

        await page.GetByTestId("state-rule-view-transition-matrix").ClickAsync();
        await Assertions.Expect(page.GetByTestId("state-transition-matrix")).ToContainTextAsync("Stale truth is never overwritten");
        await CaptureAsync(page, "158-state-rule-transition-matrix.png");
        await page.GetByTestId("state-rule-view-rule-matrix").ClickAsync();
        await Assertions.Expect(page.GetByTestId("state-rule-matrix")).ToContainTextAsync("Evaluation boundary");
        await CaptureAsync(page, "159-state-rule-decision-table.png");
        await page.GetByTestId("state-rule-view-invariant-panel").ClickAsync();
        await Assertions.Expect(page.GetByTestId("state-invariant-panel")).ToContainTextAsync("Idempotent retry property");
        await CaptureAsync(page, "160-state-rule-invariant-proof.png");

        var first = await api.GetStringAsync($"/api/v1/projects/{project.Id}/lenses/state-rule/{state.StateId}");
        var second = await api.GetStringAsync($"/api/v1/projects/{project.Id}/lenses/state-rule/{state.StateId}");
        Assert.That(second, Is.EqualTo(first));
        var darkPage = await NewPageAsync(ColorScheme.Dark);
        await darkPage.GotoAsync(url);
        await Assertions.Expect(darkPage.GetByTestId("state-rule-graph")).ToBeVisibleAsync();
        await CaptureAsync(darkPage, "161-state-rule-dark.png");
        await darkPage.Context.CloseAsync();
        var narrowPage = await NewPageAsync(ColorScheme.Light, 620, 900);
        await narrowPage.GotoAsync(url);
        await Assertions.Expect(narrowPage.GetByRole(AriaRole.Heading, new() { Name = "All elements in deterministic order" })).ToBeVisibleAsync();
        await CaptureAsync(narrowPage, "162-state-rule-responsive.png");
        await narrowPage.Context.CloseAsync();
    }

    [Test]
    [Property("ModelClaim", "0198ad00-0000-7000-8000-000000000238")]
    public async Task Given_two_accountable_authorities_when_a_context_is_composed_then_the_crossing_contract_and_overlays_remain_explicit()
    {
        using var projectRequest = CreateApiRequest("0198ad00-0000-7000-8000-000000000700",
            "0198ad00-0000-7000-8000-000000000791", new CreateProjectRequest(
                "System Context evidence", "Make owned and external authority inspectable.",
                "A reviewer can trace interface intent, boundary kinds, and declared data movement.",
                "Create the E05 system-context evidence project."));
        using var projectResponse = await api!.SendAsync(projectRequest);
        var project = (await projectResponse.Content.ReadFromJsonAsync<ProjectResponse>())!;
        using var ownedRequest = CreateEditApiRequest($"/api/v1/projects/{project.Id}/actors", "1",
            "0198ad00-0000-7000-8000-000000000792", new AddActorRequest("Modeler", "humanRole", "Owns model truth.",
                "Inspect accountable boundaries", "Define the system context", "May commit semantic context", "Cannot invent external authority", "Add owned authority."));
        using var ownedResponse = await api.SendAsync(ownedRequest);
        using var externalRequest = CreateEditApiRequest($"/api/v1/projects/{project.Id}/actors", "2",
            "0198ad00-0000-7000-8000-000000000793", new AddActorRequest("Data owner", "externalProviderRole", "Owns persistence authority.",
                "Protect durable records", "Operate PostgreSQL", "Controls storage availability", "Cannot alter semantic meaning", "Add external authority."));
        using var externalResponse = await api.SendAsync(externalRequest);
        Assert.Multiple(() => { Assert.That(projectResponse.StatusCode, Is.EqualTo(HttpStatusCode.Created)); Assert.That(ownedResponse.StatusCode, Is.EqualTo(HttpStatusCode.Created)); Assert.That(externalResponse.StatusCode, Is.EqualTo(HttpStatusCode.Created)); });

        var page = await NewPageAsync();
        await page.GotoAsync($"{baseUrl}/projects/{project.Id}/system-contexts/new");
        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Define the crossing, not a box diagram" })).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByText("One crossing · five definitions")).ToBeVisibleAsync();
        await CaptureAsync(page, "163-system-context-composer.png");
        await page.GetByRole(AriaRole.Button, new() { Name = "Commit context packet" }).ClickAsync();
        await Assertions.Expect(page.GetByTestId("system-context-map")).ToBeVisibleAsync(new() { Timeout = 15_000 });
        await Assertions.Expect(page.GetByTestId("system-context-map")).ToContainTextAsync("Project Builder");
        await Assertions.Expect(page.GetByText("Explicit movement, never inferred")).ToBeVisibleAsync();
        await CaptureAsync(page, "164-system-context-ownership.png");
        await page.GetByTestId("system-context-studio").FocusAsync();
        await page.Keyboard.PressAsync("End");
        await Assertions.Expect(page.GetByTestId("system-context-inspector")).ToContainTextAsync("PostgreSQL");
        await CaptureAsync(page, "165-system-context-keyboard-inspector.png");
        await page.GetByRole(AriaRole.Button, new() { Name = "trust", Exact = true }).ClickAsync();
        await Assertions.Expect(page.GetByTestId("system-context-map")).ToContainTextAsync("outside trust boundary");
        await CaptureAsync(page, "166-system-context-trust-overlay.png");
        var stored = await api.GetFromJsonAsync<ProjectModelResponse>($"/api/v1/projects/{project.Id}/model");
        var context = stored!.SystemContexts!.Single();
        var first = await api.GetStringAsync($"/api/v1/projects/{project.Id}/lenses/system-context/{context.OwnedSystemId}?overlay=trust");
        var second = await api.GetStringAsync($"/api/v1/projects/{project.Id}/lenses/system-context/{context.OwnedSystemId}?overlay=trust");
        Assert.That(second, Is.EqualTo(first));
        var darkPage = await NewPageAsync(ColorScheme.Dark);
        await darkPage.GotoAsync($"{baseUrl}/projects/{project.Id}/systems/{context.OwnedSystemId}/context");
        await Assertions.Expect(darkPage.GetByTestId("system-context-map")).ToBeVisibleAsync();
        await CaptureAsync(darkPage, "167-system-context-dark.png"); await darkPage.Context.CloseAsync();
        var narrowPage = await NewPageAsync(ColorScheme.Light, 620, 900);
        await narrowPage.GotoAsync($"{baseUrl}/projects/{project.Id}/systems/{context.OwnedSystemId}/context");
        await Assertions.Expect(narrowPage.GetByRole(AriaRole.Heading, new() { Name = "Deterministic topology outline" })).ToBeVisibleAsync();
        await CaptureAsync(narrowPage, "168-system-context-responsive.png"); await narrowPage.Context.CloseAsync();
        Assert.Multiple(() => { Assert.That(stored.Project.Revision, Is.EqualTo(4)); Assert.That(context.BoundaryKinds, Does.Contain("trust")); Assert.That(context.RequestData, Is.EqualTo("Typed change set")); });
    }

    [Test]
    [Property("ModelClaim", "0198ad00-0000-7000-8000-000000000239")]
    public async Task Given_a_material_outcome_when_proof_is_attached_then_trace_debt_and_change_impact_remain_attributable()
    {
        using var projectRequest = CreateApiRequest("0198ad00-0000-7000-8000-000000000700",
            "0198ad00-0000-7000-8000-000000000801", new CreateProjectRequest("Traceability evidence",
                "Connect promised value to reviewable claims and attributable proof.",
                "A reviewer can see missing, current, and impacted evidence without a completeness percentage.",
                "Create the E06 Traceability Atlas project."));
        using var projectResponse = await api!.SendAsync(projectRequest); var project = (await projectResponse.Content.ReadFromJsonAsync<ProjectResponse>())!;
        using var actorRequest = CreateEditApiRequest($"/api/v1/projects/{project.Id}/actors", "1",
            "0198ad00-0000-7000-8000-000000000802", new AddActorRequest("Evidence reviewer", "humanRole",
                "Owns claim sufficiency review.", "Trace value to proof", "Review limitations and freshness",
                "May accept attributable evidence", "Cannot treat test count as proof", "Add the E06 evidence authority."));
        using var actorResponse = await api.SendAsync(actorRequest); var actor = (await actorResponse.Content.ReadFromJsonAsync<AddActorResponse>())!.Actor;
        using var outcomeRequest = CreateEditApiRequest($"/api/v1/projects/{project.Id}/outcomes", "2",
            "0198ad00-0000-7000-8000-000000000803", new AddOutcomeRequest("Repository verification is trustworthy",
                "A contributor runs one command and sees attributable build, test, and health evidence.",
                "Verification passes\nHealth is ready\nEvidence names its limitations", actor.Id, "Add the E06 material outcome."));
        using var outcomeResponse = await api.SendAsync(outcomeRequest); var outcome = (await outcomeResponse.Content.ReadFromJsonAsync<AddOutcomeResponse>())!.Outcome;
        Assert.That(outcomeResponse.StatusCode, Is.EqualTo(HttpStatusCode.Created));

        var page = await NewPageAsync(); await page.GotoAsync($"{baseUrl}/projects/{project.Id}/traceability");
        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Name = "From promised value to proof" })).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByTestId("trace-outcome-river")).ToContainTextAsync("No first-class claim references this outcome");
        await CaptureAsync(page, "169-traceability-missing-link.png");
        await page.GetByRole(AriaRole.Link, new() { Name = "shape the claim" }).ClickAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Make one proof accountable" })).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByText("Definition", new() { Exact = true })).ToBeVisibleAsync();
        await CaptureAsync(page, "170-evidence-packet-composer.png");
        await page.GetByRole(AriaRole.Button, new() { Name = "Commit evidence packet" }).ClickAsync();
        await Assertions.Expect(page.GetByTestId("trace-outcome-river")).ToContainTextAsync("Current attributable evidence", new() { Timeout = 15_000 });
        await Assertions.Expect(page.GetByTestId("trace-outcome-river")).ToContainTextAsync("ProjectBuilder.EndToEnd.Tests");
        await CaptureAsync(page, "171-traceability-supported-path.png");
        await page.GetByTestId("traceability-atlas").FocusAsync(); await page.Keyboard.PressAsync("End");
        await Assertions.Expect(page.GetByTestId("trace-inspector")).ToContainTextAsync("complete interface journey passed");
        await CaptureAsync(page, "172-traceability-keyboard-inspector.png");
        await page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("Evidence debt") }).ClickAsync();
        await Assertions.Expect(page.GetByTestId("trace-debt")).ToContainTextAsync("No missing outcome links");
        await CaptureAsync(page, "173-traceability-debt-clear.png");

        using var updateRequest = CreatePutApiRequest($"/api/v1/projects/{project.Id}/outcomes/{outcome.Id}", "4",
            "0198ad00-0000-7000-8000-000000000805", new UpdateOutcomeRequest(outcome.Name,
                "A contributor runs one command and sees attributable build, test, health, and schema evidence.",
                "Verification passes\nHealth is ready\nSchema validates", actor.Id, "Change claimed scope and require evidence review."));
        using var updateResponse = await api.SendAsync(updateRequest); Assert.That(updateResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        await page.ReloadAsync(); await page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("Impact radar") }).ClickAsync();
        await Assertions.Expect(page.GetByTestId("trace-impact")).ToContainTextAsync("review-required");
        await CaptureAsync(page, "174-traceability-impact-radar.png");
        var darkPage = await NewPageAsync(ColorScheme.Dark); await darkPage.GotoAsync($"{baseUrl}/projects/{project.Id}/traceability?view=impact");
        await Assertions.Expect(darkPage.GetByTestId("traceability-atlas")).ToBeVisibleAsync(); await CaptureAsync(darkPage, "175-traceability-dark.png"); await darkPage.Context.CloseAsync();
        var narrowPage = await NewPageAsync(ColorScheme.Light, 620, 900); await narrowPage.GotoAsync($"{baseUrl}/projects/{project.Id}/traceability");
        await Assertions.Expect(narrowPage.GetByRole(AriaRole.Heading, new() { Name = "Every node in deterministic order" })).ToBeVisibleAsync(); await CaptureAsync(narrowPage, "176-traceability-responsive.png"); await narrowPage.Context.CloseAsync();
        var first = await api.GetStringAsync($"/api/v1/projects/{project.Id}/lenses/traceability?view=impact"); var second = await api.GetStringAsync($"/api/v1/projects/{project.Id}/lenses/traceability?view=impact"); Assert.That(second, Is.EqualTo(first));
    }

    private static HttpRequestMessage CreateEditApiRequest<T>(
        string path,
        string revision,
        string operationId,
        T contract)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = JsonContent.Create(contract),
        };
        request.Headers.IfMatch.Add(new System.Net.Http.Headers.EntityTagHeaderValue($"\"{revision}\""));
        request.Headers.Add("Idempotency-Key", operationId);
        return request;
    }

    private static HttpRequestMessage CreatePutApiRequest<T>(string path, string revision, string operationId, T contract)
    {
        var request = new HttpRequestMessage(HttpMethod.Put, path) { Content = JsonContent.Create(contract) };
        request.Headers.IfMatch.Add(new System.Net.Http.Headers.EntityTagHeaderValue($"\"{revision}\""));
        request.Headers.Add("Idempotency-Key", operationId);
        return request;
    }

    private async Task<IPage> NewPageAsync(
        ColorScheme colorScheme = ColorScheme.Light,
        int width = 1440,
        int height = 1000)
    {
        var context = await browser!.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = width, Height = height },
            ReducedMotion = ReducedMotion.Reduce,
            ColorScheme = colorScheme,
        });
        return await context.NewPageAsync();
    }

    private static Task ExpectHeadingAsync(IPage page, string name) =>
        Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Name = name, Exact = true })).ToBeVisibleAsync();

    private Task<byte[]> CaptureAsync(IPage page, string fileName) =>
        page.ScreenshotAsync(new PageScreenshotOptions
        {
            Path = Path.Combine(evidenceDirectory, fileName),
            FullPage = true,
        });

    private Task<byte[]> CaptureViewportAsync(IPage page, string fileName) =>
        page.ScreenshotAsync(new PageScreenshotOptions
        {
            Path = Path.Combine(evidenceDirectory, fileName),
            FullPage = false,
        });
}
