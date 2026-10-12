using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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

        // Mapeamos el ErrorType al código HTTP, respetando el Criterio HU-BE-11
        var statusCode = result.Error.Type switch
        {
            ErrorType.Validation => StatusCodes.Status422UnprocessableEntity, // HTTP 422 (HU-BE-11)
            ErrorType.NotFound => StatusCodes.Status404NotFound,              // HTTP 404
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,      // HTTP 401
            ErrorType.Conflict => StatusCodes.Status409Conflict,              // HTTP 409
            _ => StatusCodes.Status400BadRequest                              // HTTP 400 (Default)
        };

        // El método Problem() genera automáticamente el estándar ProblemDetails.
        // NOTA: .NET inyecta el "traceId" automáticamente en la respuesta, no necesitas capturarlo a mano.
        return Problem(
            statusCode: statusCode,
            title: result.Error.Code,
            detail: result.Error.Message
        );
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