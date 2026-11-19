using Lexicon.Infrastructure.Persistence;
using Lexicon.Models.Documents;
using Lexicon.Services.Ingestion;
using Microsoft.EntityFrameworkCore;

namespace Lexicon.Infrastructure.Ingestion;

public sealed class IngestionWorker(
    IIngestionQueue queue,
    IServiceScopeFactory scopeFactory,
    ILogger<IngestionWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await RecoverInterruptedDocumentsAsync(stoppingToken);

        await foreach (var documentId in queue.DequeueAllAsync(stoppingToken))
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var processor = scope.ServiceProvider.GetRequiredService<IDocumentIngestionProcessor>();
                await processor.ProcessAsync(documentId, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unhandled error while processing ingestion job for document {DocumentId}.", documentId);
            }
        }
    }

    private async Task RecoverInterruptedDocumentsAsync(CancellationToken stoppingToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<LexiconDbContext>();

        var pendingIds = await dbContext.Documents
            .AsNoTracking()
            .Where(d => d.Status == DocumentStatus.Pending || d.Status == DocumentStatus.Processing)
            .OrderBy(d => d.CreatedAt)
            .Select(d => d.DocumentId)
            .ToListAsync(stoppingToken);

        if (pendingIds.Count == 0)
        {
            return;
        }

        logger.LogInformation("Recovering {Count} pending/interrupted ingestion job(s) on startup.", pendingIds.Count);

        foreach (var documentId in pendingIds)
        {
            await queue.EnqueueAsync(documentId, stoppingToken);
        }
    }
}
