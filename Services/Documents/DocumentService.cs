using Lexicon.Infrastructure.Persistence;
using Lexicon.Infrastructure.Storage;
using Lexicon.Models.Documents;
using Microsoft.EntityFrameworkCore;

namespace Lexicon.Services.Documents;

public sealed class DocumentService(
    LexiconDbContext dbContext,
    IDocumentStorage documentStorage,
    ILogger<DocumentService> logger) : IDocumentService
{
    public async Task<IReadOnlyList<DocumentViewModel>> ListAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Documents
            .AsNoTracking()
            .OrderByDescending(d => d.CreatedAt)
            .Select(DocumentViewModel.Projection)
            .ToListAsync(cancellationToken);
    }

    public async Task<DocumentViewModel?> GetAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Documents
            .AsNoTracking()
            .Where(d => d.DocumentId == documentId)
            .Select(DocumentViewModel.Projection)
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<DocumentViewModel> CreateAsync(
        string fileName,
        string contentType,
        long sizeBytes,
        Stream content,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var documentId = Guid.CreateVersion7();
        var storageKey = await documentStorage.SaveAsync(documentId, fileName, content, cancellationToken);

        var document = new Document
        {
            DocumentId = documentId,
            FileName = fileName,
            ContentType = contentType,
            SizeBytes = sizeBytes,
            StorageKey = storageKey,
            Status = DocumentStatus.Pending,
            CreatedAt = now,
            UpdatedAt = now,
        };

        try
        {
            dbContext.Documents.Add(document);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            try
            {
                await documentStorage.DeleteAsync(storageKey, CancellationToken.None);
            }
            catch (Exception cleanupEx)
            {
                logger.LogWarning(
                    cleanupEx,
                    "Failed to clean up stored file '{StorageKey}' after database save failure for document {DocumentId}.",
                    storageKey,
                    documentId);
            }

            throw;
        }

        return DocumentViewModel.From(document);
    }

    public async Task<DocumentDownloadResult?> OpenDownloadAsync(
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        var document = await dbContext.Documents
            .AsNoTracking()
            .Where(d => d.DocumentId == documentId)
            .Select(d => new { d.StorageKey, d.ContentType, d.FileName })
            .SingleOrDefaultAsync(cancellationToken);

        if (document is null)
        {
            return null;
        }

        var stream = await documentStorage.OpenReadAsync(document.StorageKey, cancellationToken);
        if (stream is null)
        {
            return null;
        }

        return new DocumentDownloadResult(stream, document.ContentType, document.FileName);
    }

    public async Task<bool> DeleteAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        var document = await dbContext.Documents
            .SingleOrDefaultAsync(d => d.DocumentId == documentId, cancellationToken);

        if (document is null)
        {
            return false;
        }

        dbContext.Documents.Remove(document);
        await dbContext.SaveChangesAsync(cancellationToken);

        await documentStorage.DeleteAsync(document.StorageKey, cancellationToken);

        return true;
    }
}
