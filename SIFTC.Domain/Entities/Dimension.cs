namespace SIFTC.Domain.Entities;

public sealed class Dimension
{
    public Guid IdDimension { get; private set; }
    public string Nombre { get; private set; } = null!;
    public decimal PesoIc { get; private set; }
    public ICollection<Metrica> Metricas { get; private set; } = new List<Metrica>();

    private Dimension() { }
}
