using MediatR;
using SIFTC.Domain.Common;

namespace SIFTC.Application.UseCases.Exportar.Queries.ExportarDatos;

/// <summary>
/// Exporta rankings o series a CSV con los mismos filtros de la consulta original.
/// </summary>
/// <param name="Tipo">ranking o serie.</param>
/// <param name="Formato">Solo csv; vacío asume csv.</param>
/// <param name="Sector">Filtro de sector (ranking).</param>
/// <param name="Dimension">Filtro de dimensión (ranking).</param>
/// <param name="Periodo">Frecuencia (ranking y serie).</param>
/// <param name="Orden">Campo de orden del ranking.</param>
/// <param name="Dir">Dirección del orden del ranking.</param>
/// <param name="Limit">Tamaño de página del ranking.</param>
/// <param name="Offset">Desplazamiento del ranking.</param>
/// <param name="IdEntidad">Entidad de la serie OHLC; obligatorio cuando tipo=serie.</param>
/// <param name="Rol">Rol exacto del JWT; determina qué puede exportar (HU-BE-02).</param>
/// <param name="IdUsuario">Identificador del usuario del JWT; se registra en la bitácora.</param>
/// <param name="Authorization">Cabecera Authorization, necesaria para el ranking del microservicio Python.</param>
/// <param name="Ip">IP del cliente; se registra en la bitácora.</param>
public sealed record ExportarQuery(
    string? Tipo,
    string? Formato,
    string? Sector,
    string? Dimension,
    string? Periodo,
    string? Orden,
    string? Dir,
    int? Limit,
    int? Offset,
    Guid? IdEntidad,
    string? Rol,
    Guid? IdUsuario,
    string? Authorization,
    string? Ip
) : IRequest<Result<ExportFileDto>>;
