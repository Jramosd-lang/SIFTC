using MediatR;
using SIFTC.Application.Common.Interfaces.Persistence;
using SIFTC.Application.UseCases.Entidades;
using SIFTC.Domain.Common;

namespace SIFTC.Application.UseCases.Watchlist.Queries.ListarWatchlist;

public sealed class ListarWatchlistQueryHandler(IWatchlistRepository watchlistRepository)
    : IRequestHandler<ListarWatchlistQuery, Result<IReadOnlyList<EntidadDto>>>
{
    public async Task<Result<IReadOnlyList<EntidadDto>>> Handle(
        ListarWatchlistQuery request,
        CancellationToken cancellationToken)
    {
        var entidades = await watchlistRepository.GetByUsuarioAsync(
            request.IdUsuario,
            cancellationToken);

        return Result.Success(entidades);
    }
}
