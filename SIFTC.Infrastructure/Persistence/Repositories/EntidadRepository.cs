using Microsoft.EntityFrameworkCore;
using SIFTC.Application.Common;
using SIFTC.Application.Common.Interfaces.Persistence;
using SIFTC.Domain.Entities;

namespace SIFTC.Infrastructure.Persistence.Repositories;

// Solo lee analitica.v_entidades_cotizacion: la cotización vigente y su variación las resuelve la capa analítica.
public class EntidadRepository(ApplicationDbContext dbContext) : IEntidadRepository
{
    public async Task<Pagina<EntidadCotizacion>> GetPageAsync(
        string? sector,
        int page,
        int size,
        CampoOrdenEntidad sort,
        DireccionOrden dir,
        CancellationToken cancellationToken = default)
    {
        var entidades = dbContext.EntidadesCotizacion.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(sector))
        {
            entidades = entidades.Where(e => e.Sector == sector);
        }

        var total = await entidades.CountAsync(cancellationToken);

        // Las entidades sin cotización van al final en ambas direcciones (PostgreSQL pone NULL primero en DESC).
        var ordenadas = sort switch
        {
            CampoOrdenEntidad.Nombre => entidades.OrdenarPor(e => e.Nombre, dir),
            CampoOrdenEntidad.Sector => entidades.OrdenarPor(e => e.Sector, dir),
            CampoOrdenEntidad.Ic => entidades.OrderBy(e => e.Ic == null).EntoncesPor(e => e.Ic, dir),
            CampoOrdenEntidad.VariacionPct => entidades.OrderBy(e => e.VariacionPct == null).EntoncesPor(e => e.VariacionPct, dir),
            _ => entidades.OrdenarPor(e => e.Ticker, dir)
        };

        // El ticker es único: desempata para que la paginación sea estable.
        var items = await ordenadas
            .ThenBy(e => e.Ticker)
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync(cancellationToken);

        return new Pagina<EntidadCotizacion>(items, total);
    }

    public async Task<EntidadCotizacion?> GetByIdAsync(Guid idEntidad, CancellationToken cancellationToken = default)
    {
        return await dbContext.EntidadesCotizacion
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.IdEntidad == idEntidad, cancellationToken);
    }
}
