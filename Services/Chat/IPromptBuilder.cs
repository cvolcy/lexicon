using Lexicon.Models.Chat;
using Lexicon.Services.Retrieval;

namespace Lexicon.Services.Chat;

public interface IPromptBuilder
{
    PromptBuildResult Build(
        string userQuestion,
        IReadOnlyList<ConversationMessage> priorHistory,
        IReadOnlyList<RetrievedChunk> retrievedChunks);
}
