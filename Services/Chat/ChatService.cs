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
using AiChatMessage = Microsoft.Extensions.AI.ChatMessage;
using AiChatOptions = Microsoft.Extensions.AI.ChatOptions;

namespace Lexicon.Services.Chat;

public sealed partial class ChatService(
    LexiconDbContext dbContext,
    IRetriever retriever,
    IPromptBuilder promptBuilder,
    IChatClient chatClient,
    IOptions<RagChatOptions> options,
    TimeProvider timeProvider,
    ILogger<ChatService> logger) : IChatService
{
    private const int AutoTitleMaxLength = 60;

    private const string NoRelevantPassagesReply =
        "I couldn't find any relevant passages in your indexed documents to answer that question. Try rephrasing your question, clearing any document filters, or uploading relevant documents first.";

    private const string EmptyModelResponseReply = "The AI model returned an empty response.";

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
        var now = timeProvider.GetUtcNow();
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

    public async Task<IAsyncEnumerable<ChatStreamEvent>?> StartReplyStreamAsync(
        Guid conversationId,
        string content,
        IReadOnlyList<Guid>? documentIds = null,
        CancellationToken cancellationToken = default)
    {
        var conversation = await dbContext.Conversations
            .SingleOrDefaultAsync(c => c.ConversationId == conversationId, cancellationToken);

        if (conversation is null)
        {
            return null;
        }

        var priorMessages = await dbContext.ConversationMessages
            .AsNoTracking()
            .Where(m => m.ConversationId == conversationId)
            .OrderBy(m => m.CreatedAt)
            .ThenBy(m => m.MessageId)
            .ToListAsync(cancellationToken);

        var trimmedContent = content.Trim();
        await RecordUserMessageAsync(conversation, trimmedContent, priorMessages.Count == 0, cancellationToken);

        return StreamAssistantReplyAsync(conversation, trimmedContent, priorMessages, documentIds);
    }

    private async Task RecordUserMessageAsync(
        Conversation conversation,
        string trimmedContent,
        bool isFirstTurn,
        CancellationToken cancellationToken)
    {
        var userTimestamp = timeProvider.GetUtcNow();
        var userMessage = new ConversationMessage
        {
            MessageId = Guid.CreateVersion7(),
            ConversationId = conversation.ConversationId,
            Role = ConversationMessageRole.User,
            Content = trimmedContent,
            CreatedAt = userTimestamp,
        };

        if (isFirstTurn && string.Equals(conversation.Title, Conversation.DefaultTitle, StringComparison.Ordinal))
        {
            conversation.Title = SummarizeTitle(trimmedContent);
        }

        conversation.UpdatedAt = userTimestamp;
        dbContext.ConversationMessages.Add(userMessage);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async IAsyncEnumerable<ChatStreamEvent> StreamAssistantReplyAsync(
        Conversation conversation,
        string trimmedContent,
        IReadOnlyList<ConversationMessage> priorMessages,
        IReadOnlyList<Guid>? documentIds,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var opts = options.Value;
        var retrieval = await TryRetrieveContextAsync(
            conversation.ConversationId,
            trimmedContent,
            opts.RetrievalTopK,
            documentIds,
            cancellationToken);

        if (retrieval.Cancelled)
        {
            yield break;
        }

        if (retrieval.ErrorMessage is not null)
        {
            yield return new ErrorChatStreamEvent(retrieval.ErrorMessage);
            yield break;
        }

        var prompt = promptBuilder.Build(trimmedContent, priorMessages, retrieval.Chunks);
        if (prompt.Citations.Count == 0)
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

        yield return new CitationsChatStreamEvent(prompt.Citations);

        var streamOpen = TryOpenModelStream(
            conversation.ConversationId,
            prompt.Messages,
            opts.Temperature,
            cancellationToken);

        if (streamOpen.Cancelled)
        {
            yield break;
        }

        if (streamOpen.ErrorMessage is not null)
        {
            yield return new ErrorChatStreamEvent(streamOpen.ErrorMessage);
            yield break;
        }

        if (streamOpen.Enumerator is null)
        {
            yield break;
        }

        var contentBuilder = new StringBuilder();
        string? streamError = null;
        var cancelled = false;

        await using (streamOpen.Enumerator)
        {
            while (true)
            {
                bool hasNext;
                try
                {
                    hasNext = await streamOpen.Enumerator.MoveNextAsync();
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
                        conversation.ConversationId);
                    streamError = "AI response streaming failed. Verify Ollama is running and the chat model is available.";
                    break;
                }

                if (!hasNext)
                {
                    break;
                }

                var tokenText = streamOpen.Enumerator.Current.Text;
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
            : EmptyModelResponseReply;

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

    private async Task<RetrievalAttempt> TryRetrieveContextAsync(
        Guid conversationId,
        string query,
        int topK,
        IReadOnlyList<Guid>? documentIds,
        CancellationToken cancellationToken)
    {
        var filter = SearchFilter.From(documentIds);

        try
        {
            var chunks = await retriever.RetrieveAsync(query, topK, filter, cancellationToken);
            return new RetrievalAttempt(chunks, ErrorMessage: null, Cancelled: false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return new RetrievalAttempt([], ErrorMessage: null, Cancelled: true);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to retrieve context passages for conversation {ConversationId}.",
                conversationId);
            return new RetrievalAttempt(
                [],
                "Failed to retrieve document passages. Verify the AI embedding provider is reachable on System Health.",
                Cancelled: false);
        }
    }

    private StreamOpenAttempt TryOpenModelStream(
        Guid conversationId,
        IReadOnlyList<AiChatMessage> messages,
        float temperature,
        CancellationToken cancellationToken)
    {
        try
        {
            var enumerator = chatClient
                .GetStreamingResponseAsync(messages, new AiChatOptions { Temperature = temperature }, cancellationToken)
                .GetAsyncEnumerator(cancellationToken);

            return new StreamOpenAttempt(enumerator, ErrorMessage: null, Cancelled: false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return new StreamOpenAttempt(null, ErrorMessage: null, Cancelled: true);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to start chat response stream for conversation {ConversationId}.",
                conversationId);
            return new StreamOpenAttempt(
                null,
                "Failed to start AI response stream. Verify Ollama is reachable and the chat model is available.",
                Cancelled: false);
        }
    }

    private async Task<ChatMessageViewModel> SaveAssistantMessageAsync(
        Conversation conversation,
        string content,
        IReadOnlyList<CitationViewModel> citations,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
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
            return Conversation.DefaultTitle;
        }

        var trimmed = title.Trim();
        return trimmed.Length <= Conversation.MaxTitleLength
            ? trimmed
            : trimmed[..Conversation.MaxTitleLength];
    }

    private static string SummarizeTitle(string prompt)
    {
        var collapsed = WhitespaceRegex().Replace(prompt, " ").Trim();
        if (string.IsNullOrEmpty(collapsed))
        {
            return Conversation.DefaultTitle;
        }

        return collapsed.Length <= AutoTitleMaxLength
            ? collapsed
            : $"{collapsed[..(AutoTitleMaxLength - 3)].TrimEnd()}...";
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();

    private sealed record RetrievalAttempt(
        IReadOnlyList<RetrievedChunk> Chunks,
        string? ErrorMessage,
        bool Cancelled);

    private sealed record StreamOpenAttempt(
        IAsyncEnumerator<ChatResponseUpdate>? Enumerator,
        string? ErrorMessage,
        bool Cancelled);
}
