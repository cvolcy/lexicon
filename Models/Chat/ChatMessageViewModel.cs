namespace Lexicon.Models.Chat;

public sealed record ChatMessageViewModel(
    Guid MessageId,
    Guid ConversationId,
    ConversationMessageRole Role,
    string Content,
    IReadOnlyList<CitationViewModel> Citations,
    DateTimeOffset CreatedAt);
