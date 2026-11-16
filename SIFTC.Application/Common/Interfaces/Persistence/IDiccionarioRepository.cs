using SIFTC.Domain.Entities;

namespace SIFTC.Application.Common.Interfaces.Persistence;

/// <summary>Acceso al diccionario de datos (HU-DD-08).</summary>
public interface IDiccionarioRepository
{
    /// <summary>Vacia la tabla e inserta las filas del CSV (el cargue reemplaza todo).</summary>
    /// <returns>Número de filas insertadas.</returns>
    Task<int> ReemplazarTodoAsync(
        IReadOnlyList<Diccionario> filas,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Busca por nombre, código o sinónimo sin distinguir mayúsculas (ILIKE),
    /// filtrando por las clasificaciones que el rol puede ver.
    /// </summary>
    Task<IReadOnlyList<Diccionario>> BuscarAsync(
        string buscar,
        string? tipo,
        IReadOnlyList<string> clasificacionesVisibles,
        int limite,
        CancellationToken cancellationToken = default);

    /// <summary>Devuelve la fila por código, o null si no existe.</summary>
    Task<Diccionario?> ObtenerPorCodigoAsync(
        string codigo,
        CancellationToken cancellationToken = default);
}
