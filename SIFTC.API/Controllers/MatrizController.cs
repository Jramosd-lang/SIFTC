using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIFTC.Api.Common;
using SIFTC.Application.UseCases.Matriz.Commands.ValidarMatriz;
using SIFTC.Domain.Common;

namespace SIFTC.Api.Controllers;

[Authorize(Roles = "Analista financiero,Administrador")]
[ApiController]
[Route("api/v1")]
public sealed class MatrizController(IMediator mediator) : BaseApiController
{
    [HttpPost("cargas")]
    [Consumes("multipart/form-data")]
    [EndpointSummary("Carga y valida la matriz CSV")]
    [EndpointDescription("Recibe el archivo en el campo multipart 'file' y lo reenvía a POST /v1/validar-matriz del microservicio Python. Si el CSV tiene errores, devuelve 422 con la lista de errores en la extensión 'errores'.")]
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
            new ValidarMatrizCommand(stream, file.FileName, file.ContentType),
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
