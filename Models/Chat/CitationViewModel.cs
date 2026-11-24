using Lexicon.Services.Retrieval;

namespace Lexicon.Models.Chat;

public sealed record CitationViewModel(
    int Index,
    Guid ChunkId,
    Guid DocumentId,
    string DocumentFileName,
    int ChunkIndex,
    string Text,
    int? PageNumber,
    string? Section,
    double Score)
{
    public static CitationViewModel From(int index, RetrievedChunk chunk) => new(
        Index: index,
        ChunkId: chunk.ChunkId,
        DocumentId: chunk.DocumentId,
        DocumentFileName: chunk.DocumentFileName,
        ChunkIndex: chunk.ChunkIndex,
        Text: chunk.Text,
        PageNumber: chunk.PageNumber,
        Section: chunk.Section,
        Score: Math.Round(chunk.Score, 6));
}
