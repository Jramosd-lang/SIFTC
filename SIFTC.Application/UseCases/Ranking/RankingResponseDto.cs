namespace SIFTC.Application.UseCases.Ranking;

public sealed record RankingResponseDto(
    string Periodo,
    int Total,
    IReadOnlyList<RankingItemDto> Items
);

public sealed record RankingItemDto(
    int Posicion,
    int CambioPosicion,
    Guid EntidadId,
    string Entidad,
    string Sector,
    decimal Ic,
    decimal VariacionPct
);
