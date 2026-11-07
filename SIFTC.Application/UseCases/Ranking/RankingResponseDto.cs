namespace SIFTC.Application.UseCases.Ranking;

/// <summary>Respuesta del ranking general o por dimensión.</summary>
/// <param name="Periodo">Frecuencia solicitada: diario, mensual o trimestral.</param>
/// <param name="Total">Cantidad total de entidades que cumplen los filtros.</param>
/// <param name="Items">Filas ordenadas según `orden` y `dir`.</param>
public sealed record RankingResponseDto(
    string Periodo,
    int Total,
    IReadOnlyList<RankingItemDto> Items
);

/// <summary>Una entidad dentro del ranking.</summary>
/// <param name="Posicion">Posición actual (1 es la mejor).</param>
/// <param name="CambioPosicion">Positivo si subió, negativo si bajó, 0 si mantuvo.</param>
/// <param name="EntidadId">Identificador UUID de la entidad.</param>
/// <param name="Entidad">Nombre de la entidad.</param>
/// <param name="Sector">Sector económico.</param>
/// <param name="Ic">IC general o puntaje de la dimensión (0 a 100).</param>
/// <param name="VariacionPct">Variación porcentual frente al periodo anterior.</param>
public sealed record RankingItemDto(
    int Posicion,
    int CambioPosicion,
    Guid EntidadId,
    string Entidad,
    string Sector,
    decimal Ic,
    decimal VariacionPct
);
