namespace Lexicon.Models.Documents;

public class DocumentChunk
{
    public Guid ChunkId { get; set; }

    public Guid DocumentId { get; set; }

    public int Index { get; set; }

    public required string Text { get; set; }

    public int? PageNumber { get; set; }

    public string? Section { get; set; }

    public int TokenCount { get; set; }

    public Document Document { get; set; } = null!;

    public ChunkVector? Vector { get; set; }
}
