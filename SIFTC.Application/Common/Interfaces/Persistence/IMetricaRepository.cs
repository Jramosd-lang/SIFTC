using SIFTC.Domain.Entities;

namespace SIFTC.Application.Common.Interfaces.Persistence;

public interface IMetricaRepository : IRepository<Metrica>
{
    Task<bool> ExistsByCodigoAsync(string codigo, CancellationToken cancellationToken = default);
    Task<bool> DimensionExistsAsync(Guid idDimension, CancellationToken cancellationToken = default);
}
