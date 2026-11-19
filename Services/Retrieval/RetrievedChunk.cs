namespace Lexicon.Services.Retrieval;

public sealed record RetrievedChunk(
    Guid ChunkId,
    Guid DocumentId,
    string DocumentFileName,
    int ChunkIndex,
    string Text,
    int? PageNumber,
    string? Section,
    double Score,
    double? VectorScore,
    double? KeywordScore);
