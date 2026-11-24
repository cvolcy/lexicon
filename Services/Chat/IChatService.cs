using Lexicon.Models.Chat;

namespace Lexicon.Services.Chat;

public interface IChatService
{
    Task<IReadOnlyList<ConversationSummaryViewModel>> ListConversationsAsync(CancellationToken cancellationToken = default);

    Task<ConversationDetailViewModel?> GetConversationAsync(
        Guid conversationId,
        CancellationToken cancellationToken = default);

    Task<ConversationSummaryViewModel> CreateConversationAsync(
        string? title = null,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteConversationAsync(Guid conversationId, CancellationToken cancellationToken = default);

    Task<bool> ConversationExistsAsync(Guid conversationId, CancellationToken cancellationToken = default);

    IAsyncEnumerable<ChatStreamEvent> StreamReplyAsync(
        Guid conversationId,
        string content,
        IReadOnlyList<Guid>? documentIds = null,
        CancellationToken cancellationToken = default);
}
