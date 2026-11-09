using SIFTC.Domain.Common;
using SIFTC.Domain.Enums;
namespace SIFTC.Domain.Entities;
public sealed class Metrica
{
    public Guid IdMetrica { get; private set; }
    public string Codigo { get; private set; } = null!;
    public string Nombre { get; private set; } = null!;
    public string Formula { get; private set; } = null!;
    public Frecuencia Frecuencia { get; private set; }
    public Guid IdDimension { get; private set; }
    public Dimension Dimension { get; private set; } = null!;
    private Metrica() { }
    private Metrica(string codigo, string nombre, string formula, Frecuencia frecuencia, Guid idDimension) { IdMetrica = Guid.CreateVersion7(); Codigo = codigo; Nombre = nombre; Formula = formula; Frecuencia = frecuencia; IdDimension = idDimension; }
    public static Result<Metrica> Create(string codigo, string nombre, string formula, Frecuencia frecuencia, Guid idDimension)
    {
        if (string.IsNullOrWhiteSpace(codigo) || string.IsNullOrWhiteSpace(nombre) || string.IsNullOrWhiteSpace(formula)) return Result.Failure<Metrica>(new Error("metrica.required", "Código, nombre y fórmula son obligatorios.", ErrorType.Validation));
        if (!Enum.IsDefined(frecuencia)) return Result.Failure<Metrica>(new Error("metrica.frecuencia.invalid", "La frecuencia no es válida.", ErrorType.Validation));
        if (idDimension == Guid.Empty) return Result.Failure<Metrica>(new Error("metrica.dimension.required", "La dimensión es obligatoria.", ErrorType.Validation));
        return Result.Success(new Metrica(codigo.Trim().ToUpperInvariant(), nombre.Trim(), formula.Trim(), frecuencia, idDimension));
    }
}
