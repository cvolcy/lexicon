using Lexicon.Infrastructure.Extraction;

namespace Lexicon.Services.Ingestion;

public interface IChunker
{
    IReadOnlyList<TextChunk> Chunk(ExtractedDocument document);
}
