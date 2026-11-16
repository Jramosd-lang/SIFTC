using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIFTC.Api.Common;
using SIFTC.Application.UseCases.Diccionario;
using SIFTC.Application.UseCases.Diccionario.Commands.CargarDiccionario;
using SIFTC.Application.UseCases.Diccionario.Queries.BuscarDiccionario;
using SIFTC.Application.UseCases.Diccionario.Queries.ObtenerFicha;
using SIFTC.Domain.Common;

namespace SIFTC.Api.Controllers;

// HU-DD-08: diccionario de datos. La búsqueda y la ficha las usa cualquier
// usuario con sesión (RolesSistema.Consulta); la carga es de administradores.
[Authorize(Roles = RolesSistema.Consulta)]
[ApiController]
[Route("api/v1")]
public sealed class DiccionarioController(IMediator mediator) : BaseApiController
{
    [HttpPost("diccionario/cargar")]
    [Authorize(Roles = RolesSistema.DiccionarioCarga)]
    [Consumes("multipart/form-data")]
    [EndpointSummary("Carga diccionario.csv y reemplaza la tabla")]
    [EndpointDescription("""
        Roles admitidos: Administrador y Data Steward (403 para el resto).

        Recibe el archivo diccionario.csv en el campo multipart 'file' y reemplaza
        el contenido completo de la tabla analitica.diccionario: el cargue siempre
        deja la tabla igual al Excel de Frell.

        Formato del CSV: separador punto y coma (;), UTF-8 y fila de encabezado con
        las 8 columnas (codigo, tipo, nombre, definicion, sinonimos, formula, unidad,
        clasificacion). El orden de las columnas no importa y las que sobren se ignoran.

        Respuesta 200: {"registros": n} con las filas insertadas.
        Respuesta 422: el archivo no es válido; la tabla NO se modifica. Códigos:
        diccionario.csv.vacio, diccionario.columnas.faltan (faltan columnas del
        encabezado), diccionario.fila.invalida (valores fuera de catálogo o vacíos
        obligatorios), diccionario.codigo.duplicado, diccionario.csv.ilegible.
        Respuesta 503: diccionario.carga.error (fallo al reemplazar la tabla).
        """)]
    [ProducesResponseType(typeof(CargaDiccionarioResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable, "application/problem+json")]
    public async Task<IActionResult> Cargar([FromForm] IFormFile? file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return HandleFailure(Result.Failure(
                new Error("diccionario.file.required", "El archivo diccionario.csv es obligatorio.", ErrorType.Validation)));
        }

        await using var stream = file.OpenReadStream();
        var result = await mediator.Send(new CargarDiccionarioCommand(stream), cancellationToken);
        return HandleResult(result);
    }

    [HttpGet("diccionario")]
    [EndpointSummary("Busca términos del diccionario por nombre, código o sinónimo")]
    [EndpointDescription("""
        Cualquier usuario con sesión. Búsqueda sin distinguir mayúsculas (ILIKE)
        sobre nombre, código y sinónimos.

        Parámetros:
        - buscar (obligatorio): texto a buscar.
        - tipo (opcional): dato, metrica o termino.

        Devuelve como máximo 20 resultados, como arreglo con los campos
        codigo, tipo, nombre y definicion.

        Filtro por clasificación de sensibilidad (HU-DD-06, sección 2.4 de la HU):
        - Público e Interno: los seis roles.
        - Confidencial: Administrador, Data Steward, Analista financiero y Auditor.
        - Restringido: nadie vía API (nunca aparece en los resultados).

        Respuesta 422: diccionario.buscar.required o diccionario.tipo.invalid.
        """)]
    [ProducesResponseType(typeof(IReadOnlyList<DiccionarioResultadoDto>), StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity, "application/problem+json")]
    public async Task<IActionResult> Buscar(
        [FromQuery] string? buscar,
        [FromQuery] string? tipo,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new BuscarDiccionarioQuery(buscar, tipo, User.FindFirstValue(ClaimTypes.Role)),
            cancellationToken);
        return HandleResult(result);
    }

    [HttpGet("diccionario/{codigo}")]
    [EndpointSummary("Devuelve la ficha completa de un término del diccionario")]
    [EndpointDescription("""
        Cualquier usuario con sesión. Devuelve codigo, tipo, nombre, definicion,
        formula, unidad, sinonimos (arreglo) y clasificacion.

        Respuesta 404 cuando el código no existe Y cuando el rol no puede ver la
        clasificación (Confidencial solo para Administrador, Data Steward, Analista
        financiero y Auditor; Restringido para nadie). Es el mismo 404 en ambos
        casos a propósito: así ningún usuario puede enumerar los términos que no
        le corresponden.
        """)]
    [ProducesResponseType(typeof(DiccionarioFichaDto), StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> ObtenerFicha(string codigo, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new ObtenerFichaQuery(codigo, User.FindFirstValue(ClaimTypes.Role)),
            cancellationToken);
        return HandleResult(result);
    }
}
