using SIFTC.Domain.Entities;

namespace SIFTC.Application.Common.Interfaces.Persistence;

// Solo lectura: las entidades y cotizaciones las escribe el ETL, no el backend.
public interface IEntidadRepository
{
    Task<Pagina<EntidadCotizacion>> GetPageAsync(
        string? sector,
        int page,
        int size,
        CampoOrdenEntidad sort,
        DireccionOrden dir,
        CancellationToken cancellationToken = default);

    Task<EntidadCotizacion?> GetByIdAsync(Guid idEntidad, CancellationToken cancellationToken = default);
}
