using Lexicon.Models;
using Lexicon.Models.Documents;
using Lexicon.Services.Documents;
using Microsoft.AspNetCore.Mvc;

namespace Lexicon.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DocumentsController(IDocumentService documentService) : ControllerBase
{
    private const int MaxFileNameLength = 255;
    private const int MaxContentTypeLength = 255;

    [HttpGet]
    [ProducesResponseType<CollectionResponse<DocumentViewModel>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var documents = await documentService.ListAsync(cancellationToken);
        return Ok(new CollectionResponse<DocumentViewModel>(documents));
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    [ProducesResponseType<DocumentViewModel>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(IFormFile file, CancellationToken cancellationToken)
    {
        var fileName = Path.GetFileName(file.FileName)?.Trim() ?? string.Empty;

        if (file.Length == 0)
        {
            ModelState.AddModelError(nameof(file), "The uploaded file cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(fileName))
        {
            ModelState.AddModelError(nameof(file), "A valid file name is required.");
        }
        else if (fileName.Length > MaxFileNameLength)
        {
            ModelState.AddModelError(nameof(file), $"File name must be at most {MaxFileNameLength} characters.");
        }

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var contentType = string.IsNullOrWhiteSpace(file.ContentType) || file.ContentType.Length > MaxContentTypeLength
            ? "application/octet-stream"
            : file.ContentType;

        await using var stream = file.OpenReadStream();
        var document = await documentService.CreateAsync(fileName, contentType, file.Length, stream, cancellationToken);

        return CreatedAtAction(
            nameof(Get),
            new { documentId = document.DocumentId },
            document);
    }

    [HttpGet("{documentId:guid}")]
    [ProducesResponseType<DocumentViewModel>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid documentId, CancellationToken cancellationToken)
    {
        var document = await documentService.GetAsync(documentId, cancellationToken);
        return document is null ? NotFound() : Ok(document);
    }

    [HttpGet("{documentId:guid}/file")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Download(Guid documentId, CancellationToken cancellationToken)
    {
        var result = await documentService.OpenDownloadAsync(documentId, cancellationToken);
        if (result is null)
        {
            return NotFound();
        }

        return File(result.Content, result.ContentType, result.FileName, enableRangeProcessing: true);
    }

    [HttpDelete("{documentId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid documentId, CancellationToken cancellationToken)
    {
        var deleted = await documentService.DeleteAsync(documentId, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }
}
