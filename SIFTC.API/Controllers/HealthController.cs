using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SIFTC.Api.Controllers;

[ApiController]
[Route("health")]
public sealed class HealthController : ControllerBase
{
    [AllowAnonymous]
    [HttpGet]
    [EndpointSummary("Health check")]
    [EndpointDescription("Endpoint público. Devuelve 200 con { status: \"ok\" } cuando la API está en ejecución. No requiere token.")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Get() => Ok(new { status = "ok" });
}
