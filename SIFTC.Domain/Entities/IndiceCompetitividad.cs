namespace SIFTC.Domain.Entities;

public sealed class IndiceCompetitividad
{
    public Guid IdIndice { get; private set; }
    public DateTimeOffset FechaCarga { get; private set; }
    public decimal Ic { get; private set; }
    public Guid IdLote { get; private set; }
    public Guid IdEntidad { get; private set; }
    public Entidad Entidad { get; private set; } = null!;

    private IndiceCompetitividad() { }
}
