namespace Lexicon.Infrastructure.Storage;

public sealed class LocalFileSystemStorageOptions
{
    public const string SectionName = "Storage:LocalFileSystem";

    /// <summary>
    /// Directory where uploaded document files are stored. Relative paths are resolved
    /// against the application's content root.
    /// </summary>
    public string RootPath { get; set; } = "data/files";
}
