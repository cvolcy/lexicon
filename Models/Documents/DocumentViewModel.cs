using System.Linq.Expressions;

namespace Lexicon.Models.Documents;

public record DocumentViewModel(
    Guid DocumentId,
    string FileName,
    string ContentType,
    long SizeBytes,
    DocumentStatus Status,
    string? Error,
    int ChunkCount,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt)
{
    /// <summary>Translatable projection so queries only select the columns they need.</summary>
    public static readonly Expression<Func<Document, DocumentViewModel>> Projection = d => new DocumentViewModel(
        d.DocumentId,
        d.FileName,
        d.ContentType,
        d.SizeBytes,
        d.Status,
        d.Error,
        d.ChunkCount,
        d.CreatedAt,
        d.UpdatedAt);

    public static DocumentViewModel From(Document document) => Projection.Compile()(document);
}
