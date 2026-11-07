using SIFTC.Domain.Entities;

namespace SIFTC.Application.UseCases.Entidades;

/// <summary>Entidad con su cotización actual.</summary>
/// <param name="Id">Identificador UUID de la entidad.</param>
/// <param name="Codigo">Ticker o código único de la entidad (por ejemplo ENT001).</param>
/// <param name="Nombre">Nombre comercial o razón social.</param>
/// <param name="Sector">Sector económico al que pertenece.</param>
/// <param name="Ic">Último Índice de Competitividad (0 a 100).</param>
/// <param name="Variacion">Variación absoluta frente al cierre anterior.</param>
/// <param name="VariacionPct">Variación porcentual frente al cierre anterior.</param>
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
