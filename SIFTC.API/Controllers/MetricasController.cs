using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIFTC.Api.Common;
using SIFTC.Application.UseCases.Metricas;
using SIFTC.Application.UseCases.Metricas.Queries.ListarMetricas;

namespace SIFTC.Api.Controllers;

[Authorize(Roles = RolesSistema.Consulta)]
[Route("api/v1/metricas")]
public sealed class MetricasController(IMediator mediator) : BaseApiController
{
    [HttpGet]
    [EndpointSummary("Lista las métricas calculadas por entidad y fecha")]
    [EndpointDescription("""
        Roles admitidos: Administrador, Data Steward, Analista financiero, Gerente / Directivo, Auditor y Usuario móvil.

        Devuelve una fila por métrica calculada por la capa analítica.
        Parámetros:
        - sector: nombre exacto del sector. Vacío = todos.
        - periodo: frecuencia de la métrica: diario, mensual o trimestral. Vacío = todas.
        - fecha: periodo exacto en ISO 8601 (yyyy-MM-dd). Ejemplo: 2026-09-01. Vacío = todos.
        - dimension: Rentabilidad, Liquidez, Solvencia, Crecimiento, Mercado o Trazabilidad.
        - page: entero >= 1. Por defecto 1.
        - size: entero entre 1 y 100. Por defecto 100.
        - sort: fecha, codigo, nombre, sector, dimension, codigoMetrica, valor o valorNormalizado.
        - dir: asc o desc.
        Valores permitidos de periodo: diario (o diaria), mensual, trimestral.
        Valores permitidos de dimension: Rentabilidad, Liquidez, Solvencia, Crecimiento, Mercado, Trazabilidad.
        Valores permitidos de sort: fecha, codigo, nombre, sector, dimension, codigoMetrica, valor, valorNormalizado.
        Valores permitidos de dir: asc, desc.
        El total de registros viaja en la cabecera X-Total-Count.

        La fecha de los datos del corte va del 2024-10-01 al 2026-09-01. Una fecha fuera de ese rango devuelve 0 filas.
        Respuesta: arreglo de { entidadId, codigo, nombre, sector, fecha, codigoMetrica, nombreMetrica, dimension, formula, frecuencia, valor, valorNormalizado }.
        """)]
    [ProducesResponseType<IReadOnlyList<MetricaDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, "application/problem+json")]
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
