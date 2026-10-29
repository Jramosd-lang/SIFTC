namespace SIFTC.Domain.Entities;

// analitica.v_entidades_cotizacion: cada entidad con su cotización vigente, calculada por la capa analítica.
public sealed class EntidadCotizacion
{
    public Guid IdEntidad { get; private set; }
    public string Ticker { get; private set; } = null!;
    public string Nombre { get; private set; } = null!;
    public string Sector { get; private set; } = null!;
    public decimal? Ic { get; private set; }
    public decimal? Variacion { get; private set; }
    public decimal? VariacionPct { get; private set; }

    private EntidadCotizacion() { }
}
