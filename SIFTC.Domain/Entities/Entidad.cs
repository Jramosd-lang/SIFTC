namespace SIFTC.Domain.Entities;

// limpia.tbl_entidades: la escribe el ETL; el backend la consume desde analitica.v_entidades_cotizacion.
public sealed class Entidad
{
    public Guid IdEntidad { get; private set; }
    public string Ticker { get; private set; } = null!;
    public string Nombre { get; private set; } = null!;
    public Guid IdSector { get; private set; }
    public Sector Sector { get; private set; } = null!;
    public ICollection<DatoFinanciero> DatosFinancieros { get; private set; } = new List<DatoFinanciero>();
    public ICollection<ValorMetrica> ValoresMetrica { get; private set; } = new List<ValorMetrica>();
    public ICollection<IndiceCompetitividad> IndicesCompetitividad { get; private set; } = new List<IndiceCompetitividad>();
    public ICollection<Cotizacion> Cotizaciones { get; private set; } = new List<Cotizacion>();
    public ICollection<EntidadSeguida> Seguidores { get; private set; } = new List<EntidadSeguida>();

    private Entidad() { }
}
