using Lexicon.Services.Documents;
using Lexicon.Services.Ingestion;

namespace Lexicon.Services;

public static class LexiconServiceCollectionExtensions
{
    public static IServiceCollection AddLexiconServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddLexiconDocumentServices(configuration);
        services.AddLexiconIngestionServices(configuration);
        return services;
    }

    private static IServiceCollection AddLexiconDocumentServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<UploadOptions>()
            .Bind(configuration.GetSection(UploadOptions.SectionName))
            .Validate(
                o => o.MaxFileSizeBytes > 0,
                $"{UploadOptions.SectionName}:MaxFileSizeBytes must be greater than 0.")
            .Validate(
                o => o.AllowedExtensions is { Length: > 0 },
                $"{UploadOptions.SectionName}:AllowedExtensions must contain at least one extension.")
            .ValidateOnStart();

        services.AddScoped<IDocumentService, DocumentService>();
        return services;
    }

    private static IServiceCollection AddLexiconIngestionServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<ChunkingOptions>()
            .Bind(configuration.GetSection(ChunkingOptions.SectionName))
            .Validate(
                o => o.MaxTokens > 0,
                $"{ChunkingOptions.SectionName}:MaxTokens must be greater than 0.")
            .Validate(
                o => o.OverlapTokens >= 0 && o.OverlapTokens < o.MaxTokens,
                $"{ChunkingOptions.SectionName}:OverlapTokens must be between 0 and MaxTokens - 1.")
            .ValidateOnStart();

        services.AddSingleton<IChunker, RecursiveTextChunker>();
        services.AddScoped<IDocumentIngestionProcessor, DocumentIngestionProcessor>();
        return services;
    }
}
