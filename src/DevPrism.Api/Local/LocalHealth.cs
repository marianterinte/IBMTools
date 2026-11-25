// Spec: Documentation/dev-prism-ai-specs/11_API_AND_EVENTS.md (local readiness).
namespace DevPrism.Api.Local;

using DevPrism.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

/// <summary>Reports database readiness independently of optional provider connectivity.</summary>
internal static class LocalHealth
{
    /// <summary>Checks local PostgreSQL and pending migrations without disclosing configuration.</summary>
    internal static async Task<IResult> Check(DevPrismDbContext db, IConfiguration config, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(config.GetConnectionString("DevPrism")))
            return Result("notConfigured");

        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(TimeSpan.FromSeconds(3));
        try
        {
            if (!await db.Database.CanConnectAsync(deadline.Token)) return Result("unreachable");
            var pending = await db.Database.GetPendingMigrationsAsync(deadline.Token);
            return Result(pending.Any() ? "migrationRequired" : "ready");
        }
        catch (Exception ex) when (ex is NpgsqlException or TimeoutException or ArgumentException
            || (ex is OperationCanceledException && !cancellationToken.IsCancellationRequested))
        {
            return Result("unreachable");
        }
    }

    /// <summary>Maps a database state to a redacted readiness response.</summary>
    private static IResult Result(string database) => Results.Json(new Status(database),
        statusCode: database == "ready" ? StatusCodes.Status200OK : StatusCodes.Status503ServiceUnavailable);

    /// <summary>Local workspace database status; does not assert any provider connection.</summary>
    internal sealed record Status(string Database);
}
