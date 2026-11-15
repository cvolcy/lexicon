using Microsoft.AspNetCore.Mvc;

namespace Lexicon.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController(IHostEnvironment env) : ControllerBase
{
    [HttpGet]
    public IActionResult Index()
    {
        return Ok(new
        {
            Status = "ok",
            Environment = env.EnvironmentName,
            TimestampUtc = DateTimeOffset.UtcNow
        });
    }
}