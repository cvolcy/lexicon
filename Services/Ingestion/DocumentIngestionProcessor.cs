using Lexicon.Infrastructure.Extraction;
using Lexicon.Infrastructure.Persistence;
using Lexicon.Infrastructure.Storage;
using Lexicon.Models.Documents;
using Microsoft.EntityFrameworkCore;

namespace Lexicon.Services.Ingestion;

public sealed class DocumentIngestionProcessor(
    LexiconDbContext dbContext,
    IDocumentStorage documentStorage,
    IEnumerable<IDocumentTextExtractor> extractors,
    IChunker chunker,
    ILogger<DocumentIngestionProcessor> logger) : IDocumentIngestionProcessor
{
    private const int MaxErrorLength = 2000;

    public async Task ProcessAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        var document = await dbContext.Documents
            .SingleOrDefaultAsync(d => d.DocumentId == documentId, cancellationToken);

        if (document is null)
        {
            logger.LogDebug("Skipping ingestion for missing document {DocumentId}.", documentId);
            return;
        }

        document.Status = DocumentStatus.Processing;
        document.Error = null;
        document.UpdatedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        try
        {
            var extractor = extractors.FirstOrDefault(e => e.CanHandle(document.ContentType, document.FileName));
            if (extractor is null)
            {
                var extension = Path.GetExtension(document.FileName);
                await MarkFailedAsync(
                    documentId,
                    $"Unsupported file format '{(string.IsNullOrEmpty(extension) ? document.ContentType : extension)}'.",
                    cancellationToken);
                return;
            }

            await using var stream = await documentStorage.OpenReadAsync(document.StorageKey, cancellationToken);
            if (stream is null)
            {
                await MarkFailedAsync(documentId, "Stored file could not be found.", cancellationToken);
                return;
            }

            var extracted = await extractor.ExtractAsync(stream, cancellationToken);
            var chunks = chunker.Chunk(extracted);
            if (chunks.Count == 0)
            {
                await MarkFailedAsync(documentId, "No extractable text was found in the document.", cancellationToken);
                return;
            }

            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

            await dbContext.DocumentChunks
                .Where(c => c.DocumentId == documentId)
                .ExecuteDeleteAsync(cancellationToken);

            var chunkEntities = chunks.Select(chunk => new DocumentChunk
            {
                ChunkId = Guid.CreateVersion7(),
                DocumentId = documentId,
                Index = chunk.Index,
                Text = chunk.Text,
                PageNumber = chunk.PageNumber,
                Section = chunk.Section,
                TokenCount = chunk.TokenCount,
            });

            dbContext.DocumentChunks.AddRange(chunkEntities);

            document.ChunkCount = chunks.Count;
            document.Status = DocumentStatus.Ready;
            document.Error = null;
            document.UpdatedAt = DateTimeOffset.UtcNow;

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            logger.LogInformation(
                "Ingested document {DocumentId} ({FileName}) into {ChunkCount} chunk(s).",
                documentId,
                document.FileName,
                chunks.Count);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Ingestion failed for document {DocumentId} ({FileName}).", documentId, document.FileName);
            await MarkFailedAsync(documentId, ex.Message, CancellationToken.None);
        }
    }

    private async Task MarkFailedAsync(Guid documentId, string reason, CancellationToken cancellationToken)
    {
        dbContext.ChangeTracker.Clear();

        var truncatedError = reason.Length > MaxErrorLength ? reason[..MaxErrorLength] : reason;
        var now = DateTimeOffset.UtcNow;

        await dbContext.Documents
            .Where(d => d.DocumentId == documentId)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(d => d.Status, DocumentStatus.Failed)
                    .SetProperty(d => d.Error, truncatedError)
                    .SetProperty(d => d.UpdatedAt, now),
                cancellationToken);
    }
}
