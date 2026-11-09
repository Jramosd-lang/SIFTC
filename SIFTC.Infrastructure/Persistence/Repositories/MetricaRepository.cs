using Microsoft.EntityFrameworkCore;
using SIFTC.Application.Common.Interfaces.Persistence;
using SIFTC.Domain.Entities;

namespace SIFTC.Infrastructure.Persistence.Repositories;

public sealed class MetricaRepository(ApplicationDbContext dbContext)
    : Repository<Metrica>(dbContext), IMetricaRepository
{
    public Task<bool> ExistsByCodigoAsync(string codigo, CancellationToken cancellationToken = default) =>
        DbContext.Metricas.AsNoTracking().AnyAsync(x => x.Codigo == codigo, cancellationToken);

    public Task<bool> DimensionExistsAsync(Guid idDimension, CancellationToken cancellationToken = default) =>
        DbContext.Dimensiones.AsNoTracking().AnyAsync(x => x.IdDimension == idDimension, cancellationToken);
}
