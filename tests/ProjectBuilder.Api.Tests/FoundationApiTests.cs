using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using ProjectBuilder.Contracts;

namespace ProjectBuilder.Api.Tests;

public sealed class FoundationApiTests
{
    private WebApplicationFactory<Program> _factory = null!;

    [SetUp]
    public void SetUp()
    {
        _factory = new WebApplicationFactory<Program>();
    }

    [TearDown]
    public void TearDown()
    {
        _factory.Dispose();
    }

    [Test]
    public async Task Shell_is_accessible_and_contains_build_information()
    {
        using var client = _factory.CreateClient();

        var html = await client.GetStringAsync("/");

        Assert.Multiple(() =>
        {
            Assert.That(html, Does.Contain("<h1>Project Builder</h1>"));
            Assert.That(html, Does.Contain("Build version"));
            Assert.That(html, Does.Contain("Application health"));
            Assert.That(html, Does.Contain("aria-label=\"Global navigation\""));
            Assert.That(html, Does.Contain("Skip to work surface"));
        });
    }

    [Test]
    public async Task Unknown_api_route_remains_a_non_redirecting_not_found_boundary()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });

        using var response = await client.GetAsync("/api/route-that-does-not-exist");
        using var missingAsset = await client.GetAsync("/asset-that-does-not-exist.css");

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(response.Headers.Location, Is.Null);
            Assert.That(missingAsset.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(missingAsset.Headers.Location, Is.Null);
        });
    }

    [Test]
    public async Task Stable_foundation_boundary_returns_client_safe_contract()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetFromJsonAsync<FoundationResponse>("/api/foundation");

        Assert.That(response, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(response!.Name, Is.EqualTo("Project Builder"));
            Assert.That(response.ReadinessEndpoint, Is.EqualTo("/health"));
            Assert.That(response.Commit, Is.Not.Empty);
        });
    }

    [Test]
    public async Task Liveness_endpoint_reports_healthy()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/alive");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

}
