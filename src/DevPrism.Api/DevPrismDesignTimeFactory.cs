// Spec: Documentation/dev-prism-ai-specs/04_ARCHITECTURE.md (explicit schema migration).
namespace DevPrism.Api;

using DevPrism.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

/// <summary>Allows the EF command to run without starting Kestrel or any provider integration.</summary>
public sealed class DevPrismDesignTimeFactory : IDesignTimeDbContextFactory<DevPrismDbContext>
{
    /// <summary>Creates a context from the operator-supplied migration connection string.</summary>
    /// <param name="args">Arguments supplied by the EF command.</param>
    /// <returns>A context for EF design-time operations.</returns>
    public DevPrismDbContext CreateDbContext(string[] args)
    {
        var connection = Environment.GetEnvironmentVariable("ConnectionStrings__DevPrism");
        if (string.IsNullOrWhiteSpace(connection))
        {
            throw new InvalidOperationException("Set ConnectionStrings__DevPrism before running EF commands.");
        }

        var options = new DbContextOptionsBuilder<DevPrismDbContext>().UseNpgsql(connection).Options;
        return new DevPrismDbContext(options);
    }
}
