namespace Lexicon.Infrastructure.Retrieval;

public interface IKeywordIndex
{
    Task IndexAsync(
        IReadOnlyList<IndexedChunkRecord> chunks,
        CancellationToken cancellationToken = default);

    Task DeleteByDocumentAsync(
        Guid documentId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ScoredChunk>> SearchAsync(
        string query,
        int topK,
        SearchFilter? filter = null,
        CancellationToken cancellationToken = default);
}
