namespace SIFTC.Application.UseCases.ConfiguracionIc;

public sealed record PesoIcDto(string Dimension, decimal Peso);

public sealed record UmbralIcDto(
    string CodigoMetrica,
    decimal? LimiteInferior,
    decimal? LimiteSuperior,
    string Severidad);

public sealed record ConfiguracionIcDto(
    Guid IdVersion,
    int NumeroVersion,
    DateTimeOffset FechaVigencia,
    string Motivo,
    Guid IdUsuario,
    string FrecuenciaIc,
    IReadOnlyList<PesoIcDto> Pesos,
    IReadOnlyList<UmbralIcDto> Umbrales);
