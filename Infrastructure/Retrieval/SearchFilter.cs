namespace Lexicon.Infrastructure.Retrieval;

public sealed record SearchFilter(IReadOnlyCollection<Guid>? DocumentIds = null)
{
    public static SearchFilter? From(IEnumerable<Guid>? documentIds)
    {
        if (documentIds is null)
        {
            return null;
        }

        var distinct = documentIds.Distinct().ToList();
        return distinct.Count > 0 ? new SearchFilter(distinct) : null;
    }
}
