// Spec: Documentation/dev-prism-ai-specs/04_ARCHITECTURE.md (startup recovery).
namespace DevPrism.Api.Sync;

using DevPrism.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

/// <summary>Closes abandoned local runs without contacting any provider.</summary>
internal sealed class SyncRunRecovery(
    IServiceScopeFactory scopes, IConfiguration configuration, TimeProvider clock,
    ILogger<SyncRunRecovery> logger) : IHostedService
{
    /// <summary>Marks pending rows from a previous process as interrupted before new work can start.</summary>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(configuration.GetConnectionString("DevPrism"))) return;
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(5));
        await using var scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<DevPrismDbContext>();
        var stoppedAt = clock.GetUtcNow();
        try
        {
            var count = await db.SyncRuns
                .Where(run => run.State == SyncRunState.Queued || run.State == SyncRunState.Running)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(run => run.State, SyncRunState.Interrupted)
                    .SetProperty(run => run.FinishedAt, stoppedAt)
                    .SetProperty(run => run.ReasonCode, "SERVICE_RESTARTED"), timeout.Token);
            if (count > 0) logger.LogInformation("Recovered {RunCount} interrupted local sync runs", count);
        }
        catch (Exception ex) when (ex is NpgsqlException or TimeoutException
            || (ex is OperationCanceledException && !cancellationToken.IsCancellationRequested))
        {
            // The setup UI remains reachable, but imports cannot be enabled or presumed healthy.
            logger.LogWarning("Sync recovery could not read the local database. Verify setup and restart.");
        }
    }

    /// <summary>No provider or background operation is started by this recovery service.</summary>
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
