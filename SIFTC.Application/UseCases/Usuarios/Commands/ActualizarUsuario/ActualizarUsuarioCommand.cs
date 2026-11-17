using MediatR;
using SIFTC.Application.UseCases.Usuarios;
using SIFTC.Domain.Common;

namespace SIFTC.Application.UseCases.Usuarios.Commands.ActualizarUsuario;

/// <summary>Datos editables de un usuario existente.</summary>
/// <param name="IdUsuario">UUID del usuario a editar (viene de la ruta).</param>
/// <param name="Password">Opcional: si viene, se reemplaza el hash.</param>
public sealed record ActualizarUsuarioCommand(
    Guid IdUsuario,
    string Email,
    string PrimerNombre,
    string? SegundoNombre,
    string PrimerApellido,
    string? SegundoApellido,
    Guid IdRol,
    string? Password
) : IRequest<Result<UsuarioDto>>;

/// <summary>Cuerpo JSON de PUT /api/v1/usuarios/{id} (sin el id, que va en la ruta).</summary>
public sealed record ActualizarUsuarioSolicitud(
    string Email,
    string PrimerNombre,
    string? SegundoNombre,
    string PrimerApellido,
    string? SegundoApellido,
    Guid IdRol,
    string? Password
);
