using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIFTC.Api.Common;
using SIFTC.Application.UseCases.MatrizCruda;
using SIFTC.Application.UseCases.MatrizCruda.Queries.ListarMatrizCruda;

namespace SIFTC.Api.Controllers;

[ApiController]
[Route("api/v1/matriz-cruda")]
public sealed class MatrizCrudaController(IMediator mediator) : BaseApiController
{
    [Authorize(Roles = RolesSistema.MatrizCrudaLectura)]
    [HttpGet]
    [EndpointSummary("Lee las filas de la matriz financiera cruda")]
    [EndpointDescription("""
        Roles admitidos: Data Steward y Auditor.

        Solo lectura. Devuelve las filas tal como llegaron en la carga, sin transformar.
        Los datos son confidenciales: no se exponen a ningún otro rol.
        Parámetro limit: entero entre 1 y 200. Por defecto 50.
        """)]
    [ProducesResponseType<IReadOnlyList<MatrizCrudaDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, "application/problem+json")]
    public async Task<IActionResult> Listar([FromQuery] int? limit, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ListarMatrizCrudaQuery(limit), cancellationToken);

        return HandleResult(result);
    }
}
