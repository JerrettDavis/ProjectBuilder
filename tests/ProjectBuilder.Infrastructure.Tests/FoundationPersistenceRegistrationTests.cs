using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProjectBuilder.Infrastructure.Persistence;

namespace ProjectBuilder.Infrastructure.Tests;

public sealed class FoundationPersistenceRegistrationTests
{
    [Test]
    public void Missing_connection_string_leaves_persistence_unregistered()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        services.AddFoundationPersistence(configuration);

        Assert.That(services.Any(descriptor => descriptor.ServiceType == typeof(FoundationDbContext)), Is.False);
    }

    [Test]
    public void PostgreSQL_connection_uses_the_Npgsql_provider_without_connecting()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:projectbuilder"] = "Host=localhost;Database=projectbuilder;Username=test;Password=test"
            })
            .Build();

        services.AddFoundationPersistence(configuration);

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FoundationDbContext>();

        Assert.That(context.Database.ProviderName, Is.EqualTo("Npgsql.EntityFrameworkCore.PostgreSQL"));
    }
}
