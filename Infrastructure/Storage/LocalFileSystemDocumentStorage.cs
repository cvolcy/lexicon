using Microsoft.Extensions.Options;

namespace Lexicon.Infrastructure.Storage;

public sealed class LocalFileSystemDocumentStorage : IDocumentStorage
{
    private readonly string _rootPath;

    public LocalFileSystemDocumentStorage(
        IOptions<LocalFileSystemStorageOptions> options,
        IHostEnvironment environment)
    {
        var configuredPath = options.Value.RootPath;
        var combinedPath = Path.IsPathRooted(configuredPath)
            ? configuredPath
            : Path.Combine(environment.ContentRootPath, configuredPath);

        _rootPath = Path.GetFullPath(combinedPath)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        Directory.CreateDirectory(_rootPath);
    }

    public async Task<string> SaveAsync(
        Guid documentId,
        string fileName,
        Stream content,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);

        var storageKey = documentId.ToString("N");
        var targetPath = ResolvePath(storageKey);

        Directory.CreateDirectory(_rootPath);

        var tempPath = $"{targetPath}.{Guid.NewGuid():N}.tmp";

        try
        {
            await using (var fileStream = new FileStream(
                tempPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 81920,
                useAsync: true))
            {
                await content.CopyToAsync(fileStream, cancellationToken);
                await fileStream.FlushAsync(cancellationToken);
            }

            File.Move(tempPath, targetPath, overwrite: true);
            return storageKey;
        }
        catch
        {
            if (File.Exists(tempPath))
            {
                try
                {
                    File.Delete(tempPath);
                }
                catch
                {
                    // Best-effort cleanup of temp file.
                }
            }

            throw;
        }
    }

    public Task<Stream?> OpenReadAsync(
        string storageKey,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var fullPath = ResolvePath(storageKey);
        if (!File.Exists(fullPath))
        {
            return Task.FromResult<Stream?>(null);
        }

        Stream stream = new FileStream(
            fullPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 81920,
            useAsync: true);

        return Task.FromResult<Stream?>(stream);
    }

    public Task DeleteAsync(
        string storageKey,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var fullPath = ResolvePath(storageKey);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        return Task.CompletedTask;
    }

    private string ResolvePath(string storageKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storageKey);

        var fullPath = Path.GetFullPath(Path.Combine(_rootPath, storageKey));
        var comparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        var rootWithSeparator = _rootPath + Path.DirectorySeparatorChar;
        if (!fullPath.StartsWith(rootWithSeparator, comparison))
        {
            throw new InvalidOperationException($"Invalid storage key '{storageKey}'.");
        }

        return fullPath;
    }
}
