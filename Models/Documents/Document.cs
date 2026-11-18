namespace Lexicon.Models.Documents;

public class Document
{
    public Guid DocumentId { get; set; }

    /// <summary>Original file name as uploaded by the user (display only, never used as a path).</summary>
    public required string FileName { get; set; }

    public required string ContentType { get; set; }

    public long SizeBytes { get; set; }

    /// <summary>Opaque key used by the document storage to locate the file.</summary>
    public required string StorageKey { get; set; }

    public DocumentStatus Status { get; set; } = DocumentStatus.Pending;

    /// <summary>Failure reason when <see cref="Status"/> is <see cref="DocumentStatus.Failed"/>.</summary>
    public string? Error { get; set; }

    /// <summary>Number of chunks produced by ingestion.</summary>
    public int ChunkCount { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
