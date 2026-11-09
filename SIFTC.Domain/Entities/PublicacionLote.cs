namespace SIFTC.Domain.Entities;

public sealed class PublicacionLote
{
    public Guid IdLote { get; private set; }
    public string Estado { get; private set; } = null!;
    public Guid IdUsuarioEjecutor { get; private set; }
    public Guid? IdUsuarioDecision { get; private set; }
    public DateTimeOffset? FechaDecision { get; private set; }
    public string? Comentario { get; private set; }

    private PublicacionLote() { }
}
