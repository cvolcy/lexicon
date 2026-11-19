namespace Lexicon.Infrastructure.Ai;

public sealed record AiModelInfo(
    AiProvider Provider,
    string ChatModel,
    string EmbeddingModel);
