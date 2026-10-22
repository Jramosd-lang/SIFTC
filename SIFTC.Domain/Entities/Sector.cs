namespace SIFTC.Domain.Entities;

public sealed class Sector
{
    public Guid IdSector { get; private set; }
    public string Nombre { get; private set; } = null!;
    public ICollection<Entidad> Entidades { get; private set; } = new List<Entidad>();

    private Sector() { }
}
