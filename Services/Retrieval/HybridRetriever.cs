using Lexicon.Infrastructure.Ai;
using Lexicon.Infrastructure.Persistence;
using Lexicon.Infrastructure.Retrieval;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;

namespace Lexicon.Services.Retrieval;

public sealed class HybridRetriever(
    IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
    AiModelInfo aiModelInfo,
    IVectorStore vectorStore,
    IKeywordIndex keywordIndex,
    LexiconDbContext dbContext,
    IOptions<RetrievalOptions> options) : IRetriever
{
    public async Task<IReadOnlyList<RetrievedChunk>> RetrieveAsync(
        string query,
        int? topK = null,
        SearchFilter? filter = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return [];
        }

        var opts = options.Value;
        var k = Math.Clamp(topK ?? opts.DefaultTopK, 1, opts.MaxTopK);
        var candidateK = k * opts.CandidateMultiplier;

        var embeddings = await embeddingGenerator.GenerateAsync([query], cancellationToken: cancellationToken);
        if (embeddings.Count == 0)
        {
            return [];
        }

        var queryVector = embeddings[0].Vector;

        // Sequential calls because both stores share the scoped LexiconDbContext.
        var vectorResults = await vectorStore.SearchAsync(
            queryVector,
            aiModelInfo.EmbeddingModel,
            candidateK,
            filter,
            cancellationToken);

        var keywordResults = opts.UseKeywordSearch
            ? await keywordIndex.SearchAsync(query, candidateK, filter, cancellationToken)
            : [];

        var ranked = FuseRankings(vectorResults, keywordResults, opts.RrfK, k);
        if (ranked.Count == 0)
        {
            return [];
        }

        var chunkIds = ranked.Select(r => r.ChunkId).ToList();

        var hydrated = await dbContext.DocumentChunks
            .AsNoTracking()
            .Where(c => chunkIds.Contains(c.ChunkId))
            .Select(c => new
            {
                c.ChunkId,
                c.DocumentId,
                DocumentFileName = c.Document.FileName,
                c.Index,
                c.Text,
                c.PageNumber,
                c.Section,
            })
            .ToDictionaryAsync(c => c.ChunkId, cancellationToken);

        var results = new List<RetrievedChunk>(ranked.Count);
        foreach (var item in ranked)
        {
            if (!hydrated.TryGetValue(item.ChunkId, out var chunk))
            {
                continue;
            }

            results.Add(new RetrievedChunk(
                ChunkId: chunk.ChunkId,
                DocumentId: chunk.DocumentId,
                DocumentFileName: chunk.DocumentFileName,
                ChunkIndex: chunk.Index,
                Text: chunk.Text,
                PageNumber: chunk.PageNumber,
                Section: chunk.Section,
                Score: item.FusedScore,
                VectorScore: item.VectorScore,
                KeywordScore: item.KeywordScore));
        }

        return results;
    }

    private static List<RrfCandidate> FuseRankings(
        IReadOnlyList<ScoredChunk> vectorResults,
        IReadOnlyList<ScoredChunk> keywordResults,
        int rrfK,
        int limit)
    {
        var candidates = new Dictionary<Guid, RrfAccumulator>();

        for (var i = 0; i < vectorResults.Count; i++)
        {
            var result = vectorResults[i];
            var rrf = 1.0 / (rrfK + i + 1);
            var acc = GetOrCreate(candidates, result.ChunkId);
            acc.FusedScore += rrf;
            acc.VectorScore = result.Score;
        }

        for (var i = 0; i < keywordResults.Count; i++)
        {
            var result = keywordResults[i];
            var rrf = 1.0 / (rrfK + i + 1);
            var acc = GetOrCreate(candidates, result.ChunkId);
            acc.FusedScore += rrf;
            acc.KeywordScore = result.Score;
        }

        return candidates.Values
            .OrderByDescending(c => c.FusedScore)
            .ThenByDescending(c => c.VectorScore ?? double.MinValue)
            .Take(limit)
            .Select(c => new RrfCandidate(c.ChunkId, c.FusedScore, c.VectorScore, c.KeywordScore))
            .ToList();
    }

    private static RrfAccumulator GetOrCreate(Dictionary<Guid, RrfAccumulator> map, Guid chunkId)
    {
        if (!map.TryGetValue(chunkId, out var acc))
        {
            acc = new RrfAccumulator(chunkId);
            map[chunkId] = acc;
        }

        return acc;
    }

    private sealed class RrfAccumulator(Guid chunkId)
    {
        public Guid ChunkId { get; } = chunkId;
        public double FusedScore { get; set; }
        public double? VectorScore { get; set; }
        public double? KeywordScore { get; set; }
    }

    private sealed record RrfCandidate(
        Guid ChunkId,
        double FusedScore,
        double? VectorScore,
        double? KeywordScore);
}
