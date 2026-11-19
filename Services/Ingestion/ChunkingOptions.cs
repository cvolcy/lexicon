namespace Lexicon.Services.Ingestion;

public sealed class ChunkingOptions
{
    public const string SectionName = "Ingestion:Chunking";

    public int MaxTokens { get; set; } = 500;

    public int OverlapTokens { get; set; } = 50;
}
