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
}
