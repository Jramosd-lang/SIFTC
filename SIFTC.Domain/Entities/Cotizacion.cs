namespace SIFTC.Domain.Entities;

// Vela diaria del IC calculada por analitica.calcular_cotizaciones.
public sealed class Cotizacion
{
    public Guid IdCotizacion { get; private set; }
    public DateOnly Fecha { get; private set; }
    public decimal Apertura { get; private set; }
    public decimal Maximo { get; private set; }
    public decimal Minimo { get; private set; }
    public decimal Cierre { get; private set; }
    public decimal? VariacionPct { get; private set; }
    public int VolumenActualizaciones { get; private set; }
    public DateTimeOffset FechaCarga { get; private set; }
    public Guid IdEntidad { get; private set; }
    public Entidad Entidad { get; private set; } = null!;

    private Cotizacion() { }
}
