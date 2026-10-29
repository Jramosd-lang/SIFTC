using MediatR;
using Microsoft.AspNetCore.Mvc;
using SIFTC.Application.Common;
using SIFTC.Application.UseCases.Auth.Commands.AutenticarUsuario;

namespace SIFTC.Api.Controllers;

[Route("api/v1/auth")]
public sealed class AuthController(IMediator mediator) : BaseApiController
{
    [HttpPost("login")]
    [EndpointSummary("Inicia sesión y devuelve el token JWT")]
    [EndpointDescription("Acepta 'usuario' y 'password'. Devuelve token, expiraEn y los datos del usuario autenticado.")]
    [ProducesResponseType<AuthResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    public async Task<IActionResult> Login([FromBody] AutenticarUsuarioCommand command, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }
}
