using Lexicon.Models.Documents;

namespace Lexicon.Services.Documents;

public interface IDocumentService
{
    Task<IReadOnlyList<DocumentViewModel>> ListAsync(CancellationToken cancellationToken = default);

    Task<DocumentViewModel?> GetAsync(Guid documentId, CancellationToken cancellationToken = default);

    Task<DocumentViewModel> CreateAsync(
        string fileName,
        string contentType,
        long sizeBytes,
        Stream content,
        CancellationToken cancellationToken = default);

    Task<DocumentDownloadResult?> OpenDownloadAsync(Guid documentId, CancellationToken cancellationToken = default);

    Task<DocumentViewModel?> ReindexAsync(Guid documentId, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid documentId, CancellationToken cancellationToken = default);
}
