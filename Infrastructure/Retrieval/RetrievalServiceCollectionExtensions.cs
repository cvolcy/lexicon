namespace Lexicon.Infrastructure.Retrieval;

public static class RetrievalServiceCollectionExtensions
{
    public const string VectorStoreProviderConfigurationKey = "Retrieval:VectorStore:Provider";
    public const string KeywordIndexProviderConfigurationKey = "Retrieval:KeywordIndex:Provider";

    /// <summary>
    /// Registers <see cref="IVectorStore"/> and <see cref="IKeywordIndex"/> using the providers
    /// selected by <c>Retrieval:VectorStore:Provider</c> and <c>Retrieval:KeywordIndex:Provider</c>.
    /// </summary>
    public static IServiceCollection AddLexiconSearchIndexes(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var vectorProvider = configuration.GetValue(VectorStoreProviderConfigurationKey, VectorStoreProvider.Sqlite);
        switch (vectorProvider)
        {
            case VectorStoreProvider.Sqlite:
                services.AddScoped<IVectorStore, SqliteVectorStore>();
                break;
            default:
                throw new NotSupportedException(
                    $"Vector store provider '{vectorProvider}' (configured in '{VectorStoreProviderConfigurationKey}') is not supported.");
        }

        var keywordProvider = configuration.GetValue(KeywordIndexProviderConfigurationKey, KeywordIndexProvider.SqliteFts5);
        switch (keywordProvider)
        {
            case KeywordIndexProvider.SqliteFts5:
                services.AddScoped<IKeywordIndex, SqliteFts5KeywordIndex>();
                break;
            default:
                throw new NotSupportedException(
                    $"Keyword index provider '{keywordProvider}' (configured in '{KeywordIndexProviderConfigurationKey}') is not supported.");
        }

        return services;
    }
}
