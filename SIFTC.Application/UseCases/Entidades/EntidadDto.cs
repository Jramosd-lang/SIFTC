using SIFTC.Domain.Entities;

namespace SIFTC.Application.UseCases.Entidades;

// Contrato de la guía de Frontend (5.2): "codigo" es el ticker e "ic" el cierre de la última cotización.
public record EntidadDto(
    Guid Id,
    string Codigo,
    string Nombre,
    string Sector,
    decimal? Ic,
    decimal? Variacion,
    decimal? VariacionPct
)
{
    public static EntidadDto FromEntity(EntidadCotizacion entidad) => new(
        entidad.IdEntidad,
        entidad.Ticker,
        entidad.Nombre,
        entidad.Sector,
        entidad.Ic,
        entidad.Variacion,
        entidad.VariacionPct);
}
