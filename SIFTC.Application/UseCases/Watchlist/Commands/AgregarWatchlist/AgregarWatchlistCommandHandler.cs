using MediatR;
using SIFTC.Application.Common.Interfaces.Persistence;
using SIFTC.Domain.Common;
using SIFTC.Domain.Entities;

namespace SIFTC.Application.UseCases.Watchlist.Commands.AgregarWatchlist;

public sealed class AgregarWatchlistCommandHandler(
    IWatchlistRepository watchlistRepository,
    IEntidadRepository entidadRepository)
    : IRequestHandler<AgregarWatchlistCommand, Result>
{
    public async Task<Result> Handle(
        AgregarWatchlistCommand request,
        CancellationToken cancellationToken)
    {
        if (request.IdUsuario == Guid.Empty)
        {
            return Result.Failure(new Error(
                "auth.unauthorized",
                "No se pudo identificar el usuario autenticado.",
                ErrorType.Unauthorized));
        }

        var entidad = await entidadRepository.GetByIdAsync(request.IdEntidad, cancellationToken);
        if (entidad is null)
        {
            return Result.Failure(new Error(
                "watchlist.entidad.not_found",
                "La entidad solicitada no existe.",
                ErrorType.NotFound));
        }

        if (await watchlistRepository.ExistsAsync(
                request.IdUsuario,
                request.IdEntidad,
                cancellationToken))
        {
            return Result.Success();
        }

        var entidadSeguidaResult = EntidadSeguida.Create(
            request.IdUsuario,
            request.IdEntidad);

        if (entidadSeguidaResult.IsFailure)
        {
            return Result.Failure(entidadSeguidaResult.Error);
        }

        await watchlistRepository.AddAsync(entidadSeguidaResult.Value, cancellationToken);
        return Result.Success();
    }
}
