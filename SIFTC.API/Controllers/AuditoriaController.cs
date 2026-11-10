using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIFTC.Api.Common;
using SIFTC.Application.UseCases.Auditoria;
using SIFTC.Application.UseCases.Auditoria.Queries.ListarAuditoria;

namespace SIFTC.Api.Controllers;

[ApiController]
[Route("api/v1/auditoria")]
public sealed class AuditoriaController(IMediator mediator) : BaseApiController
{
    [Authorize(Roles = RolesSistema.AuditoriaLectura)]
    [HttpGet]
    [EndpointSummary("Consulta la bitácora de auditoría")]
    [EndpointDescription("""
        Roles admitidos: Administrador y Auditor.

        Devuelve los eventos críticos más recientes: login fallido, aprobación y rechazo de publicaciones,
        y carga de matriz. Solo lectura.
        Parámetro limit: entero entre 1 y 200. Por defecto 50.
        """)]
    [ProducesResponseType<IReadOnlyList<AuditoriaDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, "application/problem+json")]
    public async Task<IActionResult> Listar([FromQuery] int? limit, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ListarAuditoriaQuery(limit), cancellationToken);

        return HandleResult(result);
    }
}
