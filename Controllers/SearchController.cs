using Lexicon.Infrastructure.Retrieval;
using Lexicon.Models;
using Lexicon.Models.Search;
using Lexicon.Services.Retrieval;
using Microsoft.AspNetCore.Mvc;

namespace Lexicon.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SearchController(IRetriever retriever) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<CollectionResponse<SearchResultViewModel>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Search(SearchRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Query))
        {
            ModelState.AddModelError(nameof(request.Query), "Query cannot be empty or whitespace.");
            return ValidationProblem(ModelState);
        }

        var filter = request.DocumentIds is { Count: > 0 }
            ? new SearchFilter(request.DocumentIds)
            : null;

        var results = await retriever.RetrieveAsync(
            request.Query.Trim(),
            request.TopK,
            filter,
            cancellationToken);

        var items = results.Select(SearchResultViewModel.From).ToList();
        return Ok(new CollectionResponse<SearchResultViewModel>(items));
    }
}
