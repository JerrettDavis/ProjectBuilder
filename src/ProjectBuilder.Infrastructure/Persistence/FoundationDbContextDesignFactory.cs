using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ProjectBuilder.Infrastructure.Persistence;

internal sealed class FoundationDbContextDesignFactory : IDesignTimeDbContextFactory<FoundationDbContext>
{
    public FoundationDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<FoundationDbContext>()
            .UseNpgsql("Host=localhost;Database=projectbuilder")
            .Options;
        return new FoundationDbContext(options);
    }
}
