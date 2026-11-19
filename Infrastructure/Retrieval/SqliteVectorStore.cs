using System.Numerics.Tensors;
using System.Runtime.InteropServices;
using Lexicon.Infrastructure.Persistence;
using Lexicon.Models.Documents;
using Microsoft.EntityFrameworkCore;

namespace Lexicon.Infrastructure.Retrieval;

public sealed class SqliteVectorStore(LexiconDbContext dbContext) : IVectorStore
{
    public async Task UpsertAsync(
        IReadOnlyList<ChunkVectorRecord> vectors,
        CancellationToken cancellationToken = default)
    {
        if (vectors.Count == 0)
        {
            return;
        }

        var chunkIds = vectors.Select(v => v.ChunkId).ToList();

        await dbContext.ChunkVectors
            .Where(v => chunkIds.Contains(v.ChunkId))
            .ExecuteDeleteAsync(cancellationToken);

        var entities = vectors.Select(v => new ChunkVector
        {
            ChunkId = v.ChunkId,
            DocumentId = v.DocumentId,
            Dimensions = v.Embedding.Length,
            Embedding = MemoryMarshal.AsBytes(v.Embedding.Span).ToArray(),
            Model = v.Model,
        });

        dbContext.ChunkVectors.AddRange(entities);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task DeleteByDocumentAsync(Guid documentId, CancellationToken cancellationToken = default)
        => dbContext.ChunkVectors
            .Where(v => v.DocumentId == documentId)
            .ExecuteDeleteAsync(cancellationToken);

    public async Task<IReadOnlyList<ScoredChunk>> SearchAsync(
        ReadOnlyMemory<float> queryEmbedding,
        string model,
        int topK,
        SearchFilter? filter = null,
        CancellationToken cancellationToken = default)
    {
        if (topK <= 0 || queryEmbedding.IsEmpty)
        {
            return [];
        }

        var query = dbContext.ChunkVectors
            .AsNoTracking()
            .Where(v => v.Model == model && v.Dimensions == queryEmbedding.Length);

        if (filter?.DocumentIds is { Count: > 0 } documentIds)
        {
            query = query.Where(v => documentIds.Contains(v.DocumentId));
        }

        var queue = new PriorityQueue<ScoredChunk, double>(topK);

        await foreach (var entry in query
            .Select(v => new { v.ChunkId, v.DocumentId, v.Embedding })
            .AsAsyncEnumerable()
            .WithCancellation(cancellationToken))
        {
            var storedSpan = MemoryMarshal.Cast<byte, float>(entry.Embedding.AsSpan());
            if (storedSpan.Length != queryEmbedding.Length)
            {
                continue;
            }

            double score = TensorPrimitives.CosineSimilarity(queryEmbedding.Span, storedSpan);
            if (!double.IsFinite(score))
            {
                continue;
            }

            var candidate = new ScoredChunk(entry.ChunkId, entry.DocumentId, score);

            if (queue.Count < topK)
            {
                queue.Enqueue(candidate, score);
            }
            else if (queue.TryPeek(out _, out var lowestScore) && score > lowestScore)
            {
                queue.EnqueueDequeue(candidate, score);
            }
        }

        var results = new List<ScoredChunk>(queue.Count);
        while (queue.TryDequeue(out var item, out _))
        {
            results.Add(item);
        }

        results.Reverse();
        return results;
    }
}
