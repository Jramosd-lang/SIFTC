using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIFTC.Api.Common;
using SIFTC.Application.UseCases.Entidades;
using SIFTC.Application.UseCases.Entidades.Queries.ListarEntidades;
using SIFTC.Application.UseCases.Entidades.Queries.ObtenerEntidad;
using SIFTC.Application.UseCases.Entidades.Queries.ObtenerOhlc;

namespace SIFTC.Api.Controllers;

[Authorize]
[Route("api/v1/entidades")]
public sealed class EntidadesController(IMediator mediator) : BaseApiController
{
    [HttpGet]
    [EndpointSummary("Lista las entidades con su cotización actual")]
    [EndpointDescription("Filtro opcional por nombre de sector. page desde 1; size de 1 a 100 (por defecto 100). sort: codigo, nombre, sector, ic o variacionPct; dir: asc o desc. El total de registros viaja en la cabecera X-Total-Count.")]
    [ProducesResponseType<IReadOnlyList<EntidadDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    public async Task<IActionResult> Listar(
        [FromQuery] string? sector,
        [FromQuery] int? page,
        [FromQuery] int? size,
        [FromQuery] string? sort,
        [FromQuery] string? dir,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ListarEntidadesQuery(sector, page, size, sort, dir), cancellationToken);

        if (result.IsFailure)
        {
            return HandleFailure(result);
        }

        Response.Headers[PaginacionHeaders.Total] = result.Value.Total.ToString();
        return Ok(result.Value.Items);
    }

    [HttpGet("{id:guid}")]
    [EndpointSummary("Obtiene una entidad con su cotización actual")]
    [ProducesResponseType<EntidadDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> Obtener(Guid id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ObtenerEntidadQuery(id), cancellationToken);
        return HandleResult(result);
    }

    [HttpGet("{id:guid}/ohlc")]
    [EndpointSummary("Obtiene la serie OHLC de una entidad")]
    [EndpointDescription("periodo usa la frecuencia del sistema: Diaria, Mensual o Trimestral. Devuelve la serie de la fecha más antigua a la más reciente.")]
    [ProducesResponseType<OhlcResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> ObtenerOhlc(
        Guid id,
        [FromQuery] string? periodo,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ObtenerOhlcQuery(id, periodo), cancellationToken);
        return HandleResult(result);
    }
}
