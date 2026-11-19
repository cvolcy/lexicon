namespace Lexicon.Infrastructure.Ai;

public sealed class OllamaOptions
{
    public const string SectionName = "Ai:Ollama";

    public string Endpoint { get; set; } = "http://localhost:11434";

    public string ChatModel { get; set; } = "lfm3:1.7b";

    public string EmbeddingModel { get; set; } = "nomic-embed-text";

    public int RequestTimeoutSeconds { get; set; } = 120;

    public int HealthCheckTimeoutSeconds { get; set; } = 5;
}
