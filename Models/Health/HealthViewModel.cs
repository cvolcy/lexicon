namespace Lexicon.Models.Health;

public sealed record HealthViewModel(
    string Status,
    string Environment,
    DateTimeOffset TimestampUtc);
