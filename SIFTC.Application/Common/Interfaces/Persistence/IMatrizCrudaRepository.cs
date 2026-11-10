using SIFTC.Application.UseCases.MatrizCruda;

namespace SIFTC.Application.Common.Interfaces.Persistence;

public interface IMatrizCrudaRepository
{
    Task<IReadOnlyList<MatrizCrudaDto>> ListarAsync(
        int limit,
        CancellationToken cancellationToken = default);
}
