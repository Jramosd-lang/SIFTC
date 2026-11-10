using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIFTC.Api.Common;
using SIFTC.Application.UseCases.Entidades;
using SIFTC.Application.UseCases.Entidades.Queries.ListarEntidades;
using SIFTC.Application.UseCases.Entidades.Queries.ObtenerEntidad;
using SIFTC.Application.UseCases.Entidades.Queries.ObtenerOhlc;

namespace SIFTC.Api.Controllers;

[Authorize(Roles = RolesSistema.Consulta)]
[Route("api/v1/entidades")]
public sealed class EntidadesController(IMediator mediator) : BaseApiController
{
    [HttpGet]
    [EndpointSummary("Lista las entidades con su cotización actual")]
    [EndpointDescription("""
        Roles admitidos: Administrador, Data Steward, Analista financiero, Gerente / Directivo, Auditor y Usuario móvil.

        Devuelve el ticker y el tablero de entidades con su último IC y variación.
        Parámetros:
        - sector: nombre exacto del sector. Ejemplo: Energía. Vacío = todos.
        - page: entero >= 1. Por defecto 1.
        - size: entero entre 1 y 100. Por defecto 100.
        - sort: codigo, nombre, sector, ic o variacionPct. Por defecto codigo.
        - dir: asc o desc. Por defecto asc.
        Valores permitidos de sort: codigo, nombre, sector, ic, variacionPct.
        Valores permitidos de dir: asc, desc.
        El total de registros viaja en la cabecera X-Total-Count.

        Respuesta: arreglo de { id, codigo, nombre, sector, ic, variacion, variacionPct }.
        """)]
    [ProducesResponseType<IReadOnlyList<EntidadDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, "application/problem+json")]
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
    [EndpointDescription("""
        Roles admitidos: Administrador, Data Steward, Analista financiero, Gerente / Directivo, Auditor y Usuario móvil.

        Parámetro de ruta:
        - id: UUID de la entidad. Ejemplo: 4ebbf629-0d2d-46ef-b233-93421e13eaee.

        Devuelve { id, codigo, nombre, sector, ic, variacion, variacionPct }.
        Responde 404 si la entidad no existe.
        """)]
    [ProducesResponseType<EntidadDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> Obtener(Guid id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ObtenerEntidadQuery(id), cancellationToken);
        return HandleResult(result);
    }

    [HttpGet("{id:guid}/ohlc")]
    [EndpointSummary("Obtiene la serie OHLC de una entidad")]
    [EndpointDescription("""
        Roles admitidos: Administrador, Data Steward, Analista financiero, Gerente / Directivo, Auditor y Usuario móvil.

        Parámetros:
        - id: UUID de la entidad.
        - periodo: Diaria, Mensual o Trimestral. Vacío = Diaria.
        Valores permitidos de periodo: Diaria, Mensual, Trimestral.
        La serie se devuelve de la fecha más antigua a la más reciente.

        Nota: el CSV es mensual. En Diaria y Mensual cada punto tiene apertura=máximo=mínimo=cierre;
        en Trimestral se agrupan tres meses y la vela tiene rango real. Para gráficas de línea usar Mensual;
        para velas usar Trimestral.

        Respuesta: { entidadId, entidad, periodo, serie: [{ fecha, apertura, maximo, minimo, cierre, variacion, variacionPct }] }.
        """)]
    [ProducesResponseType<OhlcResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, "application/problem+json")]
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
