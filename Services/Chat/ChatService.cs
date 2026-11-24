using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Lexicon.Infrastructure.Persistence;
using Lexicon.Infrastructure.Retrieval;
using Lexicon.Models.Chat;
using Lexicon.Services.Retrieval;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using AiChatOptions = Microsoft.Extensions.AI.ChatOptions;

namespace Lexicon.Services.Chat;

public sealed partial class ChatService(
    LexiconDbContext dbContext,
    IRetriever retriever,
    IPromptBuilder promptBuilder,
    IChatClient chatClient,
    IOptions<RagChatOptions> options,
    ILogger<ChatService> logger) : IChatService
{
    private const string DefaultTitle = "New conversation";
    private const int MaxTitleLength = 200;
    private const int AutoTitleMaxLength = 60;

    private const string NoRelevantPassagesReply =
        "I couldn't find any relevant passages in your indexed documents to answer that question. Try rephrasing your question, clearing any document filters, or uploading relevant documents first.";

    private static readonly JsonSerializerOptions CitationsJsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<IReadOnlyList<ConversationSummaryViewModel>> ListConversationsAsync(
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Conversations
            .AsNoTracking()
            .OrderByDescending(c => c.UpdatedAt)
            .Select(c => new ConversationSummaryViewModel(
                c.ConversationId,
                c.Title,
                c.Messages.Count,
                c.CreatedAt,
                c.UpdatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<ConversationDetailViewModel?> GetConversationAsync(
        Guid conversationId,
        CancellationToken cancellationToken = default)
    {
        var conversation = await dbContext.Conversations
            .AsNoTracking()
            .Where(c => c.ConversationId == conversationId)
            .Select(c => new
            {
                c.ConversationId,
                c.Title,
                c.CreatedAt,
                c.UpdatedAt,
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (conversation is null)
        {
            return null;
        }

        var messageRows = await dbContext.ConversationMessages
            .AsNoTracking()
            .Where(m => m.ConversationId == conversationId)
            .OrderBy(m => m.CreatedAt)
            .ThenBy(m => m.MessageId)
            .ToListAsync(cancellationToken);

        var messages = messageRows
            .Select(m => new ChatMessageViewModel(
                m.MessageId,
                m.ConversationId,
                m.Role,
                m.Content,
                DeserializeCitations(m.CitationsJson),
                m.CreatedAt))
            .ToList();

        return new ConversationDetailViewModel(
            conversation.ConversationId,
            conversation.Title,
            messages.Count,
            conversation.CreatedAt,
            conversation.UpdatedAt,
            messages);
    }

    public async Task<ConversationSummaryViewModel> CreateConversationAsync(
        string? title = null,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var conversation = new Conversation
        {
            ConversationId = Guid.CreateVersion7(),
            Title = NormalizeTitle(title),
            CreatedAt = now,
            UpdatedAt = now,
        };

        dbContext.Conversations.Add(conversation);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new ConversationSummaryViewModel(
            conversation.ConversationId,
            conversation.Title,
            0,
            conversation.CreatedAt,
            conversation.UpdatedAt);
    }

    public async Task<bool> DeleteConversationAsync(
        Guid conversationId,
        CancellationToken cancellationToken = default)
    {
        var conversation = await dbContext.Conversations
            .SingleOrDefaultAsync(c => c.ConversationId == conversationId, cancellationToken);

        if (conversation is null)
        {
            return false;
        }

        dbContext.Conversations.Remove(conversation);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> ConversationExistsAsync(
        Guid conversationId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Conversations
            .AsNoTracking()
            .AnyAsync(c => c.ConversationId == conversationId, cancellationToken);
    }

    public async IAsyncEnumerable<ChatStreamEvent> StreamReplyAsync(
        Guid conversationId,
        string content,
        IReadOnlyList<Guid>? documentIds = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var conversation = await dbContext.Conversations
            .SingleOrDefaultAsync(c => c.ConversationId == conversationId, cancellationToken);

        if (conversation is null)
        {
            yield return new ErrorChatStreamEvent("Conversation not found.");
            yield break;
        }

        var priorMessages = await dbContext.ConversationMessages
            .AsNoTracking()
            .Where(m => m.ConversationId == conversationId)
            .OrderBy(m => m.CreatedAt)
            .ThenBy(m => m.MessageId)
            .ToListAsync(cancellationToken);

        var trimmedContent = content.Trim();
        var userTimestamp = DateTimeOffset.UtcNow;
        var userMessage = new ConversationMessage
        {
            MessageId = Guid.CreateVersion7(),
            ConversationId = conversationId,
            Role = ConversationMessageRole.User,
            Content = trimmedContent,
            CreatedAt = userTimestamp,
        };

        if (priorMessages.Count == 0 && string.Equals(conversation.Title, DefaultTitle, StringComparison.Ordinal))
        {
            conversation.Title = SummarizeTitle(trimmedContent);
        }

        conversation.UpdatedAt = userTimestamp;
        dbContext.ConversationMessages.Add(userMessage);
        await dbContext.SaveChangesAsync(cancellationToken);

        var opts = options.Value;
        var filter = documentIds is { Count: > 0 } ? new SearchFilter(documentIds) : null;

        IReadOnlyList<RetrievedChunk> retrievedChunks = [];
        string? retrievalError = null;
        try
        {
            retrievedChunks = await retriever.RetrieveAsync(
                trimmedContent,
                opts.RetrievalTopK,
                filter,
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            yield break;
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to retrieve context passages for conversation {ConversationId}.",
                conversationId);
            retrievalError = "Failed to retrieve document passages. Verify the AI embedding provider is reachable on System Health.";
        }

        if (retrievalError is not null)
        {
            yield return new ErrorChatStreamEvent(retrievalError);
            yield break;
        }

        var prompt = promptBuilder.Build(trimmedContent, priorMessages, retrievedChunks);

        if (prompt.Citations.Count > 0)
        {
            yield return new CitationsChatStreamEvent(prompt.Citations);
        }
        else
        {
            yield return new TokenChatStreamEvent(NoRelevantPassagesReply);
            var fallbackMessage = await SaveAssistantMessageAsync(
                conversation,
                NoRelevantPassagesReply,
                [],
                cancellationToken);
            yield return new DoneChatStreamEvent(fallbackMessage);
            yield break;
        }

        var contentBuilder = new StringBuilder();
        var chatOptions = new AiChatOptions { Temperature = opts.Temperature };
        string? streamError = null;
        var cancelled = false;

        IAsyncEnumerator<ChatResponseUpdate>? enumerator = null;
        try
        {
            enumerator = chatClient
                .GetStreamingResponseAsync(prompt.Messages, chatOptions, cancellationToken)
                .GetAsyncEnumerator(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            cancelled = true;
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to start chat response stream for conversation {ConversationId}.",
                conversationId);
            streamError = "Failed to start AI response stream. Verify Ollama is reachable and the chat model is available.";
        }

        if (streamError is not null)
        {
            yield return new ErrorChatStreamEvent(streamError);
            yield break;
        }

        if (cancelled || enumerator is null)
        {
            yield break;
        }

        await using (enumerator)
        {
            while (true)
            {
                bool hasNext;
                try
                {
                    hasNext = await enumerator.MoveNextAsync();
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    cancelled = true;
                    break;
                }
                catch (Exception ex)
                {
                    logger.LogError(
                        ex,
                        "Chat response streaming failed for conversation {ConversationId}.",
                        conversationId);
                    streamError = "AI response streaming failed. Verify Ollama is running and the chat model is available.";
                    break;
                }

                if (!hasNext)
                {
                    break;
                }

                var tokenText = enumerator.Current.Text;
                if (!string.IsNullOrEmpty(tokenText))
                {
                    contentBuilder.Append(tokenText);
                    yield return new TokenChatStreamEvent(tokenText);
                }
            }
        }

        if (streamError is not null)
        {
            yield return new ErrorChatStreamEvent(streamError);
            yield break;
        }

        if (cancelled)
        {
            if (contentBuilder.Length > 0)
            {
                await SaveAssistantMessageAsync(
                    conversation,
                    contentBuilder.ToString(),
                    prompt.Citations,
                    CancellationToken.None);
            }

            yield break;
        }

        var finalContent = contentBuilder.Length > 0
            ? contentBuilder.ToString()
            : "The AI model returned an empty response.";

        if (contentBuilder.Length == 0)
        {
            yield return new TokenChatStreamEvent(finalContent);
        }

        var assistantMessage = await SaveAssistantMessageAsync(
            conversation,
            finalContent,
            prompt.Citations,
            cancellationToken);

        yield return new DoneChatStreamEvent(assistantMessage);
    }

    private async Task<ChatMessageViewModel> SaveAssistantMessageAsync(
        Conversation conversation,
        string content,
        IReadOnlyList<CitationViewModel> citations,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var citationsJson = citations.Count > 0
            ? JsonSerializer.Serialize(citations, CitationsJsonOptions)
            : null;

        var entity = new ConversationMessage
        {
            MessageId = Guid.CreateVersion7(),
            ConversationId = conversation.ConversationId,
            Role = ConversationMessageRole.Assistant,
            Content = content,
            CitationsJson = citationsJson,
            CreatedAt = now,
        };

        conversation.UpdatedAt = now;
        dbContext.ConversationMessages.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new ChatMessageViewModel(
            entity.MessageId,
            entity.ConversationId,
            entity.Role,
            entity.Content,
            citations,
            entity.CreatedAt);
    }

    private IReadOnlyList<CitationViewModel> DeserializeCitations(string? citationsJson)
    {
        if (string.IsNullOrWhiteSpace(citationsJson))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<CitationViewModel>>(citationsJson, CitationsJsonOptions) ?? [];
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Failed to deserialize stored citations JSON; returning empty citations.");
            return [];
        }
    }

    private static string NormalizeTitle(string? title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return DefaultTitle;
        }

        var trimmed = title.Trim();
        return trimmed.Length <= MaxTitleLength ? trimmed : trimmed[..MaxTitleLength];
    }

    private static string SummarizeTitle(string prompt)
    {
        var collapsed = WhitespaceRegex().Replace(prompt, " ").Trim();
        if (string.IsNullOrEmpty(collapsed))
        {
            return DefaultTitle;
        }

        return collapsed.Length <= AutoTitleMaxLength
            ? collapsed
            : $"{collapsed[..(AutoTitleMaxLength - 3)].TrimEnd()}...";
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}
