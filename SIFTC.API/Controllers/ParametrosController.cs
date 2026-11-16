using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIFTC.Api.Common;
using SIFTC.Application.UseCases.Parametros;
using SIFTC.Application.UseCases.Parametros.Queries.ObtenerParametros;

namespace SIFTC.Api.Controllers;

[ApiController]
[Route("api/v1/parametros")]
public sealed class ParametrosController(IMediator mediator) : BaseApiController
{
    [Authorize(Roles = RolesSistema.ParametrosLectura)]
    [HttpGet]
    [EndpointSummary("Lee los pesos del IC, las fórmulas y las frecuencias")]
    [EndpointDescription("""
        Roles admitidos: Administrador, Analista financiero y Auditor.

        Solo lectura. Devuelve los pesos de cada dimensión del Índice de Competitividad
        y el catálogo de métricas con su fórmula y su frecuencia. No incluye credenciales
        ni configuración de infraestructura.
        """)]
    [ProducesResponseType<ParametrosDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, "application/problem+json")]
    public async Task<IActionResult> Obtener(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ObtenerParametrosQuery(), cancellationToken);

        return HandleResult(result);
    }

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
