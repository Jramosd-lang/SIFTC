namespace SIFTC.Application.UseCases.Entidades;

/// <summary>Serie OHLC del IC de una entidad.</summary>
/// <param name="EntidadId">Identificador UUID de la entidad.</param>
/// <param name="Entidad">Nombre de la entidad.</param>
/// <param name="Periodo">Frecuencia usada: Diaria, Mensual o Trimestral.</param>
/// <param name="Serie">Puntos ordenados de la fecha más antigua a la más reciente.</param>
public sealed record OhlcResponseDto(
    Guid EntidadId,
    string Entidad,
    string Periodo,
    IReadOnlyList<OhlcPuntoDto> Serie
);

/// <summary>Un punto de la serie OHLC.</summary>
/// <param name="Fecha">Fecha del periodo en formato ISO 8601.</param>
/// <param name="Apertura">Primer valor del IC en el periodo.</param>
/// <param name="Maximo">Valor máximo del IC en el periodo.</param>
/// <param name="Minimo">Valor mínimo del IC en el periodo.</param>
/// <param name="Cierre">Último valor del IC en el periodo.</param>
/// <param name="Variacion">Variación absoluta frente al cierre anterior.</param>
/// <param name="VariacionPct">Variación porcentual frente al cierre anterior.</param>
public sealed record OhlcPuntoDto(
    DateOnly Fecha,
    decimal Apertura,
    decimal Maximo,
    decimal Minimo,
    decimal Cierre,
    decimal? Variacion,
    decimal? VariacionPct
);
