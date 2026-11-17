using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIFTC.Api.Common;
using SIFTC.Application.UseCases.Usuarios;
using SIFTC.Application.UseCases.Usuarios.Commands;
using SIFTC.Application.UseCases.Usuarios.Commands.ActualizarUsuario;
using SIFTC.Application.UseCases.Usuarios.Commands.EliminarUsuario;
using SIFTC.Application.UseCases.Usuarios.Queries.ListarUsuarios;
using SIFTC.Domain.Common;

namespace SIFTC.Api.Controllers;

[Route("api/v1/usuarios")]
[ApiController]
[Authorize(Roles = RolesSistema.Administrador)]
public class UsuariosController(IMediator mediator) : BaseApiController
{
    [HttpGet]
    [EndpointSummary("Lista los usuarios activos")]
    [EndpointDescription("""
        Roles admitidos: Administrador.

        Devuelve solo los usuarios con estado 'activo', ordenados por apellido y
        nombre. Las bajas lógicas (estado 'inactivo') no aparecen en el listado.

        Cada fila trae idUsuario, email, nombres, apellidos, idRol, rol y estado.
        Nunca incluye el hash de la contraseña.
        """)]
    [ProducesResponseType(typeof(IReadOnlyList<UsuarioDto>), StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, "application/problem+json")]
    public async Task<IActionResult> Listar(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ListarUsuariosQuery(), cancellationToken);
        return HandleResult(result);
    }

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

    [HttpPut("{id:guid}")]
    [EndpointSummary("Edita los datos de un usuario")]
    [EndpointDescription("""
        Roles admitidos: Administrador.

        Cuerpo JSON (todos los datos se reemplazan; el id va en la ruta):
        - email: correo único; si cambia, no puede estar usado por otro usuario.
        - primerNombre, primerApellido: obligatorios.
        - segundoNombre, segundoApellido: opcionales.
        - idRol: UUID de un rol existente.
        - password: opcional; si viene, se guarda el nuevo hash. Si se omite o
          llega vacío, la contraseña actual se conserva.

        Respuesta 200: el usuario actualizado (sin el hash de la contraseña).
        Respuesta 404: usuario.no.encontrado.
        Respuesta 409: usuario.duplicado (correo de otro usuario).
        Respuesta 422: datos inválidos o usuario.rol.invalido.
        """)]
    [ProducesResponseType(typeof(UsuarioDto), StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity, "application/problem+json")]
    public async Task<IActionResult> Actualizar(
        Guid id,
        [FromBody] ActualizarUsuarioSolicitud solicitud,
        CancellationToken cancellationToken)
    {
        var command = new ActualizarUsuarioCommand(
            id,
            solicitud.Email,
            solicitud.PrimerNombre,
            solicitud.SegundoNombre,
            solicitud.PrimerApellido,
            solicitud.SegundoApellido,
            solicitud.IdRol,
            solicitud.Password);

        var result = await mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }

    [HttpDelete("{id:guid}")]
    [EndpointSummary("Da de baja un usuario (borrado lógico)")]
    [EndpointDescription("""
        Roles admitidos: Administrador.

        NO borra la fila: cambia estado a 'inactivo', de modo que el usuario
        desaparece del listado y ya no puede iniciar sesión, pero conserva sus
        reglas, envíos y auditoría.

        Respuesta 200: baja aplicada (o ya estaba inactivo; es idempotente).
        Respuesta 404: usuario.no.encontrado.
        Respuesta 409: usuario.ultimo.administrador (no se puede dejar el
        sistema sin administradores activos).
        Respuesta 422: usuario.autoeliminar (un administrador no puede
        desactivarse a sí mismo).
        """)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity, "application/problem+json")]
    public async Task<IActionResult> Eliminar(Guid id, CancellationToken cancellationToken)
    {
        var solicitante = ObtenerUsuarioId();
        if (solicitante is null)
        {
            return HandleFailure(Result.Failure(new Error(
                "auth.usuario.no.identificado",
                "No fue posible identificar al usuario del token.",
                ErrorType.Unauthorized)));
        }

        var result = await mediator.Send(
            new EliminarUsuarioCommand(id, solicitante.Value),
            cancellationToken);
        return HandleResult(result);
    }
}
