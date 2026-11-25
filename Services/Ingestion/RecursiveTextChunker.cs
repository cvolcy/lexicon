using System.Text;
using Lexicon.Infrastructure.Extraction;
using Microsoft.Extensions.Options;

namespace Lexicon.Services.Ingestion;

public sealed class RecursiveTextChunker(IOptions<ChunkingOptions> options) : IChunker
{
    private static readonly string[] Separators = ["\n\n", "\n", ". ", " "];

    public IReadOnlyList<TextChunk> Chunk(ExtractedDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        var maxTokens = Math.Max(1, options.Value.MaxTokens);
        var overlapTokens = Math.Clamp(options.Value.OverlapTokens, 0, maxTokens - 1);
        var maxChars = maxTokens * 4;
        var overlapChars = overlapTokens * 4;

        var chunks = new List<TextChunk>();
        var index = 0;

        foreach (var segment in document.Segments)
        {
            if (string.IsNullOrWhiteSpace(segment.Text))
            {
                continue;
            }

            var pieces = new List<(string Text, string Joiner)>();
            SplitToPieces(segment.Text.Trim(), separatorIndex: 0, maxTokens, maxChars, defaultJoiner: "\n\n", pieces);

            var packed = PackPieces(pieces, maxTokens, overlapChars);
            foreach (var chunkText in packed)
            {
                chunks.Add(new TextChunk(
                    Index: index++,
                    Text: chunkText,
                    PageNumber: segment.PageNumber,
                    Section: segment.Section,
                    TokenCount: TokenEstimator.Estimate(chunkText)));
            }
        }

        return chunks;
    }

    private static void SplitToPieces(
        string text,
        int separatorIndex,
        int maxTokens,
        int maxChars,
        string defaultJoiner,
        List<(string Text, string Joiner)> output)
    {
        var trimmed = text.Trim();
        if (trimmed.Length == 0)
        {
            return;
        }

        if (TokenEstimator.Estimate(trimmed) <= maxTokens)
        {
            output.Add((trimmed, defaultJoiner));
            return;
        }

        if (separatorIndex >= Separators.Length)
        {
            for (var start = 0; start < trimmed.Length; start += maxChars)
            {
                var length = Math.Min(maxChars, trimmed.Length - start);
                var slice = trimmed.Substring(start, length).Trim();
                if (slice.Length > 0)
                {
                    output.Add((slice, string.Empty));
                }
            }

            return;
        }

        var separator = Separators[separatorIndex];
        var rawParts = trimmed.Split(separator, StringSplitOptions.RemoveEmptyEntries);

        if (rawParts.Length <= 1)
        {
            SplitToPieces(trimmed, separatorIndex + 1, maxTokens, maxChars, defaultJoiner, output);
            return;
        }

        var joiner = separator == ". " ? " " : separator;

        for (var i = 0; i < rawParts.Length; i++)
        {
            var part = rawParts[i].Trim();
            if (part.Length == 0)
            {
                continue;
            }

            if (separator == ". " && i < rawParts.Length - 1 && !part.EndsWith('.'))
            {
                part += ".";
            }

            if (TokenEstimator.Estimate(part) <= maxTokens)
            {
                output.Add((part, joiner));
            }
            else
            {
                SplitToPieces(part, separatorIndex + 1, maxTokens, maxChars, joiner, output);
            }
        }
    }

    private static List<string> PackPieces(
        List<(string Text, string Joiner)> pieces,
        int maxTokens,
        int overlapChars)
    {
        var result = new List<string>();
        var builder = new StringBuilder();

        foreach (var (text, joiner) in pieces)
        {
            if (builder.Length == 0)
            {
                builder.Append(text);
                continue;
            }

            var separator = joiner.Length > 0 ? joiner : " ";
            var candidateLength = builder.Length + separator.Length + text.Length;
            if (TokenEstimator.Estimate(candidateLength) <= maxTokens)
            {
                builder.Append(separator).Append(text);
                continue;
            }

            var completed = builder.ToString().Trim();
            if (completed.Length > 0)
            {
                result.Add(completed);
            }

            builder.Clear();

            if (overlapChars > 0 && completed.Length > 0)
            {
                var overlap = ExtractOverlapSuffix(completed, overlapChars);
                if (overlap.Length > 0
                    && TokenEstimator.Estimate(overlap + "\n" + text) <= maxTokens)
                {
                    builder.Append(overlap).Append('\n').Append(text);
                    continue;
                }
            }

            builder.Append(text);
        }

        var finalChunk = builder.ToString().Trim();
        if (finalChunk.Length > 0)
        {
            result.Add(finalChunk);
        }

        return result;
    }

    private static string ExtractOverlapSuffix(string text, int maxOverlapChars)
    {
        if (text.Length <= maxOverlapChars)
        {
            return text;
        }

        var start = text.Length - maxOverlapChars;
        var nextSpace = text.IndexOfAny([' ', '\n', '\t'], start);
        if (nextSpace > start && nextSpace + 1 < text.Length)
        {
            start = nextSpace + 1;
        }

        return text[start..].Trim();
    }
}
