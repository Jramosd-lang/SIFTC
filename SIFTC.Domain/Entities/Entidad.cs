using SIFTC.Domain.Common;
namespace SIFTC.Domain.Entities;
public sealed class Entidad
{
    public Guid IdEntidad { get; private set; }
    public string Ticker { get; private set; } = null!;
    public string Nombre { get; private set; } = null!;
    public Guid IdSector { get; private set; }
    public Sector Sector { get; private set; } = null!;
    public ICollection<DatoFinanciero> DatosFinancieros { get; private set; } = new List<DatoFinanciero>();
    public ICollection<ValorMetrica> ValoresMetrica { get; private set; } = new List<ValorMetrica>();
    public ICollection<IndiceCompetitividad> IndicesCompetitividad { get; private set; } = new List<IndiceCompetitividad>();
    public ICollection<Cotizacion> Cotizaciones { get; private set; } = new List<Cotizacion>();
    public ICollection<EntidadSeguida> Seguidores { get; private set; } = new List<EntidadSeguida>();
    private Entidad() { }
    private Entidad(string ticker, string nombre, Guid idSector) { IdEntidad = Guid.CreateVersion7(); Ticker = ticker; Nombre = nombre; IdSector = idSector; }
    public static Result<Entidad> Create(string ticker, string nombre, Guid idSector)
    {
        if (string.IsNullOrWhiteSpace(ticker) || string.IsNullOrWhiteSpace(nombre)) return Result.Failure<Entidad>(new Error("entidad.required", "El ticker y nombre son obligatorios.", ErrorType.Validation));
        if (idSector == Guid.Empty) return Result.Failure<Entidad>(new Error("entidad.sector.required", "El sector es obligatorio.", ErrorType.Validation));
        return Result.Success(new Entidad(ticker.Trim().ToUpperInvariant(), nombre.Trim(), idSector));
    }
}
