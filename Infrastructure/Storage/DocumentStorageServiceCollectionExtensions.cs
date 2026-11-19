namespace Lexicon.Infrastructure.Storage;

public static class DocumentStorageServiceCollectionExtensions
{
    public const string ProviderConfigurationKey = "Storage:Provider";

    /// <summary>
    /// Registers <see cref="IDocumentStorage"/> using the provider selected by
    /// <c>Storage:Provider</c> (defaults to <see cref="DocumentStorageProvider.LocalFileSystem"/>).
    /// </summary>
    public static IServiceCollection AddLexiconDocumentStorage(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var provider = configuration.GetValue(ProviderConfigurationKey, DocumentStorageProvider.LocalFileSystem);

        switch (provider)
        {
            case DocumentStorageProvider.LocalFileSystem:
                AddLocalFileSystem(services, configuration);
                break;
            default:
                throw new NotSupportedException(
                    $"Document storage provider '{provider}' (configured in '{ProviderConfigurationKey}') is not supported.");
        }

        return services;
    }

    private static void AddLocalFileSystem(IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<LocalFileSystemStorageOptions>()
            .Bind(configuration.GetSection(LocalFileSystemStorageOptions.SectionName))
            .Validate(
                o => !string.IsNullOrWhiteSpace(o.RootPath),
                $"{LocalFileSystemStorageOptions.SectionName}:RootPath must not be empty.")
            .ValidateOnStart();

        services.AddSingleton<IDocumentStorage, LocalFileSystemDocumentStorage>();
        services.AddHealthChecks().AddCheck<LocalFileSystemStorageHealthCheck>("storage");
    }
}
