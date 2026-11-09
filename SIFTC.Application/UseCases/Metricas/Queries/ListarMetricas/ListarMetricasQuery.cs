using MediatR;
using SIFTC.Application.Common;
using SIFTC.Domain.Common;

namespace SIFTC.Application.UseCases.Metricas.Queries.ListarMetricas;

public sealed record ListarMetricasQuery(
    string? Sector,
    string? Periodo,
    string? Fecha,
    string? Dimension,
    int? Page,
    int? Size,
    string? Sort,
    string? Dir
) : IRequest<Result<Pagina<MetricaDto>>>;
