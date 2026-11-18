using Lexicon.Infrastructure.Persistence;
using Lexicon.Models;
using Lexicon.Models.Documents;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Lexicon.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DocumentsController(LexiconDbContext dbContext) : ControllerBase
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

        var document = new Document
        {
            DocumentId = documentId,
            FileName = Path.GetFileName(file.FileName),
            ContentType = string.IsNullOrWhiteSpace(file.ContentType)
                ? "application/octet-stream"
                : file.ContentType,
            SizeBytes = file.Length,
            // Placeholder until a real IDocumentStorage assigns the key.
            StorageKey = documentId.ToString("N"),
            Status = DocumentStatus.Pending,
            CreatedAt = now,
            UpdatedAt = now,
        };

        dbContext.Documents.Add(document);
        await dbContext.SaveChangesAsync(cancellationToken);

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

    [HttpDelete("{documentId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid documentId, CancellationToken cancellationToken)
    {
        var deleted = await dbContext.Documents
            .Where(d => d.DocumentId == documentId)
            .ExecuteDeleteAsync(cancellationToken);

        return deleted == 0 ? NotFound() : NoContent();
    }
}
