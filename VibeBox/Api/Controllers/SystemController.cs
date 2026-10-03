using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

/// <summary>
/// Controller for system-related endpoints, such as health checks and status monitoring.
/// </summary>
[ApiController]
[Route("api/system")]
public sealed class SystemController : ControllerBase
{
    /// <summary>
    /// Endpoint to check the health of the API. Returns a simple status message indicating that the API is operational.
    /// </summary>
    /// <returns></returns>
    [HttpGet("ping")]
    public IActionResult Ping()
    {
        return Ok(new { status = "ok" });
    }
}