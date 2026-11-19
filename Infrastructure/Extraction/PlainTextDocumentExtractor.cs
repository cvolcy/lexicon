namespace Lexicon.Infrastructure.Extraction;

public sealed class PlainTextDocumentExtractor : IDocumentTextExtractor
{
    public bool CanHandle(string contentType, string fileName)
    {
        if (string.Equals(Path.GetExtension(fileName), ".txt", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var mediaType = contentType.Split(';', 2)[0].Trim();
        return string.Equals(mediaType, "text/plain", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<ExtractedDocument> ExtractAsync(Stream content, CancellationToken cancellationToken = default)
    {
        using var reader = new StreamReader(content, leaveOpen: true);
        var raw = await reader.ReadToEndAsync(cancellationToken);
        var normalized = raw.Replace("\r\n", "\n").Replace('\r', '\n').Trim();

        IReadOnlyList<ExtractedSegment> segments = string.IsNullOrWhiteSpace(normalized)
            ? []
            : [new ExtractedSegment(normalized)];

        return new ExtractedDocument(segments);
    }
}
