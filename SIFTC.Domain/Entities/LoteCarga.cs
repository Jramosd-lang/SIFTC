using SIFTC.Domain.Common;
namespace SIFTC.Domain.Entities;
public sealed class LoteCarga
{
    public Guid IdLote { get; private set; }
    public string NombreArchivo { get; private set; } = null!;
    public DateTimeOffset FechaCarga { get; private set; }
    public int RegistrosTotales { get; private set; }
    public int RegistrosValidos { get; private set; }
    public decimal? IndiceCalidad { get; private set; }
    public Guid IdUsuario { get; private set; }
    public Usuario Usuario { get; private set; } = null!;
    public ICollection<MatrizFinancieraCruda> RegistrosCrudos { get; private set; } = new List<MatrizFinancieraCruda>();
    private LoteCarga() { }
    private LoteCarga(string nombreArchivo, int total, int validos, Guid idUsuario) { IdLote = Guid.CreateVersion7(); NombreArchivo = nombreArchivo; FechaCarga = DateTimeOffset.UtcNow; RegistrosTotales = total; RegistrosValidos = validos; IdUsuario = idUsuario; }
    public static Result<LoteCarga> Create(string nombreArchivo, int registrosTotales, int registrosValidos, Guid idUsuario)
    {
        if (string.IsNullOrWhiteSpace(nombreArchivo)) return Result.Failure<LoteCarga>(new Error("lote.archivo.required", "El nombre del archivo es obligatorio.", ErrorType.Validation));
        if (registrosTotales < 0 || registrosValidos < 0 || registrosValidos > registrosTotales) return Result.Failure<LoteCarga>(new Error("lote.registros.invalid", "Los conteos de registros no son válidos.", ErrorType.Validation));
        if (idUsuario == Guid.Empty) return Result.Failure<LoteCarga>(new Error("lote.usuario.required", "El usuario es obligatorio.", ErrorType.Validation));
        return Result.Success(new LoteCarga(nombreArchivo.Trim(), registrosTotales, registrosValidos, idUsuario));
    }
}
