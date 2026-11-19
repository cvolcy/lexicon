using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Lexicon.Infrastructure.Storage;

public sealed class LocalFileSystemStorageHealthCheck(
    IOptions<LocalFileSystemStorageOptions> options,
    IHostEnvironment environment,
    ILogger<LocalFileSystemStorageHealthCheck> logger) : IHealthCheck
{
    private readonly string _rootPath = LocalFileSystemStoragePath.ResolveRootPath(
        options.Value.RootPath,
        environment.ContentRootPath);

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!Directory.Exists(_rootPath))
            {
                return HealthCheckResult.Unhealthy("Storage directory does not exist.");
            }

            var probePath = Path.Combine(_rootPath, $".healthcheck-{Guid.NewGuid():N}.tmp");
            await using (var stream = new FileStream(
                probePath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 1,
                FileOptions.Asynchronous | FileOptions.DeleteOnClose))
            {
                await stream.FlushAsync(cancellationToken);
            }

            return HealthCheckResult.Healthy("Local file system storage is accessible.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Local file system storage health check failed for '{RootPath}'.", _rootPath);
            return HealthCheckResult.Unhealthy("Local file system storage is not accessible.", ex);
        }
    }
}
