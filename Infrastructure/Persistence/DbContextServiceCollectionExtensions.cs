using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Lexicon.Infrastructure.Persistence;

public static class DbContextServiceCollectionExtensions
{
    public const string ConnectionStringName = "Lexicon";
    public const string ProviderConfigurationKey = "Database:Provider";

    /// <summary>
    /// Registers <see cref="LexiconDbContext"/> using the provider selected by
    /// <c>Database:Provider</c> (defaults to <see cref="DatabaseProvider.Sqlite"/>) and the
    /// connection string <c>ConnectionStrings:Lexicon</c>.
    /// </summary>
    public static IServiceCollection AddLexiconDbContext(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var provider = configuration.GetValue(ProviderConfigurationKey, DatabaseProvider.Sqlite);

        var connectionString = configuration.GetConnectionString(ConnectionStringName)
            ?? throw new InvalidOperationException(
                $"Connection string '{ConnectionStringName}' is not configured.");

        switch (provider)
        {
            case DatabaseProvider.Sqlite:
                AddSqlite(services, connectionString);
                break;
            default:
                throw new NotSupportedException(
                    $"Database provider '{provider}' (configured in '{ProviderConfigurationKey}') is not supported.");
        }

        return services;
    }

    private static void AddSqlite(IServiceCollection services, string connectionString)
    {
        EnsureSqliteDirectoryExists(connectionString);

        services.AddDbContext<LexiconDbContext>(options => options.UseSqlite(connectionString));
    }

    private static void EnsureSqliteDirectoryExists(string connectionString)
    {
        var dataSource = new SqliteConnectionStringBuilder(connectionString).DataSource;

        if (string.IsNullOrWhiteSpace(dataSource)
            || dataSource == ":memory:"
            || dataSource.StartsWith("file:", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var directory = Path.GetDirectoryName(Path.GetFullPath(dataSource));
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }
}
