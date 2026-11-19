using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace Lexicon.Infrastructure.Extraction;

public sealed class PdfDocumentExtractor : IDocumentTextExtractor
{
    public bool CanHandle(string contentType, string fileName)
    {
        if (string.Equals(Path.GetExtension(fileName), ".pdf", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var mediaType = contentType.Split(';', 2)[0].Trim();
        return string.Equals(mediaType, "application/pdf", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<ExtractedDocument> ExtractAsync(Stream content, CancellationToken cancellationToken = default)
    {
        Stream sourceStream = content;
        MemoryStream? buffered = null;

        try
        {
            if (!content.CanSeek)
            {
                buffered = new MemoryStream();
                await content.CopyToAsync(buffered, cancellationToken);
                buffered.Position = 0;
                sourceStream = buffered;
            }

            var segments = new List<ExtractedSegment>();
            using var pdf = PdfDocument.Open(sourceStream);

            foreach (var page in pdf.GetPages())
            {
                cancellationToken.ThrowIfCancellationRequested();

                var text = ContentOrderTextExtractor.GetText(page)
                    .Replace("\r\n", "\n")
                    .Replace('\r', '\n')
                    .Trim();

                if (!string.IsNullOrWhiteSpace(text))
                {
                    segments.Add(new ExtractedSegment(text, PageNumber: page.Number));
                }
            }

            return new ExtractedDocument(segments);
        }
        finally
        {
            if (buffered is not null)
            {
                await buffered.DisposeAsync();
            }
        }
    }
}
