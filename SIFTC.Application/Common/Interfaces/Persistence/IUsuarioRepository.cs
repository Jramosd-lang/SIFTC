using SIFTC.Domain.Entities;

namespace SIFTC.Application.Common.Interfaces.Persistence;

public interface IUsuarioRepository : IRepository<Usuario>
{
    Task<Usuario?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Solo usuarios con estado 'activo', ordenados por nombre. Es lo único
    /// que se expone al front: los inactivos quedan fuera del listado.
    /// </summary>
    Task<IReadOnlyList<Usuario>> ListarActivosAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Comprueba si otro usuario (distinto de <paramref name="idUsuario"/>)
    /// ya usa el correo. Evita el 409 falso al editar sin cambiar el correo.
    /// </summary>
    Task<bool> ExistsByEmailExceptoAsync(
        string email,
        Guid idUsuario,
        CancellationToken cancellationToken = default);

    /// <summary>Nombre del rol, o null si el UUID no existe.</summary>
    Task<string?> ObtenerNombreRolAsync(Guid idRol, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cuántos usuarios ACTIVOS tienen el rol indicado, sin contar al que se
    /// está desactivando. Se usa para no dejar el sistema sin administradores.
    /// </summary>
    Task<int> ContarActivosPorRolAsync(
        Guid idRol,
        Guid excluirIdUsuario,
        CancellationToken cancellationToken = default);
}
