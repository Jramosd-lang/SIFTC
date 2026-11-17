using Microsoft.EntityFrameworkCore;
using SIFTC.Application.Common.Interfaces.Persistence;
using SIFTC.Domain.Entities;

namespace SIFTC.Infrastructure.Persistence.Repositories;

public class UsuarioRepository(ApplicationDbContext dbContext)
    : Repository<Usuario>(dbContext), IUsuarioRepository
{
    public async Task<Usuario?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        // AsNoTracking() hace que la consulta sea más rápida ya que solo vamos a leer, no a modificar
        return await DbContext.Set<Usuario>()
            .Include(u => u.Rol)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
    }

    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await DbContext.Set<Usuario>()
            .AnyAsync(u => u.Email == email, cancellationToken);
    }

    public async Task<IReadOnlyList<Usuario>> ListarActivosAsync(CancellationToken cancellationToken = default)
    {
        return await DbContext.Set<Usuario>()
            .Include(u => u.Rol)
            .AsNoTracking()
            .Where(u => u.Estado == "activo")
            .OrderBy(u => u.PrimerApellido)
            .ThenBy(u => u.PrimerNombre)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsByEmailExceptoAsync(
        string email,
        Guid idUsuario,
        CancellationToken cancellationToken = default)
    {
        return await DbContext.Set<Usuario>()
            .AnyAsync(u => u.Email == email && u.IdUsuario != idUsuario, cancellationToken);
    }

    public async Task<string?> ObtenerNombreRolAsync(Guid idRol, CancellationToken cancellationToken = default)
    {
        return await DbContext.Set<RolSistema>()
            .AsNoTracking()
            .Where(r => r.IdRol == idRol)
            .Select(r => r.Nombre)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<int> ContarActivosPorRolAsync(
        Guid idRol,
        Guid excluirIdUsuario,
        CancellationToken cancellationToken = default)
    {
        return await DbContext.Set<Usuario>()
            .CountAsync(
                u => u.IdRol == idRol
                     && u.IdUsuario != excluirIdUsuario
                     && u.Estado == "activo",
                cancellationToken);
    }
}
