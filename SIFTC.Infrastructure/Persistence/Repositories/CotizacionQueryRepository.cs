using Microsoft.EntityFrameworkCore;
using SIFTC.Application.Common.Interfaces.Persistence;
using SIFTC.Domain.Entities;

namespace SIFTC.Infrastructure.Persistence.Repositories;

public sealed class CotizacionQueryRepository(ApplicationDbContext dbContext) : ICotizacionQueryRepository
{
    public async Task<IReadOnlyList<Cotizacion>> GetByEntidadAsync(
        Guid idEntidad,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Cotizaciones
            .AsNoTracking()
            .Where(c => c.IdEntidad == idEntidad)
            .OrderBy(c => c.Fecha)
            .ToListAsync(cancellationToken);
    }
}
