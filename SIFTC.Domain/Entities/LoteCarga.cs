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
}
