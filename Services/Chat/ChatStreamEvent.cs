using Lexicon.Models.Chat;

namespace Lexicon.Services.Chat;

public abstract record ChatStreamEvent;

public sealed record CitationsChatStreamEvent(IReadOnlyList<CitationViewModel> Citations) : ChatStreamEvent;

public sealed record TokenChatStreamEvent(string Content) : ChatStreamEvent;

public sealed record DoneChatStreamEvent(ChatMessageViewModel Message) : ChatStreamEvent;

public sealed record ErrorChatStreamEvent(string Message) : ChatStreamEvent;
