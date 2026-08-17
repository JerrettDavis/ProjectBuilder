using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProjectBuilder.Application.Modeling;
using ProjectBuilder.Application.Portability;
using ProjectBuilder.Application.Projects.CreateProject;
using ProjectBuilder.Application.Traceability;
using ProjectBuilder.Application.Views;
using ProjectBuilder.Infrastructure.Portability;
using ProjectBuilder.Infrastructure.Runtime;

namespace ProjectBuilder.Infrastructure.Persistence;

public static class FoundationPersistenceRegistration
{
    public static IServiceCollection AddFoundationPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("projectbuilder");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return services;
        }

        services.AddDbContextPool<FoundationDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<IProjectCreationStore, PostgresProjectCreationStore>();
        services.AddScoped<IProjectElementStore, PostgresProjectElementStore>();
        services.AddScoped<ITraceabilityStore>(services => (PostgresProjectElementStore)services.GetRequiredService<IProjectElementStore>());
        services.AddScoped<IPortableProjectStore, PostgresPortableProjectStore>();
        services.AddScoped<ICanvasViewStore, PostgresCanvasViewStore>();
        services.AddScoped<PortableProjectSnapshotProjector>();
        services.AddSingleton<IPortableProjectCodec, JsonPortableProjectCodec>();
        services.AddSingleton<SystemProjectIdentitySource>();
        services.AddSingleton<IProjectIdentitySource>(services => services.GetRequiredService<SystemProjectIdentitySource>());
        services.AddSingleton<IModelIdentitySource>(services => services.GetRequiredService<SystemProjectIdentitySource>());
        services.AddSingleton<IApplicationClock, SystemApplicationClock>();
        services.AddHealthChecks().AddDbContextCheck<FoundationDbContext>("postgresql");

        return services;
    }

    public static async Task ApplyFoundationMigrationsAsync(
        this IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetService<FoundationDbContext>();
        if (database is not null)
        {
            await database.Database.MigrateAsync(cancellationToken);
        }
    }
}
