using MediatR;
using SIFTC.Application.Common.Interfaces.Persistence;
using SIFTC.Domain.Common;
using SIFTC.Domain.Entities;

namespace SIFTC.Application.UseCases.Metricas.Commands.CrearMetrica;

public sealed class CrearMetricaCommandHandler(IMetricaRepository metricaRepository)
    : IRequestHandler<CrearMetricaCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CrearMetricaCommand request, CancellationToken cancellationToken)
    {
        var codigo = request.Codigo?.Trim().ToUpperInvariant() ?? string.Empty;

        if (await metricaRepository.ExistsByCodigoAsync(codigo, cancellationToken))
        {
            return Result.Failure<Guid>(new Error("metrica.codigo.duplicado", "Ya existe una métrica con ese código.", ErrorType.Conflict));
        }

        if (!await metricaRepository.DimensionExistsAsync(request.IdDimension, cancellationToken))
        {
            return Result.Failure<Guid>(new Error("metrica.dimension.notFound", "La dimensión especificada no existe.", ErrorType.NotFound));
        }

        var metricaResult = Metrica.Create(codigo, request.Nombre, request.Formula, request.Frecuencia, request.IdDimension);
        if (metricaResult.IsFailure)
        {
            return Result.Failure<Guid>(metricaResult.Error);
        }

        await metricaRepository.AddAsync(metricaResult.Value, cancellationToken);
        return Result.Success(metricaResult.Value.IdMetrica);
    }
}
