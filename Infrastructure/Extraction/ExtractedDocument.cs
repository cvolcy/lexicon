namespace Lexicon.Infrastructure.Extraction;

public sealed record ExtractedDocument(IReadOnlyList<ExtractedSegment> Segments);
