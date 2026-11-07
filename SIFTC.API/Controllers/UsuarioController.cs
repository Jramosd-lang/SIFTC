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
    [EndpointDescription("""
        Endpoint público (no requiere token).

        Cuerpo JSON:
        - usuario: correo o usuario registrado.
        - password: contraseña en texto plano.

        Respuesta 200: { token, expiraEn, usuario: { id, nombre, rol } }.
        El token debe enviarse como Bearer en los demás endpoints.
        Respuesta 401: credenciales incorrectas.
        """)]
    [ProducesResponseType<AuthResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    public async Task<IActionResult> Login([FromBody] AutenticarUsuarioCommand command, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }
}
