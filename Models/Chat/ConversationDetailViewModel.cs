namespace Lexicon.Models.Chat;

public sealed record ConversationDetailViewModel(
    Guid ConversationId,
    string Title,
    int MessageCount,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<ChatMessageViewModel> Messages)
    : ConversationSummaryViewModel(ConversationId, Title, MessageCount, CreatedAt, UpdatedAt);
