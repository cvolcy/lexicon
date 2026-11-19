using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using OllamaSharp;

namespace Lexicon.Infrastructure.Ai;

public sealed class OllamaHealthCheck(
    IHttpClientFactory httpClientFactory,
    IOptions<OllamaOptions> options,
    ILogger<OllamaHealthCheck> logger) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var opts = options.Value;

        try
        {
            var httpClient = httpClientFactory.CreateClient(AiServiceCollectionExtensions.OllamaHealthCheckHttpClientName);
            var client = new OllamaApiClient(httpClient);
            var models = await client.ListLocalModelsAsync(cancellationToken);

            var availableNames = models
                .Select(m => m.Name)
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var requiredModels = new[] { opts.ChatModel, opts.EmbeddingModel }
                .Distinct(StringComparer.OrdinalIgnoreCase);

            var missing = requiredModels
                .Where(model => !IsModelPresent(availableNames, model))
                .ToList();

            if (missing.Count > 0)
            {
                var pullCommands = string.Join(" && ", missing.Select(m => $"ollama pull {m}"));
                return HealthCheckResult.Degraded(
                    $"Ollama is reachable, but missing model(s): {string.Join(", ", missing)}. Run: {pullCommands}");
            }

            return HealthCheckResult.Healthy($"Ollama ready ({opts.ChatModel}, {opts.EmbeddingModel}).");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Ollama health check failed for endpoint '{Endpoint}'.", opts.Endpoint);
            return HealthCheckResult.Unhealthy($"Unable to reach Ollama at {opts.Endpoint}.", ex);
        }
    }

    private static bool IsModelPresent(HashSet<string> availableNames, string configuredModel)
    {
        if (availableNames.Contains(configuredModel))
        {
            return true;
        }

        return !configuredModel.Contains(':') && availableNames.Contains($"{configuredModel}:latest");
    }
}
