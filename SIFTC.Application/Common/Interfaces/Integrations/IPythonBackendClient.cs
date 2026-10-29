using SIFTC.Application.UseCases.Matriz;
using SIFTC.Application.UseCases.Ranking;
using SIFTC.Domain.Common;

namespace SIFTC.Application.Common.Interfaces.Integrations;

public interface IPythonBackendClient
{
    Task<Result<RankingResponseDto>> GetRankingAsync(
        RankingQuery query,
        CancellationToken cancellationToken = default);

    Task<Result<ValidacionMatrizResponse>> ValidarMatrizAsync(
        Stream archivo,
        string nombreArchivo,
        string contentType,
        CancellationToken cancellationToken = default);
}
