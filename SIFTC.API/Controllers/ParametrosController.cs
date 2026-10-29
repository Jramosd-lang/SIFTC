using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SIFTC.Api.Controllers;

[ApiController]
[Route("api/v1/parametros")]
public sealed class ParametrosController : BaseApiController
{
    [Authorize(Roles = "Analista financiero,Administrador")]
    [HttpPost("actualizar-umbral")]
    [EndpointSummary("Actualiza el umbral de un parámetro")]
    [EndpointDescription("Endpoint de administración reservado para Analista financiero o Administrador.")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, "application/problem+json")]
    public IActionResult ActualizarUmbral() => Ok(new { mensaje = "Umbral actualizado" });
}
