namespace Lexicon.Services.Retrieval;

public sealed class RetrievalOptions
{
    public const string SectionName = "Retrieval";

    public int DefaultTopK { get; set; } = 8;

    public int MaxTopK { get; set; } = 50;

    public bool UseKeywordSearch { get; set; } = true;

    public int RrfK { get; set; } = 60;

    public int CandidateMultiplier { get; set; } = 2;
}
