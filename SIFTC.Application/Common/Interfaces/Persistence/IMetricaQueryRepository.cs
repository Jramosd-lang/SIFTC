using SIFTC.Application.Common;
using SIFTC.Application.UseCases.Metricas;
using SIFTC.Domain.Enums;

namespace SIFTC.Application.Common.Interfaces.Persistence;

// Lado de lectura de CQRS: solo consulta la capa analítica, nunca modifica estado.
public interface IMetricaQueryRepository
{
    Task<Pagina<MetricaDto>> GetPageAsync(
        string? sector,
        Frecuencia? frecuencia,
        DateOnly? fecha,
        string? dimension,
        int page,
        int size,
        CampoOrdenMetrica sort,
        DireccionOrden dir,
        CancellationToken cancellationToken = default);
}
