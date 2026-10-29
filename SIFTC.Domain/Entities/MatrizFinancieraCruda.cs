using SIFTC.Domain.Common;
namespace SIFTC.Domain.Entities;
public sealed class MatrizFinancieraCruda
{
    public Guid IdCruda { get; private set; }
    public Guid IdLote { get; private set; }
    public LoteCarga Lote { get; private set; } = null!;
    public string? CodigoEntidad { get; private set; }
    public string? NombreEntidad { get; private set; }
    public string? Sector { get; private set; }
    public string? Periodo { get; private set; }
    public string? UtilidadNeta { get; private set; }
    public string? Patrimonio { get; private set; }
    public string? ActivosTotales { get; private set; }
    public string? Ebitda { get; private set; }
    public string? Ingresos { get; private set; }
    public string? ActivoCorriente { get; private set; }
    public string? PasivoCorriente { get; private set; }
    public string? Inventarios { get; private set; }
    public string? PasivoTotal { get; private set; }
    public string? Ebit { get; private set; }
    public string? GastosFinancieros { get; private set; }
    public DateTimeOffset FechaCarga { get; private set; }
    private MatrizFinancieraCruda() { }
    private MatrizFinancieraCruda(Guid idLote, string? codigo, string? nombre, string? sector, string? periodo) { IdCruda = Guid.CreateVersion7(); IdLote = idLote; CodigoEntidad = codigo; NombreEntidad = nombre; Sector = sector; Periodo = periodo; FechaCarga = DateTimeOffset.UtcNow; }
    public static Result<MatrizFinancieraCruda> Create(Guid idLote, string? codigoEntidad, string? nombreEntidad, string? sector, string? periodo) => idLote == Guid.Empty
        ? Result.Failure<MatrizFinancieraCruda>(new Error("cruda.lote.required", "El lote es obligatorio.", ErrorType.Validation))
        : Result.Success(new MatrizFinancieraCruda(idLote, codigoEntidad, nombreEntidad, sector, periodo));
}
