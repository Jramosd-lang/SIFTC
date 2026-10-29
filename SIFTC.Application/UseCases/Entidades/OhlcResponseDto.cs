namespace SIFTC.Application.UseCases.Entidades;

public sealed record OhlcResponseDto(
    Guid EntidadId,
    string Entidad,
    string Periodo,
    IReadOnlyList<OhlcPuntoDto> Serie
);

public sealed record OhlcPuntoDto(
    DateOnly Fecha,
    decimal Apertura,
    decimal Maximo,
    decimal Minimo,
    decimal Cierre,
    decimal? Variacion,
    decimal? VariacionPct
);
