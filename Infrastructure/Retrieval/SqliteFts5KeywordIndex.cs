using System.Text.RegularExpressions;
using Lexicon.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Lexicon.Infrastructure.Retrieval;

/// <summary>
/// SQLite FTS5 keyword index backed by the <c>ChunksFts</c> virtual table.
/// Uses <see cref="Microsoft.EntityFrameworkCore.Infrastructure.DatabaseFacade"/> SQL APIs
/// (<c>ExecuteSqlInterpolatedAsync</c> / <c>SqlQuery</c>) rather than a <c>DbSet&lt;T&gt;</c>
/// because EF Core does not translate SQLite FTS5 virtual table syntax (<c>MATCH</c>, <c>bm25()</c>),
/// while still keeping parameterization, connection lifetime, and active transactions managed by <see cref="LexiconDbContext"/>.
/// </summary>
public sealed partial class SqliteFts5KeywordIndex(LexiconDbContext dbContext) : IKeywordIndex
{
    private const int MaxQueryTokens = 24;

    [GeneratedRegex(@"[\p{L}\p{N}]+", RegexOptions.CultureInvariant)]
    private static partial Regex TokenRegex();

    public async Task IndexAsync(
        IReadOnlyList<IndexedChunkRecord> chunks,
        CancellationToken cancellationToken = default)
    {
        if (chunks.Count == 0)
        {
            return;
        }

        foreach (var chunk in chunks)
        {
            var chunkId = FormatGuid(chunk.ChunkId);
            var documentId = FormatGuid(chunk.DocumentId);

            await dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"""
                INSERT INTO "ChunksFts" ("ChunkId", "DocumentId", "Text")
                VALUES ({chunkId}, {documentId}, {chunk.Text});
                """,
                cancellationToken);
        }
    }

    public Task DeleteByDocumentAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        var formattedId = FormatGuid(documentId);

        return dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""
            DELETE FROM "ChunksFts"
            WHERE "DocumentId" = {formattedId};
            """,
            cancellationToken);
    }

    public async Task<IReadOnlyList<ScoredChunk>> SearchAsync(
        string query,
        int topK,
        SearchFilter? filter = null,
        CancellationToken cancellationToken = default)
    {
        if (topK <= 0 || string.IsNullOrWhiteSpace(query))
        {
            return [];
        }

        var ftsQuery = BuildFtsQuery(query);
        if (ftsQuery is null)
        {
            return [];
        }

        // Omit trailing semicolon so EF Core can compose Where/OrderBy/Take over this query.
        var matches = dbContext.Database.SqlQuery<FtsMatchRow>(
            $"""
            SELECT "ChunkId", "DocumentId", -bm25("ChunksFts") AS "Score"
            FROM "ChunksFts"
            WHERE "ChunksFts" MATCH {ftsQuery}
            """);

        if (filter?.DocumentIds is { Count: > 0 } documentIds)
        {
            var formattedIds = documentIds.Select(FormatGuid).ToList();
            matches = matches.Where(m => formattedIds.Contains(m.DocumentId));
        }

        var rows = await matches
            .OrderByDescending(m => m.Score)
            .Take(topK)
            .ToListAsync(cancellationToken);

        return rows
            .Select(r => new ScoredChunk(Guid.Parse(r.ChunkId), Guid.Parse(r.DocumentId), r.Score))
            .ToList();
    }

    private static string FormatGuid(Guid value) => value.ToString("D").ToUpperInvariant();

    private static string? BuildFtsQuery(string query)
    {
        var distinctTokens = TokenRegex()
            .Matches(query)
            .Select(m => m.Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (distinctTokens.Count == 0)
        {
            return null;
        }

        var hasMultiCharToken = distinctTokens.Any(t => t.Length >= 2);
        var selectedTokens = (hasMultiCharToken
                ? distinctTokens.Where(t => t.Length >= 2)
                : distinctTokens)
            .Take(MaxQueryTokens)
            .ToList();

        return string.Join(" OR ", selectedTokens.Select(t => $"\"{t}\""));
    }

    private sealed class FtsMatchRow
    {
        public string ChunkId { get; init; } = string.Empty;
        public string DocumentId { get; init; } = string.Empty;
        public double Score { get; init; }
    }
}
