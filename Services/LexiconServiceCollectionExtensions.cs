using Lexicon.Services.Documents;

namespace Lexicon.Services;

public static class LexiconServiceCollectionExtensions
{
    public static IServiceCollection AddLexiconServices(this IServiceCollection services)
    {
        services.AddLexiconDocumentServices();
        return services;
    }

    private static IServiceCollection AddLexiconDocumentServices(this IServiceCollection services)
    {
        services.AddScoped<IDocumentService, DocumentService>();
        return services;
    }
}
