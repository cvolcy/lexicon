namespace Lexicon.Services.Ingestion;

public sealed record TextChunk(
    int Index,
    string Text,
    int? PageNumber,
    string? Section,
    int TokenCount);
