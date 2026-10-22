namespace SIFTC.Domain.Entities;

// Watchlist del usuario (seguridad.entidades_seguidas).
public sealed class EntidadSeguida
{
    public Guid IdUsuario { get; private set; }
    public Usuario Usuario { get; private set; } = null!;
    public Guid IdEntidad { get; private set; }
    public Entidad Entidad { get; private set; } = null!;

    private EntidadSeguida() { }
}
