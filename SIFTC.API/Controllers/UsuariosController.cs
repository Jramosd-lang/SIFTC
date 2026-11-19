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
    [EndpointSummary("Registra un nuevo usuario")]
    [EndpointDescription("""
        Roles admitidos: Administrador (403 para otros roles).

        Crea un nuevo usuario en la tabla `seguridad.usuarios` con estado inicial 'activo'.

        Cuerpo JSON:
        - email: correo electrónico único del usuario (se normaliza a minúsculas).
        - password: contraseña en texto plano; el backend genera el hash bcrypt.
        - primerNombre: primer nombre (obligatorio).
        - primerApellido: primer apellido (obligatorio).
        - idRol: UUID de un rol existente en la tabla `seguridad.roles`.

        Catálogo de Roles y sus UUIDs:
        | Rol | UUID (idRol) | Responsabilidad |
        | :--- | :--- | :--- |
        | Administrador | `179acd26-b52c-4e75-8c26-0b6b1b5737e4` | Gestión global del sistema, usuarios y parámetros IC |
        | Analista financiero | `bacadb8f-63d4-4032-8c43-729f597623b4` | Carga de matrices CSV, cálculos y aprobación de lotes |
        | Auditor | `36078d03-86e6-4075-a7b5-c82b3e414760` | Lectura de bitácora, trazabilidad y matriz cruda |
        | Data Steward | `ea07d6fd-a849-4764-8b03-688163837303` | Carga y gestión del diccionario de datos y calidad |
        | Gerente / Directivo | `c43ddacf-b545-4190-b051-c8172e3026f0` | Tableros ejecutivos, series OHLC y ranking general |
        | Usuario móvil | `c6152fdb-6b06-4c89-95b9-42b6222320f1` | Consulta de cotizaciones y watchlist personal |

        Respuesta 200: UUID del usuario creado exitosamente.
        Respuesta 409: Conflicto si el correo electrónico ya se encuentra registrado.
        Respuesta 422: Datos inválidos o UUID de rol no registrado en `seguridad.roles`.
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
        Roles admitidos: Administrador (403 para otros roles).

        Reemplaza la información del usuario especificado por su UUID en la ruta (`{id}`).

        Cuerpo JSON:
        - email: correo electrónico del usuario (no puede coincidir con el de otro usuario existente).
        - primerNombre, primerApellido: obligatorios.
        - segundoNombre, segundoApellido: opcionales (pasar null o cadena vacía si no aplica).
        - idRol: UUID del rol a asignar.
        - password: nueva contraseña en texto plano (opcional). Si es null o vacía, la contraseña previa se conserva sin cambios.

        Catálogo de Roles y sus UUIDs:
        | Rol | UUID (idRol) |
        | :--- | :--- |
        | Administrador | `179acd26-b52c-4e75-8c26-0b6b1b5737e4` |
        | Analista financiero | `bacadb8f-63d4-4032-8c43-729f597623b4` |
        | Auditor | `36078d03-86e6-4075-a7b5-c82b3e414760` |
        | Data Steward | `ea07d6fd-a849-4764-8b03-688163837303` |
        | Gerente / Directivo | `c43ddacf-b545-4190-b051-c8172e3026f0` |
        | Usuario móvil | `c6152fdb-6b06-4c89-95b9-42b6222320f1` |

        Respuesta 200: Objeto `UsuarioDto` con la información actualizada (sin el hash de la contraseña).
        Respuesta 404: `usuario.no.encontrado` si el id no existe.
        Respuesta 409: `usuario.duplicado` si el nuevo correo ya pertenece a otro usuario.
        Respuesta 422: Datos inválidos o `usuario.rol.invalido` si el idRol no existe en `seguridad.roles`.
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
