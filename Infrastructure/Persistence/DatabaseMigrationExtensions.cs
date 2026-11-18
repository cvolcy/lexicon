using Microsoft.EntityFrameworkCore;

namespace Lexicon.Infrastructure.Persistence;

public static class DatabaseMigrationExtensions
{
    /// <summary>Applies any pending EF Core migrations to the Lexicon database.</summary>
    public static async Task MigrateLexiconDatabaseAsync(this WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<LexiconDbContext>();
        var logger = scope.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger(typeof(DatabaseMigrationExtensions).FullName!);

        var pending = (await dbContext.Database.GetPendingMigrationsAsync()).ToList();
        if (pending.Count == 0)
        {
            return;
        }

        logger.LogInformation("Applying {Count} pending database migration(s): {Migrations}", pending.Count, pending);
        await dbContext.Database.MigrateAsync();
    }
}
