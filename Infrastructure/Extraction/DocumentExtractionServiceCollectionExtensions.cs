namespace Lexicon.Infrastructure.Extraction;

public static class DocumentExtractionServiceCollectionExtensions
{
    public static IServiceCollection AddLexiconDocumentExtractors(this IServiceCollection services)
    {
        // Register Markdown before PlainText so .md files sent as text/plain still match Markdown.
        services.AddSingleton<IDocumentTextExtractor, MarkdownDocumentExtractor>();
        services.AddSingleton<IDocumentTextExtractor, PdfDocumentExtractor>();
        services.AddSingleton<IDocumentTextExtractor, PlainTextDocumentExtractor>();

        return services;
    }
}
