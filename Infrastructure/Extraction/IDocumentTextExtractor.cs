namespace Lexicon.Infrastructure.Extraction;

public interface IDocumentTextExtractor
{
    bool CanHandle(string contentType, string fileName);

    Task<ExtractedDocument> ExtractAsync(Stream content, CancellationToken cancellationToken = default);
}
