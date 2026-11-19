namespace Lexicon.Services.Documents;

public sealed class UploadOptions
{
    public const string SectionName = "Upload";

    public long MaxFileSizeBytes { get; set; } = 25 * 1024 * 1024;

    public string[] AllowedExtensions { get; set; } = [".txt", ".md", ".pdf"];
}
