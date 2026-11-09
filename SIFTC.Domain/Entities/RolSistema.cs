using SIFTC.Domain.Common;
namespace SIFTC.Domain.Entities;
public sealed class RolSistema
{
    public Guid IdRol { get; private set; }
    public string Nombre { get; private set; } = null!;
    public string? Descripcion { get; private set; }
    public string PermisosClave { get; private set; } = null!;
    public ICollection<Usuario> Usuarios { get; private set; } = new List<Usuario>();
    private RolSistema() { }
    private RolSistema(string nombre, string? descripcion, string permisos) { IdRol = Guid.CreateVersion7(); Nombre = nombre; Descripcion = descripcion; PermisosClave = permisos; }
    public static Result<RolSistema> Create(string nombre, string? descripcion, string permisos) => string.IsNullOrWhiteSpace(nombre) || string.IsNullOrWhiteSpace(permisos)
        ? Result.Failure<RolSistema>(new Error("rol.required", "El nombre y los permisos del rol son obligatorios.", ErrorType.Validation))
        : Result.Success(new RolSistema(nombre.Trim(), descripcion?.Trim(), permisos.Trim()));
}
