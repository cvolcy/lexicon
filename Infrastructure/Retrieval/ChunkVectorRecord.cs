namespace Lexicon.Infrastructure.Retrieval;

public sealed record ChunkVectorRecord(
    Guid ChunkId,
    Guid DocumentId,
    ReadOnlyMemory<float> Embedding,
    string Model);
