namespace Lexicon.Infrastructure.Ingestion;

public interface IIngestionQueue
{
    ValueTask EnqueueAsync(Guid documentId, CancellationToken cancellationToken = default);

    IAsyncEnumerable<Guid> DequeueAllAsync(CancellationToken cancellationToken);
}
