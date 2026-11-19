namespace Lexicon.Infrastructure.Retrieval;

public sealed record ScoredChunk(
    Guid ChunkId,
    Guid DocumentId,
    double Score);
