namespace SIFTC.Application.UseCases.Metricas;

/// <summary>Valor de una métrica calculada para una entidad y un periodo.</summary>
/// <param name="EntidadId">Identificador UUID de la entidad.</param>
/// <param name="Codigo">Ticker de la entidad.</param>
/// <param name="Nombre">Nombre de la entidad.</param>
/// <param name="Sector">Sector económico.</param>
/// <param name="Fecha">Periodo del dato en formato ISO 8601.</param>
/// <param name="CodigoMetrica">Código del catálogo: RNT-01, LIQ-01, END-01, CRE-01, etc.</param>
/// <param name="NombreMetrica">Nombre legible de la métrica.</param>
/// <param name="Dimension">Dimensión del catálogo a la que pertenece.</param>
/// <param name="Formula">Fórmula usada para el cálculo.</param>
/// <param name="Frecuencia">Diaria, Mensual o Trimestral.</param>
/// <param name="Valor">Valor calculado como proporción o veces, según la métrica.</param>
/// <param name="ValorNormalizado">Valor llevado a la escala 0 a 1 para el cálculo del IC.</param>
public sealed record MetricaDto(
    Guid EntidadId,
    string Codigo,
    string Nombre,
    string Sector,
    DateOnly Fecha,
    string CodigoMetrica,
    string NombreMetrica,
    string Dimension,
    string Formula,
    string Frecuencia,
    decimal? Valor,
    decimal? ValorNormalizado
);
