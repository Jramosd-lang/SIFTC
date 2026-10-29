using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIFTC.Application.UseCases.Usuarios.Commands;

namespace SIFTC.Api.Controllers;

[Route("api/v1/usuarios")]
[ApiController]
public class UsuariosController(IMediator mediator) : BaseApiController
{
    [Authorize(Roles = "Administrador")]
    [HttpPost("registrar")]
    [EndpointSummary("Registra un usuario")]
    [EndpointDescription("Requiere rol Administrador. Devuelve el identificador UUID del usuario creado.")]
    [ProducesResponseType<Guid>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> Registrar([FromBody] CrearUsuarioCommand command, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }
}
