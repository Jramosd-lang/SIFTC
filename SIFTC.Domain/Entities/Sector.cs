using SIFTC.Domain.Common;
namespace SIFTC.Domain.Entities;
public sealed class Sector
{
    public Guid IdSector { get; private set; }
    public string Nombre { get; private set; } = null!;
    public ICollection<Entidad> Entidades { get; private set; } = new List<Entidad>();
    private Sector() { }
    private Sector(string nombre) { IdSector = Guid.CreateVersion7(); Nombre = nombre; }
    public static Result<Sector> Create(string nombre) => string.IsNullOrWhiteSpace(nombre)
        ? Result.Failure<Sector>(new Error("sector.nombre.required", "El nombre del sector es obligatorio.", ErrorType.Validation))
        : Result.Success(new Sector(nombre.Trim()));
}
