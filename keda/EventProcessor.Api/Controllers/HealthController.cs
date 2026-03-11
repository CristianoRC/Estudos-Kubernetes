using Microsoft.AspNetCore.Mvc;

namespace EventProcessor.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class HealthController : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<HealthResult>(StatusCodes.Status200OK)]
    public IActionResult Get() => Ok(new HealthResult("healthy", DateTimeOffset.UtcNow));
}

public record HealthResult(string Status, DateTimeOffset Timestamp);
