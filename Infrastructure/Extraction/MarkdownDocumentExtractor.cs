namespace Lexicon.Infrastructure.Extraction;

public sealed class MarkdownDocumentExtractor : IDocumentTextExtractor
{
    public bool CanHandle(string contentType, string fileName)
    {
        var extension = Path.GetExtension(fileName);
        if (string.Equals(extension, ".md", StringComparison.OrdinalIgnoreCase)
            || string.Equals(extension, ".markdown", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var mediaType = contentType.Split(';', 2)[0].Trim();
        return string.Equals(mediaType, "text/markdown", StringComparison.OrdinalIgnoreCase)
            || string.Equals(mediaType, "text/x-markdown", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<ExtractedDocument> ExtractAsync(Stream content, CancellationToken cancellationToken = default)
    {
        using var reader = new StreamReader(content, leaveOpen: true);
        var raw = await reader.ReadToEndAsync(cancellationToken);
        var normalized = raw.Replace("\r\n", "\n").Replace('\r', '\n');

        if (string.IsNullOrWhiteSpace(normalized))
        {
            return new ExtractedDocument([]);
        }

        var segments = new List<ExtractedSegment>();
        var buffer = new List<string>();
        string? currentSection = null;
        var inFence = false;

        foreach (var line in normalized.Split('\n'))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var trimmedStart = line.TrimStart();
            if (trimmedStart.StartsWith("```", StringComparison.Ordinal)
                || trimmedStart.StartsWith("~~~", StringComparison.Ordinal))
            {
                inFence = !inFence;
                buffer.Add(line);
                continue;
            }

            if (!inFence && TryParseAtxHeading(line, out var heading))
            {
                FlushSegment(segments, buffer, currentSection);
                currentSection = heading;
                buffer.Add(line);
                continue;
            }

            buffer.Add(line);
        }

        FlushSegment(segments, buffer, currentSection);
        return new ExtractedDocument(segments);
    }

    private static void FlushSegment(List<ExtractedSegment> segments, List<string> buffer, string? section)
    {
        if (buffer.Count == 0)
        {
            return;
        }

        var text = string.Join('\n', buffer).Trim();
        buffer.Clear();

        if (!string.IsNullOrWhiteSpace(text))
        {
            segments.Add(new ExtractedSegment(text, Section: section));
        }
    }

    private static bool TryParseAtxHeading(string line, out string heading)
    {
        heading = string.Empty;

        var span = line.AsSpan();
        var leadingSpaces = 0;
        while (leadingSpaces < span.Length && span[leadingSpaces] == ' ')
        {
            leadingSpaces++;
        }

        if (leadingSpaces > 3)
        {
            return false;
        }

        span = span[leadingSpaces..];

        var hashes = 0;
        while (hashes < span.Length && span[hashes] == '#')
        {
            hashes++;
        }

        if (hashes is < 1 or > 6 || hashes >= span.Length || (span[hashes] != ' ' && span[hashes] != '\t'))
        {
            return false;
        }

        var text = span[(hashes + 1)..].Trim().TrimEnd('#').Trim();
        if (text.Length == 0)
        {
            return false;
        }

        heading = text.ToString();
        return true;
    }
}
