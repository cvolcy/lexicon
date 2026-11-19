namespace Lexicon.Infrastructure.Retrieval;

public sealed record SearchFilter(IReadOnlyCollection<Guid>? DocumentIds = null);
