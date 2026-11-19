namespace Lexicon.Models.Documents;

public class ChunkVector
{
    public Guid ChunkId { get; set; }

    public Guid DocumentId { get; set; }

    public int Dimensions { get; set; }

    /// <summary>Little-endian float32[] raw bytes for compact storage and Span-based cosine similarity.</summary>
    public required byte[] Embedding { get; set; }

    public required string Model { get; set; }

    public DocumentChunk Chunk { get; set; } = null!;
}
