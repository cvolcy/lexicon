using Lexicon.Infrastructure.Retrieval;

namespace Lexicon.Services.Retrieval;

public interface IRetriever
{
    Task<IReadOnlyList<RetrievedChunk>> RetrieveAsync(
        string query,
        int? topK = null,
        SearchFilter? filter = null,
        CancellationToken cancellationToken = default);
}
