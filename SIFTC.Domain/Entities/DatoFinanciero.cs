namespace SIFTC.Domain.Entities;

public sealed class DatoFinanciero
{
    public Guid IdDato { get; private set; }
    public DateOnly Periodo { get; private set; }
    public decimal? UtilidadNeta { get; private set; }
    public decimal? Patrimonio { get; private set; }
    public decimal? ActivosTotales { get; private set; }
    public decimal? PasivoTotal { get; private set; }
    public decimal? ActivoCorriente { get; private set; }
    public decimal? PasivoCorriente { get; private set; }
    public decimal? Inventarios { get; private set; }
    public decimal? Ebitda { get; private set; }
    public decimal? Ebit { get; private set; }
    public decimal? Ingresos { get; private set; }
    public decimal? GastosFinancieros { get; private set; }
    public DateTimeOffset FechaCarga { get; private set; }
    public Guid IdLote { get; private set; }
    public Guid IdEntidad { get; private set; }
    public Entidad Entidad { get; private set; } = null!;
    public Guid IdRegistroOrigen { get; private set; }

    private DatoFinanciero() { }
}
