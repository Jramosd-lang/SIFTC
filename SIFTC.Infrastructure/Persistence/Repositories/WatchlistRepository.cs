using Microsoft.EntityFrameworkCore;
using SIFTC.Application.Common.Interfaces.Persistence;
using SIFTC.Application.UseCases.Entidades;
using SIFTC.Domain.Entities;

namespace SIFTC.Infrastructure.Persistence.Repositories;

public sealed class WatchlistRepository(ApplicationDbContext dbContext) : IWatchlistRepository
{
    public async Task<IReadOnlyList<EntidadDto>> GetByUsuarioAsync(
        Guid idUsuario,
        CancellationToken cancellationToken = default)
    {
        return await (
            from seguida in dbContext.EntidadesSeguidas.AsNoTracking()
            join entidad in dbContext.EntidadesCotizacion.AsNoTracking()
                on seguida.IdEntidad equals entidad.IdEntidad
            where seguida.IdUsuario == idUsuario
            orderby entidad.Ticker
            select new EntidadDto(
                entidad.IdEntidad,
                entidad.Ticker,
                entidad.Nombre,
                entidad.Sector,
                entidad.Ic,
                entidad.Variacion,
                entidad.VariacionPct))
            .ToListAsync(cancellationToken);
    }

    public Task<bool> ExistsAsync(
        Guid idUsuario,
        Guid idEntidad,
        CancellationToken cancellationToken = default) =>
        dbContext.EntidadesSeguidas
            .AsNoTracking()
            .AnyAsync(
                x => x.IdUsuario == idUsuario && x.IdEntidad == idEntidad,
                cancellationToken);

    public async Task AddAsync(
        EntidadSeguida entidadSeguida,
        CancellationToken cancellationToken = default)
    {
        await dbContext.EntidadesSeguidas.AddAsync(entidadSeguida, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveAsync(
        Guid idUsuario,
        Guid idEntidad,
        CancellationToken cancellationToken = default)
    {
        var entidadSeguida = await dbContext.EntidadesSeguidas
            .FirstOrDefaultAsync(
                x => x.IdUsuario == idUsuario && x.IdEntidad == idEntidad,
                cancellationToken);

        if (entidadSeguida is null)
        {
            return;
        }

        dbContext.EntidadesSeguidas.Remove(entidadSeguida);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
