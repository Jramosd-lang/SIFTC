using MediatR;
using SIFTC.Domain.Common;

namespace SIFTC.Application.UseCases.Watchlist.Commands.AgregarWatchlist;

public sealed record AgregarWatchlistCommand(
    Guid IdUsuario,
    Guid IdEntidad
) : IRequest<Result>;
