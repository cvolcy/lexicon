namespace Lexicon.Models.Chat;

public record ConversationSummaryViewModel(
    Guid ConversationId,
    string Title,
    int MessageCount,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
