using SIFTC.Domain.Common;
namespace SIFTC.Domain.Entities;
public sealed class ValorMetrica
{
    public Guid IdValor { get; private set; }
    public DateOnly Periodo { get; private set; }
    public decimal? Valor { get; private set; }
    public decimal? ValorNormalizado { get; private set; }
    public DateTimeOffset FechaCarga { get; private set; }
    public Guid IdLote { get; private set; }
    public Guid IdEntidad { get; private set; }
    public Entidad Entidad { get; private set; } = null!;
    public Guid IdMetrica { get; private set; }
    public Metrica Metrica { get; private set; } = null!;
    public Guid? IdRegistroOrigen { get; private set; }
    private ValorMetrica() { }
    private ValorMetrica(DateOnly periodo, decimal? valor, Guid idLote, Guid idEntidad, Guid idMetrica, Guid? origen) { IdValor = Guid.CreateVersion7(); Periodo = periodo; Valor = valor; IdLote = idLote; IdEntidad = idEntidad; IdMetrica = idMetrica; IdRegistroOrigen = origen; FechaCarga = DateTimeOffset.UtcNow; }
    public static Result<ValorMetrica> Create(DateOnly periodo, decimal? valor, Guid idLote, Guid idEntidad, Guid idMetrica, Guid? idRegistroOrigen) => idLote == Guid.Empty || idEntidad == Guid.Empty || idMetrica == Guid.Empty
        ? Result.Failure<ValorMetrica>(new Error("valorMetrica.references.required", "Lote, entidad y métrica son obligatorios.", ErrorType.Validation))
        : Result.Success(new ValorMetrica(periodo, valor, idLote, idEntidad, idMetrica, idRegistroOrigen));
}
