using SIFTC.Domain.Common;
namespace SIFTC.Domain.Entities;
public sealed class Auditoria
{
    public Guid IdBitacora { get; private set; }
    public string Accion { get; private set; } = null!;
    public string? Detalle { get; private set; }
    public DateTimeOffset FechaEvento { get; private set; }
    public Guid? IdUsuario { get; private set; }
    private Auditoria() { }
    private Auditoria(string accion, string? detalle, Guid? idUsuario) { IdBitacora = Guid.CreateVersion7(); Accion = accion; Detalle = detalle; IdUsuario = idUsuario; FechaEvento = DateTimeOffset.UtcNow; }
    public static Result<Auditoria> Create(string accion, string? detalle, Guid? idUsuario) => string.IsNullOrWhiteSpace(accion)
        ? Result.Failure<Auditoria>(new Error("auditoria.accion.required", "La acción es obligatoria.", ErrorType.Validation))
        : Result.Success(new Auditoria(accion.Trim(), detalle?.Trim(), idUsuario));
}
