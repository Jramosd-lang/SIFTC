namespace SIFTC.Domain.Entities;

public sealed class RolSistema
{
    public Guid IdRol { get; private set; }
    public string Nombre { get; private set; } = null!;
    public string? Descripcion { get; private set; }
    public string PermisosClave { get; private set; } = null!;
    public ICollection<Usuario> Usuarios { get; private set; } = new List<Usuario>();

    private RolSistema() { }
}
