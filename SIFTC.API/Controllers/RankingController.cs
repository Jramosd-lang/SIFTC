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
        Usuario móvil no tiene acceso al ranking (devuelve 403 Forbidden).

        Devuelve el ranking comparativo de entidades según su Índice de Competitividad (IC) o por dimensión financiera.
        Si se especifica `dimension`, calcula el puntaje ponderado de dicha dimensión (promedio de sus métricas normalizadas) y ordena según ese puntaje.

        Política de visualización para roles directivos:
        Para el rol `Gerente / Directivo`, los atributos `posicion` y `cambioPosicion` se devuelven en `null` por cumplimiento de políticas de visualización ejecutiva.

        Parámetros de consulta (Query Params):
        - `sector` (opcional): Nombre exacto del sector económico (ej. 'Financiero', 'Energía'). Si se omite o va vacío, incluye todos los sectores.
        - `dimension` (opcional): Dimensión analítica a evaluar ('Rentabilidad', 'Liquidez', 'Solvencia' o 'Crecimiento'). Si se omite, calcula el IC general.
        - `periodo` (opcional): Frecuencia temporal ('diario'/'Diaria', 'mensual'/'Mensual', 'trimestral'/'Trimestral'). Por defecto 'Mensual'.
        - `orden` (opcional): Criterio de ordenación ('posicion', 'entidad', 'sector', 'ic', 'variacionPct'). Por defecto 'ic'.
        - `dir` (opcional): Dirección del orden ('asc' o 'desc'). Por defecto 'desc'.
        - `limit` (opcional): Entero entre 1 y 200. Por defecto 50.
        - `offset` (opcional): Entero mayor o igual a 0 para desplazamiento paginado. Por defecto 0.

        Estructura de respuesta:
        - `periodo`: Frecuencia consultada.
        - `total`: Total de entidades que cumplen los filtros.
        - `items`: Arreglo de objetos `{ posicion, cambioPosicion, entidadId, entidad, sector, ic, variacionPct }`.
          `cambioPosicion > 0` indica mejora de puesto (subió), `< 0` indica descenso (bajó) y `0` que mantuvo el puesto.
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

