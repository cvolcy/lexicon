using Lexicon.Services.Chat;
using Lexicon.Services.Documents;
using Lexicon.Services.Ingestion;
using Lexicon.Services.Retrieval;

namespace Lexicon.Services;

public static class LexiconServiceCollectionExtensions
{
    public static IServiceCollection AddLexiconServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddLexiconDocumentServices(configuration);
        services.AddLexiconIngestionServices(configuration);
        services.AddLexiconRetrievalServices(configuration);
        services.AddLexiconChatServices(configuration);
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

    private static IServiceCollection AddLexiconRetrievalServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<RetrievalOptions>()
            .Bind(configuration.GetSection(RetrievalOptions.SectionName))
            .Validate(
                o => o.DefaultTopK > 0,
                $"{RetrievalOptions.SectionName}:DefaultTopK must be greater than 0.")
            .Validate(
                o => o.MaxTopK >= o.DefaultTopK,
                $"{RetrievalOptions.SectionName}:MaxTopK must be greater than or equal to DefaultTopK.")
            .Validate(
                o => o.RrfK > 0,
                $"{RetrievalOptions.SectionName}:RrfK must be greater than 0.")
            .Validate(
                o => o.CandidateMultiplier > 0,
                $"{RetrievalOptions.SectionName}:CandidateMultiplier must be greater than 0.")
            .ValidateOnStart();

        services.AddScoped<IRetriever, HybridRetriever>();
        return services;
    }

    private static IServiceCollection AddLexiconChatServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<RagChatOptions>()
            .Bind(configuration.GetSection(RagChatOptions.SectionName))
            .Validate(
                o => o.RetrievalTopK > 0,
                $"{RagChatOptions.SectionName}:RetrievalTopK must be greater than 0.")
            .Validate(
                o => o.MaxHistoryMessages >= 0,
                $"{RagChatOptions.SectionName}:MaxHistoryMessages must be greater than or equal to 0.")
            .Validate(
                o => o.MaxHistoryTokens >= 0,
                $"{RagChatOptions.SectionName}:MaxHistoryTokens must be greater than or equal to 0.")
            .Validate(
                o => o.MaxContextTokens > 0,
                $"{RagChatOptions.SectionName}:MaxContextTokens must be greater than 0.")
            .Validate(
                o => o.MaxMessageLength > 0,
                $"{RagChatOptions.SectionName}:MaxMessageLength must be greater than 0.")
            .Validate(
                o => o.Temperature is >= 0f and <= 2f,
                $"{RagChatOptions.SectionName}:Temperature must be between 0 and 2.")
            .ValidateOnStart();

        services.AddSingleton<IPromptBuilder, PromptBuilder>();
        services.AddScoped<IChatService, ChatService>();
        return services;
    }
}
