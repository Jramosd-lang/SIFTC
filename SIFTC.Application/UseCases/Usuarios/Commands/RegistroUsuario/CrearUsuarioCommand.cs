using MediatR;
using SIFTC.Domain.Common;

namespace SIFTC.Application.UseCases.Usuarios.Commands;

/// <summary>Datos para registrar un usuario.</summary>
/// <param name="Email">Correo único del usuario.</param>
/// <param name="Password">Contraseña en texto plano; el backend la guarda con bcrypt.</param>
/// <param name="PrimerNombre">Primer nombre.</param>
/// <param name="PrimerApellido">Primer apellido.</param>
/// <param name="IdRol">UUID del rol existente en seguridad.roles.</param>
public record CrearUsuarioCommand(
    string Email,
    string Password,
    string PrimerNombre,
    string PrimerApellido,
    Guid IdRol
) : IRequest<Result<Guid>>;
