using SIFTC.Domain.Common;
namespace SIFTC.Domain.Entities;
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
    private Cotizacion(DateOnly fecha, decimal apertura, decimal maximo, decimal minimo, decimal cierre, Guid idEntidad) { IdCotizacion = Guid.CreateVersion7(); Fecha = fecha; Apertura = apertura; Maximo = maximo; Minimo = minimo; Cierre = cierre; IdEntidad = idEntidad; FechaCarga = DateTimeOffset.UtcNow; }
    public static Result<Cotizacion> Create(DateOnly fecha, decimal apertura, decimal maximo, decimal minimo, decimal cierre, Guid idEntidad)
    {
        if (minimo > Math.Min(apertura, cierre) || maximo < Math.Max(apertura, cierre)) return Result.Failure<Cotizacion>(new Error("cotizacion.rango.invalid", "El mínimo y máximo no contienen apertura y cierre.", ErrorType.Validation));
        if (idEntidad == Guid.Empty) return Result.Failure<Cotizacion>(new Error("cotizacion.entidad.required", "La entidad es obligatoria.", ErrorType.Validation));
        return Result.Success(new Cotizacion(fecha, apertura, maximo, minimo, cierre, idEntidad));
    }
}
