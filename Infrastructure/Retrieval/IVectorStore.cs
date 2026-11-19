namespace Lexicon.Infrastructure.Retrieval;

public interface IVectorStore
{
    Task UpsertAsync(
        IReadOnlyList<ChunkVectorRecord> vectors,
        CancellationToken cancellationToken = default);

    Task DeleteByDocumentAsync(
        Guid documentId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ScoredChunk>> SearchAsync(
        ReadOnlyMemory<float> queryEmbedding,
        string model,
        int topK,
        SearchFilter? filter = null,
        CancellationToken cancellationToken = default);
}
