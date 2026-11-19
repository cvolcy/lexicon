namespace Lexicon.Infrastructure.Storage;

internal static class LocalFileSystemStoragePath
{
    public static string ResolveRootPath(string configuredPath, string contentRootPath)
    {
        var combinedPath = Path.IsPathRooted(configuredPath)
            ? configuredPath
            : Path.Combine(contentRootPath, configuredPath);

        return Path.GetFullPath(combinedPath)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }
}
