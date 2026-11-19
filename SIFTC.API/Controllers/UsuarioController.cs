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
        Endpoint público (no requiere token de autenticación previo).

        Permite a los usuarios del sistema autenticarse y obtener un token JWT Bearer.

        Cuerpo JSON:
        - usuario: correo electrónico registrado del usuario (ej. 'admin@siftc.local'). El backend utiliza el correo como nombre de usuario para el login.
        - password: contraseña en texto plano; se valida de forma segura contra el hash bcrypt almacenado en `seguridad.usuarios`.

        Comportamiento y Respuestas:
        - Respuesta 200: Objeto `{ token, expiraEn, usuario: { id, nombre, rol } }`.
          El token generado debe enviarse en la cabecera HTTP `Authorization: Bearer <token>` para consumir cualquiera de las rutas protegidas.
        - Respuesta 401: Credenciales inválidas (`auth.credenciales.invalidas`) o usuario desactivado (`auth.inactivo`). Los usuarios con estado 'inactivo' tienen el acceso revocado.
        """)]
    [ProducesResponseType<AuthResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    public async Task<IActionResult> Login([FromBody] AutenticarUsuarioCommand command, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }
}
