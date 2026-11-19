namespace Lexicon.Services.Ingestion;

public interface IDocumentIngestionProcessor
{
    Task ProcessAsync(Guid documentId, CancellationToken cancellationToken = default);
}
