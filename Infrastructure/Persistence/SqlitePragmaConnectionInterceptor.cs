using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Lexicon.Infrastructure.Persistence;

/// <summary>
/// Applies per-connection SQLite PRAGMAs on open so background ingestion and HTTP requests share
/// WAL concurrency cleanly and wait briefly instead of failing immediately on lock contention.
/// </summary>
internal sealed class SqlitePragmaConnectionInterceptor : DbConnectionInterceptor
{
    private const string PragmaSql = """
        PRAGMA busy_timeout = 5000;
        PRAGMA journal_mode = WAL;
        PRAGMA synchronous = NORMAL;
        PRAGMA foreign_keys = ON;
        """;

    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
    {
        using var command = connection.CreateCommand();
        command.CommandText = PragmaSql;
        command.ExecuteNonQuery();
    }

    public override async Task ConnectionOpenedAsync(
        DbConnection connection,
        ConnectionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = PragmaSql;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
