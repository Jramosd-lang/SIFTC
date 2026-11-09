using SIFTC.Domain.Common;
namespace SIFTC.Domain.Entities;
public sealed class DatoFinanciero
{
    public Guid IdDato { get; private set; }
    public DateOnly Periodo { get; private set; }
    public decimal? UtilidadNeta { get; private set; }
    public decimal? Patrimonio { get; private set; }
    public decimal? ActivosTotales { get; private set; }
    public decimal? PasivoTotal { get; private set; }
    public decimal? ActivoCorriente { get; private set; }
    public decimal? PasivoCorriente { get; private set; }
    public decimal? Inventarios { get; private set; }
    public decimal? Ebitda { get; private set; }
    public decimal? Ebit { get; private set; }
    public decimal? Ingresos { get; private set; }
    public decimal? GastosFinancieros { get; private set; }
    public DateTimeOffset FechaCarga { get; private set; }
    public Guid IdLote { get; private set; }
    public Guid IdEntidad { get; private set; }
    public Guid IdRegistroOrigen { get; private set; }
    public Entidad Entidad { get; private set; } = null!;
    private DatoFinanciero() { }
    private DatoFinanciero(DateOnly periodo, Guid idLote, Guid idEntidad, Guid origen) { IdDato = Guid.CreateVersion7(); Periodo = periodo; IdLote = idLote; IdEntidad = idEntidad; IdRegistroOrigen = origen; FechaCarga = DateTimeOffset.UtcNow; }
    public static Result<DatoFinanciero> Create(DateOnly periodo, Guid idLote, Guid idEntidad, Guid idRegistroOrigen) => idLote == Guid.Empty || idEntidad == Guid.Empty || idRegistroOrigen == Guid.Empty
        ? Result.Failure<DatoFinanciero>(new Error("datoFinanciero.references.required", "Lote, entidad y registro de origen son obligatorios.", ErrorType.Validation))
        : Result.Success(new DatoFinanciero(periodo, idLote, idEntidad, idRegistroOrigen));
}
