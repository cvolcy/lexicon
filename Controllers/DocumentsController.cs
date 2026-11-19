using Lexicon.Infrastructure.Persistence;
using Lexicon.Infrastructure.Storage;
using Lexicon.Models;
using Lexicon.Models.Documents;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Lexicon.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DocumentsController(
    LexiconDbContext dbContext,
    IDocumentStorage documentStorage,
    ILogger<DocumentsController> logger) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<CollectionResponse<DocumentViewModel>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var documents = await dbContext.Documents
            .AsNoTracking()
            .OrderByDescending(d => d.CreatedAt)
            .Select(DocumentViewModel.Projection)
            .ToListAsync(cancellationToken);

        return Ok(new CollectionResponse<DocumentViewModel>(documents));
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    [ProducesResponseType<DocumentViewModel>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(IFormFile file, CancellationToken cancellationToken)
    {
        if (file.Length == 0)
        {
            ModelState.AddModelError(nameof(file), "The uploaded file cannot be empty.");
            return ValidationProblem(ModelState);
        }

        var now = DateTimeOffset.UtcNow;
        var documentId = Guid.CreateVersion7();
        var fileName = Path.GetFileName(file.FileName);

        await using var stream = file.OpenReadStream();
        var storageKey = await documentStorage.SaveAsync(documentId, fileName, stream, cancellationToken);

        var document = new Document
        {
            DocumentId = documentId,
            FileName = fileName,
            ContentType = string.IsNullOrWhiteSpace(file.ContentType)
                ? "application/octet-stream"
                : file.ContentType,
            SizeBytes = file.Length,
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

        return CreatedAtAction(
            nameof(Get),
            new { documentId = document.DocumentId },
            DocumentViewModel.From(document));
    }

    [HttpGet("{documentId:guid}")]
    [ProducesResponseType<DocumentViewModel>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid documentId, CancellationToken cancellationToken)
    {
        var document = await dbContext.Documents
            .AsNoTracking()
            .Where(d => d.DocumentId == documentId)
            .Select(DocumentViewModel.Projection)
            .SingleOrDefaultAsync(cancellationToken);

        return document is null ? NotFound() : Ok(document);
    }

    [HttpGet("{documentId:guid}/file")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Download(Guid documentId, CancellationToken cancellationToken)
    {
        var document = await dbContext.Documents
            .AsNoTracking()
            .Where(d => d.DocumentId == documentId)
            .Select(d => new { d.StorageKey, d.ContentType, d.FileName })
            .SingleOrDefaultAsync(cancellationToken);

        if (document is null)
        {
            return NotFound();
        }

        var stream = await documentStorage.OpenReadAsync(document.StorageKey, cancellationToken);
        if (stream is null)
        {
            return NotFound();
        }

        return File(stream, document.ContentType, document.FileName, enableRangeProcessing: true);
    }

    [HttpDelete("{documentId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid documentId, CancellationToken cancellationToken)
    {
        var document = await dbContext.Documents
            .SingleOrDefaultAsync(d => d.DocumentId == documentId, cancellationToken);

        if (document is null)
        {
            return NotFound();
        }

        dbContext.Documents.Remove(document);
        await dbContext.SaveChangesAsync(cancellationToken);

        await documentStorage.DeleteAsync(document.StorageKey, cancellationToken);

        return NoContent();
    }
}
