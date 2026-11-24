namespace Lexicon.Models.Chat;

public sealed record SendMessageRequest(
    string Content,
    IReadOnlyList<Guid>? DocumentIds = null);
