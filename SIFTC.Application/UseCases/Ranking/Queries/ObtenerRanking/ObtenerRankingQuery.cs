using MediatR;
using SIFTC.Domain.Common;

namespace SIFTC.Application.UseCases.Ranking.Queries.ObtenerRanking;

public sealed record ObtenerRankingQuery(
    string? Sector,
    string? Dimension,
    string? Periodo,
    string? Orden,
    string? Dir,
    int? Limit,
    int? Offset
) : IRequest<Result<RankingResponseDto>>;
