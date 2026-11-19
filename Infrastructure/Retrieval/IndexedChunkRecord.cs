namespace Lexicon.Infrastructure.Retrieval;

public sealed record IndexedChunkRecord(
    Guid ChunkId,
    Guid DocumentId,
    string Text);
