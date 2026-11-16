namespace SIFTC.Application.UseCases.Diccionario;

/// <summary>Fila de la búsqueda: exactamente los 4 campos del PDF de la HU-DD-08.</summary>
public sealed record DiccionarioResultadoDto(
    string Codigo,
    string Tipo,
    string Nombre,
    string Definicion);

/// <summary>Ficha completa de un término (sinonimos llega como arreglo).</summary>
public sealed record DiccionarioFichaDto(
    string Codigo,
    string Tipo,
    string Nombre,
    string Definicion,
    string? Formula,
    string? Unidad,
    IReadOnlyList<string> Sinonimos,
    string Clasificacion);

/// <summary>Respuesta de POST /api/v1/diccionario/cargar: {"registros": n}.</summary>
public sealed record CargaDiccionarioResponseDto(int Registros);
