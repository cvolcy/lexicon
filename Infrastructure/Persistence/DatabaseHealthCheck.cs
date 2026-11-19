using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Lexicon.Infrastructure.Persistence;

public sealed class DatabaseHealthCheck(
    LexiconDbContext dbContext,
    ILogger<DatabaseHealthCheck> logger) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!await dbContext.Database.CanConnectAsync(cancellationToken))
            {
                return HealthCheckResult.Unhealthy("Unable to connect to the database.");
            }

            var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync(cancellationToken);
            var pendingCount = pendingMigrations.Count();
            if (pendingCount > 0)
            {
                return HealthCheckResult.Degraded($"Database has {pendingCount} pending migration(s).");
            }

            return HealthCheckResult.Healthy("Database is reachable.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Database health check failed.");
            return HealthCheckResult.Unhealthy("Database health check failed.", ex);
        }
    }
}
