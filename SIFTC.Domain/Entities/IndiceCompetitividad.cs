using SIFTC.Domain.Common;
namespace SIFTC.Domain.Entities;
public sealed class IndiceCompetitividad
{
    public Guid IdIndice { get; private set; }
    public DateTimeOffset FechaCarga { get; private set; }
    public decimal Ic { get; private set; }
    public Guid IdLote { get; private set; }
    public Guid IdEntidad { get; private set; }
    public Entidad Entidad { get; private set; } = null!;
    private IndiceCompetitividad() { }
    private IndiceCompetitividad(decimal ic, Guid idLote, Guid idEntidad) { IdIndice = Guid.CreateVersion7(); Ic = ic; IdLote = idLote; IdEntidad = idEntidad; FechaCarga = DateTimeOffset.UtcNow; }
    public static Result<IndiceCompetitividad> Create(decimal ic, Guid idLote, Guid idEntidad)
    {
        if (ic is < 0 or > 100) return Result.Failure<IndiceCompetitividad>(new Error("indice.ic.invalid", "El índice de competitividad debe estar entre 0 y 100.", ErrorType.Validation));
        if (idLote == Guid.Empty || idEntidad == Guid.Empty) return Result.Failure<IndiceCompetitividad>(new Error("indice.references.required", "Lote y entidad son obligatorios.", ErrorType.Validation));
        return Result.Success(new IndiceCompetitividad(ic, idLote, idEntidad));
    }
}
