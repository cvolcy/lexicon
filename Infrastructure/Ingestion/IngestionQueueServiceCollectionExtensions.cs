namespace Lexicon.Infrastructure.Ingestion;

public static class IngestionQueueServiceCollectionExtensions
{
    public const string ProviderConfigurationKey = "Ingestion:Queue:Provider";

    /// <summary>
    /// Registers <see cref="IIngestionQueue"/> and <see cref="IngestionWorker"/> using the provider
    /// selected by <c>Ingestion:Queue:Provider</c> (defaults to <see cref="IngestionQueueProvider.InMemoryChannel"/>).
    /// </summary>
    public static IServiceCollection AddLexiconIngestionQueue(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var provider = configuration.GetValue(ProviderConfigurationKey, IngestionQueueProvider.InMemoryChannel);

        switch (provider)
        {
            case IngestionQueueProvider.InMemoryChannel:
                services.AddSingleton<IIngestionQueue, ChannelIngestionQueue>();
                break;
            default:
                throw new NotSupportedException(
                    $"Ingestion queue provider '{provider}' (configured in '{ProviderConfigurationKey}') is not supported.");
        }

        services.AddHostedService<IngestionWorker>();

        return services;
    }
}
