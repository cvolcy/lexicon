namespace Lexicon.Infrastructure.Storage;

public interface IDocumentStorage
{
    /// <summary>
    /// Saves the document content and returns an opaque storage key used to retrieve or delete it later.
    /// </summary>
    Task<string> SaveAsync(
        Guid documentId,
        string fileName,
        Stream content,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Opens a readable stream for the stored document, or returns <see langword="null"/> if it does not exist.
    /// </summary>
    Task<Stream?> OpenReadAsync(
        string storageKey,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the stored document if it exists. Succeeds silently if the file is already absent.
    /// </summary>
    Task DeleteAsync(
        string storageKey,
        CancellationToken cancellationToken = default);
}
