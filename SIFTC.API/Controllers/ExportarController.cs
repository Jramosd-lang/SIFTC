using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIFTC.Application.UseCases.Exportar;
using SIFTC.Application.UseCases.Exportar.Queries.ExportarDatos;

namespace SIFTC.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1")]
public sealed class ExportarController(IMediator mediator) : BaseApiController
{
    [HttpGet("exportar")]
    [EndpointSummary("Exporta rankings o series a CSV")]
    [EndpointDescription("""
        Descarga el resultado de una consulta como archivo CSV con separador ';' y decimales
        con coma (la misma convención que el CSV de carga), UTF-8 con BOM.

        Parámetros:
        - tipo: obligatorio. Valores: ranking o serie.
        - formato: opcional. Valor por defecto csv; solo csv está soportado hoy.

        tipo=ranking usa exactamente los mismos filtros de GET /api/v1/ranking:
        - sector, dimension, periodo, orden, dir, limit, offset.

        tipo=serie usa exactamente los mismos filtros de GET /api/v1/entidades/{id}/ohlc:
        - idEntidad (obligatorio) y periodo.

        Permisos (Matriz de permisos SIFTC v1.1 y HU-BE-02):
        - ranking: Administrador, Data Steward, Analista financiero, Gerente / Directivo y Auditor.
          Usuario móvil: 403.
        - serie: los seis roles (las series OHLC son de sensibilidad Pública).
        - Gerente / Directivo exporta el ranking con posicion y cambioPosicion vacíos.

        Bitácora (HU-BE-06): cada exportación exitosa queda registrada en seguridad.auditorias
        con accion=exportacion. Si el registro falla, la exportación responde 503 y no se
        entrega el archivo.

        Respuesta 200: text/csv con Content-Disposition y nombre de archivo
        ranking_aaaaMMdd_HHmmss.csv o serie_<entidad>_<periodo>_aaaaMMdd_HHmmss.csv.
        """)]
    [Produces("text/csv")]
    [ProducesResponseType(typeof(byte[]), StatusCodes.Status200OK, "text/csv")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable, "application/problem+json")]
    public async Task<IActionResult> Exportar(
        [FromQuery] string? tipo,
        [FromQuery] string? formato,
        [FromQuery] string? sector,
        [FromQuery] string? dimension,
        [FromQuery] string? periodo,
        [FromQuery] string? orden,
        [FromQuery] string? dir,
        [FromQuery] int? limit,
        [FromQuery] int? offset,
        [FromQuery] Guid? idEntidad,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new ExportarQuery(
                tipo,
                formato,
                sector,
                dimension,
                periodo,
                orden,
                dir,
                limit,
                offset,
                idEntidad,
                User.FindFirstValue(ClaimTypes.Role),
                ObtenerUsuarioId(),
                Request.Headers.Authorization.ToString(),
                ObtenerIpCliente()),
            cancellationToken);

        if (!result.IsSuccess)
        {
            return HandleFailure(result);
        }

        var archivo = result.Value;
        return File(archivo.Contenido, archivo.ContentType, archivo.FileName);
    }

    private string? ObtenerIpCliente()
    {
        var proxied = Request.Headers["X-Forwarded-For"].FirstOrDefault();
        var ip = proxied?.Split(',')[0].Trim();

        return string.IsNullOrEmpty(ip)
            ? HttpContext.Connection.RemoteIpAddress?.ToString()
            : ip;
    }
}
