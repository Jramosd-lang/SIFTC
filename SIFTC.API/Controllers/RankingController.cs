using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIFTC.Application.UseCases.Ranking;
using SIFTC.Application.UseCases.Ranking.Queries.ObtenerRanking;

namespace SIFTC.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1")]
public sealed class RankingController(IMediator mediator) : BaseApiController
{
    [HttpGet("ranking")]
    [EndpointSummary("Obtiene el ranking desde el microservicio Python")]
    [EndpointDescription("Reenvía la consulta a GET /api/v1/ranking del microservicio. Filtros: sector, dimension (Rentabilidad, Liquidez, Solvencia, Crecimiento), periodo (diario, mensual, trimestral), orden (posicion, entidad, sector, ic, variacionPct), dir (asc, desc), limit (1..200) y offset (>=0).")]
    [ProducesResponseType<RankingResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
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

        return HandleResult(result);
    }
}
