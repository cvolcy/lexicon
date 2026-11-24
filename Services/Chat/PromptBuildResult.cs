using Lexicon.Models.Chat;
using AiChatMessage = Microsoft.Extensions.AI.ChatMessage;

namespace Lexicon.Services.Chat;

public sealed record PromptBuildResult(
    IReadOnlyList<AiChatMessage> Messages,
    IReadOnlyList<CitationViewModel> Citations);
