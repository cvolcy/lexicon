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
            .Select(d => new DocumentViewModel(d.DocumentId, d.CreatedAt))
            .ToListAsync(cancellationToken);

        return Ok(new CollectionResponse<DocumentViewModel>(documents));
    }

    [HttpPost]
    [ProducesResponseType<DocumentViewModel>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        var document = new Document
        {
            DocumentId = Guid.CreateVersion7(),
            CreatedAt = DateTimeOffset.UtcNow,
        };

        dbContext.Documents.Add(document);
        await dbContext.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(
            nameof(Get),
            new { documentId = document.DocumentId },
            new DocumentViewModel(document.DocumentId, document.CreatedAt));
    }

    [HttpGet("{documentId:guid}")]
    [ProducesResponseType<DocumentViewModel>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid documentId, CancellationToken cancellationToken)
    {
        var document = await dbContext.Documents
            .AsNoTracking()
            .Where(d => d.DocumentId == documentId)
            .Select(d => new DocumentViewModel(d.DocumentId, d.CreatedAt))
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
