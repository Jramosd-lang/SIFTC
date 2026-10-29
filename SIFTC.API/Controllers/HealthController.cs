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
    [EndpointDescription("Devuelve 200 cuando la API está en ejecución. No requiere autenticación.")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Get() => Ok(new { status = "ok" });
}
