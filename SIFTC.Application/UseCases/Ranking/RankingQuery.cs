namespace SIFTC.Application.UseCases.Ranking;

public sealed record RankingQuery(
    string? Sector,
    string? Dimension,
    string Periodo,
    string Orden,
    string Dir,
    int Limit,
    int Offset
);
