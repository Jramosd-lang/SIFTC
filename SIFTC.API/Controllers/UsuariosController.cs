using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIFTC.Api.Common;
using SIFTC.Application.UseCases.Usuarios.Commands;

namespace SIFTC.Api.Controllers;

[Route("api/v1/usuarios")]
[ApiController]
public class UsuariosController(IMediator mediator) : BaseApiController
{
    [Authorize(Roles = RolesSistema.Administrador)]
    [HttpPost("registrar")]
    [EndpointSummary("Registra un usuario")]
    [EndpointDescription("""
        Roles admitidos: Administrador.

        Cuerpo JSON:
        - email: correo único del usuario.
        - password: contraseña en texto plano; se guarda con bcrypt.
        - primerNombre: primer nombre.
        - primerApellido: primer apellido.
        - idRol: UUID de un rol existente en seguridad.roles.

        Respuesta 200: UUID del usuario creado.
        Respuesta 409: el correo ya existe.
        """)]
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
