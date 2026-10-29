namespace SIFTC.Application.UseCases.Metricas;

// Contrato de lectura: una fila por métrica calculada por la capa analítica.
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
