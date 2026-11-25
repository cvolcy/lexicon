using Lexicon.Models.Health;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Lexicon.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController(
    IHostEnvironment env,
    HealthCheckService healthCheckService,
    TimeProvider timeProvider) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<HealthViewModel>(StatusCodes.Status200OK)]
    [ProducesResponseType<HealthViewModel>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var report = await healthCheckService.CheckHealthAsync(cancellationToken);

        var checks = report.Entries
            .OrderBy(e => e.Key, StringComparer.OrdinalIgnoreCase)
            .Select(e => new HealthCheckEntryViewModel(
                Name: e.Key,
                Status: e.Value.Status,
                Description: e.Value.Description ?? (e.Value.Status == HealthStatus.Unhealthy ? "Check failed." : null),
                DurationMs: Math.Round(e.Value.Duration.TotalMilliseconds, 2)))
            .ToList();

        var viewModel = new HealthViewModel(
            Status: report.Status,
            Environment: env.EnvironmentName,
            TimestampUtc: timeProvider.GetUtcNow(),
            Checks: checks);

        var statusCode = report.Status == HealthStatus.Unhealthy
            ? StatusCodes.Status503ServiceUnavailable
            : StatusCodes.Status200OK;

        return StatusCode(statusCode, viewModel);
    }
}
