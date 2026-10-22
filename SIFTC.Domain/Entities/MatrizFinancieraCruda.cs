namespace SIFTC.Domain.Entities;

// cruda.ext_matriz_financiera_entidades: copia fiel del CSV, todo en texto.
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
}
