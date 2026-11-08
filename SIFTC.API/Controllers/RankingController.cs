using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIFTC.Api.Common;
using SIFTC.Application.UseCases.Ranking;
using SIFTC.Application.UseCases.Ranking.Queries.ObtenerRanking;

namespace SIFTC.Api.Controllers;

[Authorize(Roles = RolesSistema.Ranking)]
[ApiController]
[Route("api/v1")]
public sealed class RankingController(IMediator mediator) : BaseApiController
{
    [HttpGet("ranking")]
    [EndpointSummary("Obtiene el ranking general o por dimensión")]
    [EndpointDescription("""
        Roles admitidos: Administrador, Data Steward, Analista financiero, Gerente / Directivo y Auditor.
        Usuario móvil no tiene acceso al ranking.

        Devuelve el ranking de entidades por IC. Si se envía dimension, calcula el puntaje de esa dimensión
        (promedio de sus métricas normalizadas) y ordena por ese puntaje.
        Para Gerente / Directivo, posicion y cambioPosicion se devuelven en null.
        Parámetros:
        - sector: nombre exacto del sector. Vacío = todos.
        - dimension: Rentabilidad, Liquidez, Solvencia o Crecimiento. Vacío = IC general.
        - periodo: diario, mensual o trimestral. Se devuelve en la respuesta.
        - orden: posicion, entidad, sector, ic o variacionPct. Por defecto ic.
        - dir: asc o desc. Por defecto desc.
        - limit: entero entre 1 y 200. Por defecto 50.
        - offset: entero >= 0. Por defecto 0.

        Respuesta: { periodo, total, items: [{ posicion, cambioPosicion, entidadId, entidad, sector, ic, variacionPct }] }.
        cambioPosicion > 0 significa que subió; < 0 que bajó; 0 que mantuvo.
        """)]
    [ProducesResponseType<RankingResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable, "application/problem+json")]
    public async Task<IActionResult> Obtener(
        [FromQuery] string? sector,
        [FromQuery] string? dimension,
        [FromQuery] string? periodo,
        [FromQuery] string? orden,
        [FromQuery] string? dir,
        [FromQuery] int? limit,
        [FromQuery] int? offset,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new ObtenerRankingQuery(
                sector,
                dimension,
                periodo,
                orden,
                dir,
                limit,
                offset,
                Request.Headers.Authorization.ToString()),
            cancellationToken);

        if (result.IsSuccess && User.IsInRole(RolesSistema.Gerente))
        {
            var ranking = result.Value;
            var items = ranking.Items
                .Select(item => item with { Posicion = null, CambioPosicion = null })
                .ToList();

            return Ok(ranking with { Items = items });
        }

        return HandleResult(result);
    }
}
