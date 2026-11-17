using SIFTC.Domain.Entities;

namespace SIFTC.Application.UseCases.Usuarios;

/// <summary>
/// Usuario expuesto al front. Nunca incluye el hash de la contraseña.
/// </summary>
public sealed record UsuarioDto(
    Guid IdUsuario,
    string Email,
    string PrimerNombre,
    string? SegundoNombre,
    string PrimerApellido,
    string? SegundoApellido,
    Guid IdRol,
    string Rol,
    string Estado)
{
    public static UsuarioDto Desde(Usuario usuario, string rol) =>
        new(
            usuario.IdUsuario,
            usuario.Email,
            usuario.PrimerNombre,
            usuario.SegundoNombre,
            usuario.PrimerApellido,
            usuario.SegundoApellido,
            usuario.IdRol,
            rol,
            usuario.Estado);
}
