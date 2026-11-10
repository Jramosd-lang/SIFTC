using Microsoft.EntityFrameworkCore;
using SIFTC.Application.Common.Interfaces.Persistence;
using SIFTC.Application.UseCases.MatrizCruda;

namespace SIFTC.Infrastructure.Persistence.Repositories;

public sealed class MatrizCrudaRepository(ApplicationDbContext dbContext) : IMatrizCrudaRepository
{
    public async Task<IReadOnlyList<MatrizCrudaDto>> ListarAsync(
        int limit,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.MatrizFinancieraCruda
            .AsNoTracking()
            .OrderByDescending(m => m.FechaCarga)
            .Take(limit)
            .Select(m => new MatrizCrudaDto(
                m.IdCruda,
                m.IdLote,
                m.CodigoEntidad,
                m.NombreEntidad,
                m.Sector,
                m.Periodo,
                m.UtilidadNeta,
                m.Patrimonio,
                m.ActivosTotales,
                m.Ebitda,
                m.Ingresos,
                m.ActivoCorriente,
                m.PasivoCorriente,
                m.Inventarios,
                m.PasivoTotal,
                m.Ebit,
                m.GastosFinancieros,
                m.FechaCarga))
            .ToListAsync(cancellationToken);
    }
}
