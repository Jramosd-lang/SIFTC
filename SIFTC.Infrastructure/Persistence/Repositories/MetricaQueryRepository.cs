using Microsoft.EntityFrameworkCore;
using SIFTC.Application.Common;
using SIFTC.Application.Common.Interfaces.Persistence;
using SIFTC.Application.UseCases.Metricas;
using SIFTC.Domain.Enums;

namespace SIFTC.Infrastructure.Persistence.Repositories;

public sealed class MetricaQueryRepository(ApplicationDbContext dbContext) : IMetricaQueryRepository
{
    public async Task<Pagina<MetricaDto>> GetPageAsync(
        string? sector,
        Frecuencia? frecuencia,
        DateOnly? fecha,
        string? dimension,
        int page,
        int size,
        CampoOrdenMetrica sort,
        DireccionOrden dir,
        CancellationToken cancellationToken = default)
    {
        var metricas =
            from v in dbContext.ValoresMetrica.AsNoTracking()
            join m in dbContext.Metricas.AsNoTracking() on v.IdMetrica equals m.IdMetrica
            join d in dbContext.Dimensiones.AsNoTracking() on m.IdDimension equals d.IdDimension
            join e in dbContext.EntidadesCotizacion.AsNoTracking() on v.IdEntidad equals e.IdEntidad
            select new { v, m, d, e };

        if (!string.IsNullOrWhiteSpace(sector))
        {
            metricas = metricas.Where(x => x.e.Sector == sector);
        }

        if (frecuencia.HasValue)
        {
            metricas = metricas.Where(x => x.m.Frecuencia == frecuencia.Value);
        }

        if (fecha.HasValue)
        {
            metricas = metricas.Where(x => x.v.Periodo == fecha.Value);
        }

        if (!string.IsNullOrWhiteSpace(dimension))
        {
            metricas = metricas.Where(x => x.d.Nombre == dimension);
        }

        var total = await metricas.CountAsync(cancellationToken);

        // Los valores nulos van al final en ambos sentidos; ticker, métrica y fecha desempatan.
        var ordenadas = sort switch
        {
            CampoOrdenMetrica.Codigo => metricas.OrdenarPor(x => x.e.Ticker, dir),
            CampoOrdenMetrica.Nombre => metricas.OrdenarPor(x => x.e.Nombre, dir),
            CampoOrdenMetrica.Sector => metricas.OrdenarPor(x => x.e.Sector, dir),
            CampoOrdenMetrica.Dimension => metricas.OrdenarPor(x => x.d.Nombre, dir),
            CampoOrdenMetrica.CodigoMetrica => metricas.OrdenarPor(x => x.m.Codigo, dir),
            CampoOrdenMetrica.Valor => metricas.OrderBy(x => x.v.Valor == null).EntoncesPor(x => x.v.Valor, dir),
            CampoOrdenMetrica.ValorNormalizado => metricas.OrderBy(x => x.v.ValorNormalizado == null).EntoncesPor(x => x.v.ValorNormalizado, dir),
            _ => metricas.OrdenarPor(x => x.v.Periodo, dir)
        };

        var items = await ordenadas
            .ThenBy(x => x.e.Ticker)
            .ThenBy(x => x.m.Codigo)
            .ThenBy(x => x.v.Periodo)
            .Skip((page - 1) * size)
            .Take(size)
            .Select(x => new MetricaDto(
                x.v.IdEntidad,
                x.e.Ticker,
                x.e.Nombre,
                x.e.Sector,
                x.v.Periodo,
                x.m.Codigo,
                x.m.Nombre,
                x.d.Nombre,
                x.m.Formula,
                x.m.Frecuencia.ToString(),
                x.v.Valor,
                x.v.ValorNormalizado))
            .ToListAsync(cancellationToken);

        return new Pagina<MetricaDto>(items, total);
    }
}
