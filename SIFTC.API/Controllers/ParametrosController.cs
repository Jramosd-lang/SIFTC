using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIFTC.Api.Common;

namespace SIFTC.Api.Controllers;

[ApiController]
[Route("api/v1/parametros")]
public sealed class ParametrosController : BaseApiController
{
    [Authorize(Roles = RolesSistema.Parametros)]
    [HttpPost("actualizar-umbral")]
    [EndpointSummary("Actualiza el umbral de un parámetro")]
    [EndpointDescription("""
        Roles admitidos: Analista financiero y Administrador.

        Endpoint de administración de parámetros. Actualmente devuelve un mensaje de confirmación;
        el parámetro y su valor se conectarán en la HU de administración.
        """)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, "application/problem+json")]
    public IActionResult ActualizarUmbral() => Ok(new { mensaje = "Umbral actualizado" });
}
