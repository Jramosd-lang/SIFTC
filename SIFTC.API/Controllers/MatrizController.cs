using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIFTC.Api.Common;
using SIFTC.Application.UseCases.Matriz.Commands.ValidarMatriz;
using SIFTC.Domain.Common;

namespace SIFTC.Api.Controllers;

[Authorize(Roles = RolesSistema.CargaMatriz)]
[ApiController]
[Route("api/v1")]
public sealed class MatrizController(IMediator mediator) : BaseApiController
{
    [HttpPost("cargas")]
    [Consumes("multipart/form-data")]
    [EndpointSummary("Carga y valida la matriz CSV")]
    [EndpointDescription("""
        Roles admitidos: Analista financiero y Administrador.

        Recibe el archivo CSV de la matriz en el campo multipart 'file' y lo reenvía al microservicio Python.
        El CSV debe estar separado por punto y coma (;), codificado en UTF-8 y contener las 15 columnas del diccionario.

        Respuesta 200: el resultado del ETL.
        Respuesta 422: el CSV tiene errores; la lista viaja en la extensión 'errores'.
        Respuesta 503: el microservicio Python no está disponible.
        """)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable, "application/problem+json")]
    public async Task<IActionResult> Validar([FromForm] IFormFile? file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return HandleFailure(Result.Failure(
                new Error("matriz.file.required", "El archivo CSV es obligatorio.", ErrorType.Validation)));
        }

        await using var stream = file.OpenReadStream();
        var result = await mediator.Send(
            new ValidarMatrizCommand(
                stream,
                file.FileName,
                file.ContentType,
                Request.Headers.Authorization.ToString(),
                ObtenerUsuarioId()),
            cancellationToken);

        if (result.IsFailure)
        {
            return HandleFailure(result);
        }

        if (!result.Value.EsValida)
        {
            var problem = ProblemDetailsResponse.Create(
                HttpContext,
                StatusCodes.Status422UnprocessableEntity,
                "matriz.invalid",
                "Matriz inválida",
                result.Value.Mensaje ?? "El CSV contiene errores de validación.");

            problem.Extensions["errores"] = result.Value.Errores;
            return new ObjectResult(problem)
            {
                StatusCode = StatusCodes.Status422UnprocessableEntity,
                ContentTypes = { "application/problem+json" }
            };
        }

        return Ok(result.Value.Resultado);
    }
}
