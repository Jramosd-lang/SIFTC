namespace SIFTC.Domain.Entities;

public sealed class Auditoria
{
    public Guid IdBitacora { get; private set; }
    public string Accion { get; private set; } = null!;
    public string? Detalle { get; private set; }
    public DateTimeOffset FechaEvento { get; private set; }
    public Guid? IdUsuario { get; private set; }

    private Auditoria() { }
}
