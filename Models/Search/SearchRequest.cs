using System.ComponentModel.DataAnnotations;

namespace Lexicon.Models.Search;

public sealed record SearchRequest(
    [Required, MinLength(1), StringLength(2000)] string Query,
    [Range(1, 50)] int? TopK = null,
    IReadOnlyCollection<Guid>? DocumentIds = null);
