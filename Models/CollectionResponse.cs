namespace Lexicon.Models;

public record CollectionResponse<T>(IReadOnlyList<T> Items, string? NextLink = null);
