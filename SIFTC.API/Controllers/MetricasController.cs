using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIFTC.Api.Common;
using SIFTC.Application.UseCases.Metricas;
using SIFTC.Application.UseCases.Metricas.Queries.ListarMetricas;

namespace SIFTC.Api.Controllers;

[Authorize]
[Route("api/v1/metricas")]
public sealed class MetricasController(IMediator mediator) : BaseApiController
{
    [HttpGet]
    [EndpointSummary("Lista las métricas calculadas por entidad y fecha")]
    [EndpointDescription("periodo filtra por frecuencia: diario, mensual o trimestral. fecha usa ISO 8601 (yyyy-MM-dd). Filtros opcionales: sector y dimension (Rentabilidad, Liquidez, Solvencia, Crecimiento, Mercado o Trazabilidad). page desde 1; size de 1 a 100 (por defecto 100). sort: fecha, codigo, nombre, sector, dimension, codigoMetrica, valor o valorNormalizado; dir: asc o desc. El total de registros viaja en la cabecera X-Total-Count.")]
    [ProducesResponseType<IReadOnlyList<MetricaDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    public async Task<IActionResult> Listar(
        [FromQuery] string? sector,
        [FromQuery] string? periodo,
        [FromQuery] string? fecha,
        [FromQuery] string? dimension,
        [FromQuery] int? page,
        [FromQuery] int? size,
        [FromQuery] string? sort,
        [FromQuery] string? dir,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ListarMetricasQuery(sector, periodo, fecha, dimension, page, size, sort, dir), cancellationToken);

        if (result.IsFailure)
        {
            return HandleFailure(result);
        }

        Response.Headers[PaginacionHeaders.Total] = result.Value.Total.ToString();
        return Ok(result.Value.Items);
    }
}
