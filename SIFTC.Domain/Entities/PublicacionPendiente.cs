namespace SIFTC.Domain.Entities;

// analitica.v_publicaciones_pendientes: lotes pendientes con sus datos de carga, sin exponer la capa cruda.
public sealed class PublicacionPendiente
{
    public Guid IdLote { get; private set; }
    public string NombreArchivo { get; private set; } = null!;
    public DateTimeOffset FechaCarga { get; private set; }
    public int RegistrosTotales { get; private set; }
    public int RegistrosValidos { get; private set; }
    public string EstadoCarga { get; private set; } = null!;
    public string Estado { get; private set; } = null!;
    public Guid IdUsuarioEjecutor { get; private set; }
    public Guid? IdUsuarioDecision { get; private set; }
    public DateTimeOffset? FechaDecision { get; private set; }
    public string? Comentario { get; private set; }

    private PublicacionPendiente() { }
}
