using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SIFTC.Api.Common;
using SIFTC.Domain.Common;

namespace SIFTC.Api.Controllers;

[ApiController]
public abstract class BaseApiController : ControllerBase
{
    protected IActionResult HandleFailure(Result result)
    {
        if (result.IsSuccess)
        {
            throw new InvalidOperationException("No se puede procesar un resultado exitoso como fallo.");
        }

        var (statusCode, title) = result.Error.Type switch
        {
            ErrorType.Validation => (StatusCodes.Status422UnprocessableEntity, "Solicitud inválida"),
            ErrorType.NotFound => (StatusCodes.Status404NotFound, "Recurso no encontrado"),
            ErrorType.Unauthorized => (StatusCodes.Status401Unauthorized, "No autorizado"),
            ErrorType.Conflict => (StatusCodes.Status409Conflict, "Conflicto"),
            _ => (StatusCodes.Status400BadRequest, "Solicitud incorrecta")
        };

        var problem = ProblemDetailsResponse.Create(HttpContext, statusCode, result.Error.Code, title, result.Error.Message);
        problem.Extensions["code"] = result.Error.Code;
        return new ObjectResult(problem) { StatusCode = statusCode, ContentTypes = { "application/problem+json" } };
    }

    // Para Queries y Commands que devuelven datos (Ej. Login)
    protected IActionResult HandleResult<T>(Result<T> result)
    {
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    // Para Commands que no devuelven datos (Ej. ActivarDesarrollador)
    protected IActionResult HandleResult(Result result)
    {
        return result.IsSuccess ? Ok() : HandleFailure(result);
    }
}
