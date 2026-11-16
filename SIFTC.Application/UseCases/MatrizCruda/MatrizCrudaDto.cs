namespace SIFTC.Application.UseCases.MatrizCruda;

/// <summary>Fila de la matriz cruda tal como llegó en la carga. Confidencial.</summary>
public sealed record MatrizCrudaDto(
    Guid IdCruda,
    Guid IdLote,
    string? CodigoEntidad,
    string? NombreEntidad,
    string? Sector,
    string? Periodo,
    string? UtilidadNeta,
    string? Patrimonio,
    string? ActivosTotales,
    string? Ebitda,
    string? Ingresos,
    string? ActivoCorriente,
    string? PasivoCorriente,
    string? Inventarios,
    string? PasivoTotal,
    string? Ebit,
    string? GastosFinancieros,
    DateTimeOffset FechaCarga);
