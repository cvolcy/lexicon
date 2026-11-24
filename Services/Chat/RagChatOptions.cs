namespace Lexicon.Services.Chat;

public sealed class RagChatOptions
{
    public const string SectionName = "Chat";

    public int RetrievalTopK { get; set; } = 6;

    public int MaxHistoryMessages { get; set; } = 10;

    public int MaxHistoryTokens { get; set; } = 2000;

    public int MaxContextTokens { get; set; } = 4000;

    public int MaxMessageLength { get; set; } = 16_000;

    public float Temperature { get; set; } = 0.2f;
}
