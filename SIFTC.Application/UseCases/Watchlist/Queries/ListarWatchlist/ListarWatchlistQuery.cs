using MediatR;
using SIFTC.Application.UseCases.Entidades;
using SIFTC.Domain.Common;

namespace SIFTC.Application.UseCases.Watchlist.Queries.ListarWatchlist;

public sealed record ListarWatchlistQuery(Guid IdUsuario)
    : IRequest<Result<IReadOnlyList<EntidadDto>>>;
