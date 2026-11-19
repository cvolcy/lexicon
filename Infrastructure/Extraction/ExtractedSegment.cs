namespace Lexicon.Infrastructure.Extraction;

public sealed record ExtractedSegment(
    string Text,
    int? PageNumber = null,
    string? Section = null);
