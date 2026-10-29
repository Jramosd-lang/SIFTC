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

        var (statusCode, title, detail, type) = result.Error.Type switch
        {
            ErrorType.Validation => (StatusCodes.Status422UnprocessableEntity, "Solicitud inválida", result.Error.Message, (string?)null),
            ErrorType.NotFound => (StatusCodes.Status404NotFound, "Not Found", "Recurso no encontrado", ProblemDetailsResponse.AboutBlank),
            ErrorType.Unauthorized => (StatusCodes.Status401Unauthorized, "Unauthorized", "Token inválido o vencido", ProblemDetailsResponse.AboutBlank),
            ErrorType.Conflict => (StatusCodes.Status409Conflict, "Conflicto", result.Error.Message, (string?)null),
            ErrorType.Dependency => (StatusCodes.Status503ServiceUnavailable, "Servicio no disponible", result.Error.Message, (string?)null),
            _ => (StatusCodes.Status400BadRequest, "Solicitud incorrecta", result.Error.Message, (string?)null)
        };

        return ProblemDetailsResponse.ToResult(HttpContext, statusCode, result.Error.Code, title, detail, type);
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
