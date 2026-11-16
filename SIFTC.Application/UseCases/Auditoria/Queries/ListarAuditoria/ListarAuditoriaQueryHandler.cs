using MediatR;
using SIFTC.Application.Common.Interfaces.Persistence;
using SIFTC.Domain.Common;

namespace SIFTC.Application.UseCases.Auditoria.Queries.ListarAuditoria;

public sealed class ListarAuditoriaQueryHandler(IAuditoriaRepository auditoriaRepository)
    : IRequestHandler<ListarAuditoriaQuery, Result<IReadOnlyList<AuditoriaDto>>>
{
    private const int LimitePorDefecto = 50;
    private const int LimiteMaximo = 200;

    public async Task<Result<IReadOnlyList<AuditoriaDto>>> Handle(
        ListarAuditoriaQuery request,
        CancellationToken cancellationToken)
    {
        var limit = request.Limit ?? LimitePorDefecto;

        if (limit is < 1 or > LimiteMaximo)
        {
            return Result.Failure<IReadOnlyList<AuditoriaDto>>(new Error(
                "auditoria.limit.invalid",
                $"El parámetro limit debe estar entre 1 y {LimiteMaximo}.",
                ErrorType.Validation));
        }

        var eventos = await auditoriaRepository.ListarAsync(limit, cancellationToken);

        return Result.Success(eventos);
    }
}
