using Microsoft.EntityFrameworkCore;
using SIFTC.Application.Common.Interfaces.Persistence;
using SIFTC.Application.UseCases.Parametros;

namespace SIFTC.Infrastructure.Persistence.Repositories;

public sealed class ParametrosRepository(ApplicationDbContext dbContext) : IParametrosRepository
{
    public async Task<ParametrosDto> ObtenerAsync(CancellationToken cancellationToken = default)
    {
        var dimensiones = await dbContext.Dimensiones
            .AsNoTracking()
            .OrderBy(d => d.Nombre)
            .Select(d => new DimensionParametroDto(d.Nombre, d.PesoIc))
            .ToListAsync(cancellationToken);

        // Se materializa primero y el enum se convierte a texto en memoria, sin depender de la traducción de EF.
        var metricas = await dbContext.Metricas
            .AsNoTracking()
            .OrderBy(m => m.Codigo)
            .Select(m => new
            {
                m.Codigo,
                m.Nombre,
                Dimension = m.Dimension.Nombre,
                m.Formula,
                m.Frecuencia
            })
            .ToListAsync(cancellationToken);

        return new ParametrosDto(
            dimensiones,
            metricas
                .Select(m => new MetricaParametroDto(m.Codigo, m.Nombre, m.Dimension, m.Formula, m.Frecuencia.ToString()))
                .ToList());
    }
}
