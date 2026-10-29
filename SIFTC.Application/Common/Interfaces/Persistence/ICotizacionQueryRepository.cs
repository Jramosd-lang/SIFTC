using SIFTC.Domain.Entities;

namespace SIFTC.Application.Common.Interfaces.Persistence;

public interface ICotizacionQueryRepository
{
    Task<IReadOnlyList<Cotizacion>> GetByEntidadAsync(
        Guid idEntidad,
        CancellationToken cancellationToken = default);
}
