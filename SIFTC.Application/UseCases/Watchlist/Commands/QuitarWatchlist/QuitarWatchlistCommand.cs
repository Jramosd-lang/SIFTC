using MediatR;
using SIFTC.Domain.Common;

namespace SIFTC.Application.UseCases.Watchlist.Commands.QuitarWatchlist;

public sealed record QuitarWatchlistCommand(
    Guid IdUsuario,
    Guid IdEntidad
) : IRequest<Result>;
