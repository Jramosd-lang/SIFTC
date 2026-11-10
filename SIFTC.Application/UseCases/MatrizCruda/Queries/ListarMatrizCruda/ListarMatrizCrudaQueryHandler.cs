using MediatR;
using SIFTC.Application.Common.Interfaces.Persistence;
using SIFTC.Domain.Common;

namespace SIFTC.Application.UseCases.MatrizCruda.Queries.ListarMatrizCruda;

public sealed class ListarMatrizCrudaQueryHandler(IMatrizCrudaRepository matrizCrudaRepository)
    : IRequestHandler<ListarMatrizCrudaQuery, Result<IReadOnlyList<MatrizCrudaDto>>>
{
    private const int LimitePorDefecto = 50;
    private const int LimiteMaximo = 200;

    public async Task<Result<IReadOnlyList<MatrizCrudaDto>>> Handle(
        ListarMatrizCrudaQuery request,
        CancellationToken cancellationToken)
    {
        var limit = request.Limit ?? LimitePorDefecto;

        if (limit is < 1 or > LimiteMaximo)
        {
            return Result.Failure<IReadOnlyList<MatrizCrudaDto>>(new Error(
                "matrizcruda.limit.invalid",
                $"El parámetro limit debe estar entre 1 y {LimiteMaximo}.",
                ErrorType.Validation));
        }

        var filas = await matrizCrudaRepository.ListarAsync(limit, cancellationToken);

        return Result.Success(filas);
    }
}
