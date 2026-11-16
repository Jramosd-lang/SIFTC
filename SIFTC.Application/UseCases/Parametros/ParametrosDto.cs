namespace SIFTC.Application.UseCases.Parametros;

/// <summary>Parámetros de solo lectura del Índice de Competitividad.</summary>
public sealed record ParametrosDto(
    IReadOnlyList<DimensionParametroDto> Dimensiones,
    IReadOnlyList<MetricaParametroDto> Metricas);

/// <summary>Peso de una dimensión dentro del IC (0 a 1).</summary>
public sealed record DimensionParametroDto(string Nombre, decimal PesoIc);

/// <summary>Métrica del catálogo con su fórmula y frecuencia.</summary>
public sealed record MetricaParametroDto(
    string Codigo,
    string Nombre,
    string Dimension,
    string Formula,
    string Frecuencia);
