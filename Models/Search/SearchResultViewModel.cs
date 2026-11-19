using Lexicon.Services.Retrieval;

namespace Lexicon.Models.Search;

public sealed record SearchResultViewModel(
    Guid ChunkId,
    Guid DocumentId,
    string DocumentFileName,
    int ChunkIndex,
    string Text,
    int? PageNumber,
    string? Section,
    double Score,
    double? VectorScore,
    double? KeywordScore)
{
    public static SearchResultViewModel From(RetrievedChunk chunk) => new(
        ChunkId: chunk.ChunkId,
        DocumentId: chunk.DocumentId,
        DocumentFileName: chunk.DocumentFileName,
        ChunkIndex: chunk.ChunkIndex,
        Text: chunk.Text,
        PageNumber: chunk.PageNumber,
        Section: chunk.Section,
        Score: Math.Round(chunk.Score, 6),
        VectorScore: chunk.VectorScore.HasValue ? Math.Round(chunk.VectorScore.Value, 6) : null,
        KeywordScore: chunk.KeywordScore.HasValue ? Math.Round(chunk.KeywordScore.Value, 6) : null);
}
