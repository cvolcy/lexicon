using Lexicon.Infrastructure.Ai;
using Lexicon.Infrastructure.Extraction;
using Lexicon.Infrastructure.Persistence;
using Lexicon.Infrastructure.Retrieval;
using Lexicon.Infrastructure.Storage;
using Lexicon.Models.Documents;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;

namespace Lexicon.Services.Ingestion;

public sealed class DocumentIngestionProcessor(
    LexiconDbContext dbContext,
    IDocumentStorage documentStorage,
    IEnumerable<IDocumentTextExtractor> extractors,
    IChunker chunker,
    IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
    AiModelInfo aiModelInfo,
    IVectorStore vectorStore,
    IKeywordIndex keywordIndex,
    TimeProvider timeProvider,
    ILogger<DocumentIngestionProcessor> logger) : IDocumentIngestionProcessor
{
    private const int MaxErrorLength = 2000;
    private const int EmbeddingBatchSize = 32;

    public async Task ProcessAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        var document = await dbContext.Documents
            .SingleOrDefaultAsync(d => d.DocumentId == documentId, cancellationToken);

        if (document is null)
        {
            logger.LogDebug("Skipping ingestion for missing document {DocumentId}.", documentId);
            return;
        }

        document.Status = DocumentStatus.Processing;
        document.Error = null;
        document.UpdatedAt = timeProvider.GetUtcNow();
        await dbContext.SaveChangesAsync(cancellationToken);

        try
        {
            var extractor = extractors.FirstOrDefault(e => e.CanHandle(document.ContentType, document.FileName));
            if (extractor is null)
            {
                var extension = Path.GetExtension(document.FileName);
                await MarkFailedAsync(
                    documentId,
                    $"Unsupported file format '{(string.IsNullOrEmpty(extension) ? document.ContentType : extension)}'.",
                    cancellationToken);
                return;
            }

            await using var stream = await documentStorage.OpenReadAsync(document.StorageKey, cancellationToken);
            if (stream is null)
            {
                await MarkFailedAsync(documentId, "Stored file could not be found.", cancellationToken);
                return;
            }

            var extracted = await extractor.ExtractAsync(stream, cancellationToken);
            var chunks = chunker.Chunk(extracted);
            if (chunks.Count == 0)
            {
                await MarkFailedAsync(documentId, "No extractable text was found in the document.", cancellationToken);
                return;
            }

            var embeddings = await GenerateEmbeddingsAsync(chunks, cancellationToken);

            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

            var stillExists = await dbContext.Documents
                .AsNoTracking()
                .AnyAsync(d => d.DocumentId == documentId, cancellationToken);

            if (!stillExists)
            {
                logger.LogInformation(
                    "Skipping commit for document {DocumentId} ({FileName}) because it was deleted during ingestion.",
                    documentId,
                    document.FileName);
                return;
            }

            await vectorStore.DeleteByDocumentAsync(documentId, cancellationToken);
            await keywordIndex.DeleteByDocumentAsync(documentId, cancellationToken);

            await dbContext.DocumentChunks
                .Where(c => c.DocumentId == documentId)
                .ExecuteDeleteAsync(cancellationToken);

            var chunkEntities = chunks.Select(chunk => new DocumentChunk
            {
                ChunkId = Guid.CreateVersion7(),
                DocumentId = documentId,
                Index = chunk.Index,
                Text = chunk.Text,
                PageNumber = chunk.PageNumber,
                Section = chunk.Section,
                TokenCount = chunk.TokenCount,
            }).ToList();

            dbContext.DocumentChunks.AddRange(chunkEntities);
            await dbContext.SaveChangesAsync(cancellationToken);

            var vectorRecords = chunkEntities
                .Zip(embeddings, (chunk, vector) => new ChunkVectorRecord(
                    chunk.ChunkId,
                    documentId,
                    vector,
                    aiModelInfo.EmbeddingModel))
                .ToList();

            await vectorStore.UpsertAsync(vectorRecords, cancellationToken);

            var indexedChunks = chunkEntities
                .Select(chunk => new IndexedChunkRecord(chunk.ChunkId, documentId, chunk.Text))
                .ToList();

            await keywordIndex.IndexAsync(indexedChunks, cancellationToken);

            document.ChunkCount = chunkEntities.Count;
            document.Status = DocumentStatus.Ready;
            document.Error = null;
            document.UpdatedAt = timeProvider.GetUtcNow();

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            logger.LogInformation(
                "Ingested document {DocumentId} ({FileName}) into {ChunkCount} chunk(s) using embedding model '{EmbeddingModel}'.",
                documentId,
                document.FileName,
                chunkEntities.Count,
                aiModelInfo.EmbeddingModel);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Ingestion failed for document {DocumentId} ({FileName}).", documentId, document.FileName);
            await MarkFailedAsync(documentId, FormatSafeErrorMessage(ex), CancellationToken.None);
        }
    }

    private async Task<List<ReadOnlyMemory<float>>> GenerateEmbeddingsAsync(
        IReadOnlyList<TextChunk> chunks,
        CancellationToken cancellationToken)
    {
        var vectors = new List<ReadOnlyMemory<float>>(chunks.Count);

        foreach (var batch in chunks.Chunk(EmbeddingBatchSize))
        {
            var inputs = batch.Select(c => c.Text).ToList();
            var generated = await embeddingGenerator.GenerateAsync(inputs, cancellationToken: cancellationToken);

            if (generated.Count != inputs.Count)
            {
                throw new InvalidOperationException(
                    $"Embedding generator returned {generated.Count} vector(s) for {inputs.Count} chunk(s).");
            }

            foreach (var embedding in generated)
            {
                vectors.Add(embedding.Vector);
            }
        }

        return vectors;
    }

    private async Task MarkFailedAsync(Guid documentId, string reason, CancellationToken cancellationToken)
    {
        dbContext.ChangeTracker.Clear();

        var truncatedError = reason.Length > MaxErrorLength ? reason[..MaxErrorLength] : reason;
        var now = timeProvider.GetUtcNow();

        await dbContext.Documents
            .Where(d => d.DocumentId == documentId)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(d => d.Status, DocumentStatus.Failed)
                    .SetProperty(d => d.Error, truncatedError)
                    .SetProperty(d => d.UpdatedAt, now),
                cancellationToken);
    }

    private static string FormatSafeErrorMessage(Exception ex) => ex switch
    {
        HttpRequestException => "Failed to reach the AI embedding provider while indexing the document.",
        TaskCanceledException or TimeoutException => "Timed out while generating embeddings for the document.",
        InvalidDataException or FormatException => "Document content could not be read or parsed.",
        _ => "Document ingestion failed unexpectedly. Check server logs for details.",
    };
}
