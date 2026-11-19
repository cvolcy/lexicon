using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using OllamaSharp;

namespace Lexicon.Infrastructure.Ai;

public static class AiServiceCollectionExtensions
{
    public const string ProviderConfigurationKey = "Ai:Provider";
    internal const string OllamaHttpClientName = "Ollama";
    internal const string OllamaHealthCheckHttpClientName = "OllamaHealthCheck";

    /// <summary>
    /// Registers <see cref="IChatClient"/>, <see cref="IEmbeddingGenerator{TInput, TEmbedding}"/>,
    /// <see cref="AiModelInfo"/>, and the AI health check using the provider selected by
    /// <c>Ai:Provider</c> (defaults to <see cref="AiProvider.Ollama"/>).
    /// </summary>
    public static IServiceCollection AddLexiconAi(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var provider = configuration.GetValue(ProviderConfigurationKey, AiProvider.Ollama);

        switch (provider)
        {
            case AiProvider.Ollama:
                AddOllama(services, configuration);
                break;
            default:
                throw new NotSupportedException(
                    $"AI provider '{provider}' (configured in '{ProviderConfigurationKey}') is not supported.");
        }

        return services;
    }

    private static void AddOllama(IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<OllamaOptions>()
            .Bind(configuration.GetSection(OllamaOptions.SectionName))
            .Validate(
                o => Uri.TryCreate(o.Endpoint, UriKind.Absolute, out var uri)
                     && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps),
                $"{OllamaOptions.SectionName}:Endpoint must be a valid HTTP or HTTPS URI.")
            .Validate(
                o => !string.IsNullOrWhiteSpace(o.ChatModel),
                $"{OllamaOptions.SectionName}:ChatModel must not be empty.")
            .Validate(
                o => !string.IsNullOrWhiteSpace(o.EmbeddingModel),
                $"{OllamaOptions.SectionName}:EmbeddingModel must not be empty.")
            .Validate(
                o => o.RequestTimeoutSeconds > 0,
                $"{OllamaOptions.SectionName}:RequestTimeoutSeconds must be greater than 0.")
            .Validate(
                o => o.HealthCheckTimeoutSeconds > 0,
                $"{OllamaOptions.SectionName}:HealthCheckTimeoutSeconds must be greater than 0.")
            .ValidateOnStart();

        services.AddHttpClient(OllamaHttpClientName, (sp, client) =>
        {
            var opts = sp.GetRequiredService<IOptions<OllamaOptions>>().Value;
            client.BaseAddress = new Uri(opts.Endpoint);
            client.Timeout = TimeSpan.FromSeconds(opts.RequestTimeoutSeconds);
        });

        services.AddHttpClient(OllamaHealthCheckHttpClientName, (sp, client) =>
        {
            var opts = sp.GetRequiredService<IOptions<OllamaOptions>>().Value;
            client.BaseAddress = new Uri(opts.Endpoint);
            client.Timeout = TimeSpan.FromSeconds(opts.HealthCheckTimeoutSeconds);
        });

        services.AddSingleton(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<OllamaOptions>>().Value;
            return new AiModelInfo(AiProvider.Ollama, opts.ChatModel, opts.EmbeddingModel);
        });

        services.AddChatClient(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<OllamaOptions>>().Value;
            var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient(OllamaHttpClientName);
            return (IChatClient)new OllamaApiClient(httpClient, opts.ChatModel);
        }).UseLogging();

        services.AddEmbeddingGenerator(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<OllamaOptions>>().Value;
            var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient(OllamaHttpClientName);
            return (IEmbeddingGenerator<string, Embedding<float>>)new OllamaApiClient(httpClient, opts.EmbeddingModel);
        }).UseLogging();

        services.AddHealthChecks().AddCheck<OllamaHealthCheck>("ai");
    }
}
