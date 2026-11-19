using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Lexicon.Models.Health;

public sealed record HealthViewModel(
    HealthStatus Status,
    string Environment,
    DateTimeOffset TimestampUtc,
    IReadOnlyList<HealthCheckEntryViewModel> Checks);

public sealed record HealthCheckEntryViewModel(
    string Name,
    HealthStatus Status,
    string? Description,
    double DurationMs);
