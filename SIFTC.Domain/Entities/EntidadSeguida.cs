using SIFTC.Domain.Common;
namespace SIFTC.Domain.Entities;
public sealed class EntidadSeguida
{
    public Guid IdUsuario { get; private set; }
    public Usuario Usuario { get; private set; } = null!;
    public Guid IdEntidad { get; private set; }
    public Entidad Entidad { get; private set; } = null!;
    private EntidadSeguida() { }
    private EntidadSeguida(Guid idUsuario, Guid idEntidad) { IdUsuario = idUsuario; IdEntidad = idEntidad; }
    public static Result<EntidadSeguida> Create(Guid idUsuario, Guid idEntidad) => idUsuario == Guid.Empty || idEntidad == Guid.Empty
        ? Result.Failure<EntidadSeguida>(new Error("seguida.references.required", "Usuario y entidad son obligatorios.", ErrorType.Validation))
        : Result.Success(new EntidadSeguida(idUsuario, idEntidad));
}
