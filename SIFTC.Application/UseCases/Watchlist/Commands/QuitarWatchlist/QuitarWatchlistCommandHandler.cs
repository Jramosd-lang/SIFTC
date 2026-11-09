using MediatR;
using SIFTC.Application.Common.Interfaces.Persistence;
using SIFTC.Domain.Common;

namespace SIFTC.Application.UseCases.Watchlist.Commands.QuitarWatchlist;

public sealed class QuitarWatchlistCommandHandler(IWatchlistRepository watchlistRepository)
    : IRequestHandler<QuitarWatchlistCommand, Result>
{
    public async Task<Result> Handle(
        QuitarWatchlistCommand request,
        CancellationToken cancellationToken)
    {
        if (request.IdUsuario == Guid.Empty)
        {
            return Result.Failure(new Error(
                "auth.unauthorized",
                "No se pudo identificar el usuario autenticado.",
                ErrorType.Unauthorized));
        }

        await watchlistRepository.RemoveAsync(
            request.IdUsuario,
            request.IdEntidad,
            cancellationToken);

        return Result.Success();
    }
}
