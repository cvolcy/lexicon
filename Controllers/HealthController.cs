using Lexicon.Models.Health;
using Microsoft.AspNetCore.Mvc;

namespace Lexicon.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController(IHostEnvironment env) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<HealthViewModel>(StatusCodes.Status200OK)]
    public IActionResult Index() => Ok(new HealthViewModel(
        Status: "ok",
        Environment: env.EnvironmentName,
        TimestampUtc: DateTimeOffset.UtcNow));
}
