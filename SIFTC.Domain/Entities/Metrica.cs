namespace SIFTC.Domain.Entities;

public sealed class Metrica
{
    public Guid IdMetrica { get; private set; }
    public string Codigo { get; private set; } = null!;
    public string Nombre { get; private set; } = null!;
    public string Formula { get; private set; } = null!;
    public string Frecuencia { get; private set; } = null!;
    public Guid IdDimension { get; private set; }
    public Dimension Dimension { get; private set; } = null!;

    private Metrica() { }
}
