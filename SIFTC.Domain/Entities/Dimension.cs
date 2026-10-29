using SIFTC.Domain.Common;
namespace SIFTC.Domain.Entities;
public sealed class Dimension
{
    public Guid IdDimension { get; private set; }
    public string Nombre { get; private set; } = null!;
    public decimal PesoIc { get; private set; }
    public ICollection<Metrica> Metricas { get; private set; } = new List<Metrica>();
    private Dimension() { }
    private Dimension(string nombre, decimal pesoIc) { IdDimension = Guid.CreateVersion7(); Nombre = nombre; PesoIc = pesoIc; }
    public static Result<Dimension> Create(string nombre, decimal pesoIc)
    {
        if (string.IsNullOrWhiteSpace(nombre)) return Result.Failure<Dimension>(new Error("dimension.nombre.required", "El nombre es obligatorio.", ErrorType.Validation));
        if (pesoIc is < 0 or > 1) return Result.Failure<Dimension>(new Error("dimension.peso.invalid", "El peso debe estar entre 0 y 1.", ErrorType.Validation));
        return Result.Success(new Dimension(nombre.Trim(), pesoIc));
    }
}
