using SIFTC.Domain.Common;

namespace SIFTC.Domain.Entities;

/// <summary>
/// Fila del diccionario de datos (HU-DD-08). Equivale a una fila de
/// diccionario.csv; el cargue reemplaza la tabla completa.
/// </summary>
public sealed class Diccionario
{
    /// <summary>Código de la ficha: RNT-01 (métrica), utilidad_neta (dato) o GLO-IC (término).</summary>
    public string Codigo { get; private set; } = null!;

    /// <summary>dato, metrica o termino.</summary>
    public string Tipo { get; private set; } = null!;

    public string Nombre { get; private set; } = null!;

    public string Definicion { get; private set; } = null!;

    /// <summary>Sinónimos separados por coma; vacío si la fila no trae ninguno.</summary>
    public string? Sinonimos { get; private set; }

    /// <summary>Solo métricas.</summary>
    public string? Formula { get; private set; }

    /// <summary>Solo métricas.</summary>
    public string? Unidad { get; private set; }

    /// <summary>Público, Interno, Confidencial o Restringido.</summary>
    public string Clasificacion { get; private set; } = null!;

    private Diccionario() { }

    private Diccionario(
        string codigo,
        string tipo,
        string nombre,
        string definicion,
        string? sinonimos,
        string? formula,
        string? unidad,
        string clasificacion)
    {
        Codigo = codigo;
        Tipo = tipo;
        Nombre = nombre;
        Definicion = definicion;
        Sinonimos = sinonimos;
        Formula = formula;
        Unidad = unidad;
        Clasificacion = clasificacion;
    }

    public static Result<Diccionario> Crear(
        string codigo,
        string tipo,
        string nombre,
        string definicion,
        string? sinonimos,
        string? formula,
        string? unidad,
        string clasificacion)
    {
        codigo = codigo.Trim();
        tipo = tipo.Trim().ToLowerInvariant();
        nombre = nombre.Trim();
        definicion = definicion.Trim();

        // Tolerante a mayúsculas/minúsculas (el Excel podría traerlas en minúscula),
        // pero se guarda siempre la forma canónica que espera el CHECK de la BD.
        clasificacion = clasificacion.Trim().ToLowerInvariant() switch
        {
            "público" or "publico" => "Público",
            "interno" => "Interno",
            "confidencial" => "Confidencial",
            "restringido" => "Restringido",
            _ => clasificacion.Trim()
        };

        if (codigo.Length == 0 || nombre.Length == 0 || definicion.Length == 0)
        {
            return Result.Failure<Diccionario>(new Error(
                "diccionario.fila.required",
                "codigo, nombre y definicion son obligatorios.",
                ErrorType.Validation));
        }

        if (tipo is not ("dato" or "metrica" or "termino"))
        {
            return Result.Failure<Diccionario>(new Error(
                "diccionario.tipo.invalid",
                "El tipo admite: dato, metrica o termino.",
                ErrorType.Validation));
        }

        if (clasificacion is not ("Público" or "Interno" or "Confidencial" or "Restringido"))
        {
            return Result.Failure<Diccionario>(new Error(
                "diccionario.clasificacion.invalid",
                "La clasificación admite: Público, Interno, Confidencial o Restringido.",
                ErrorType.Validation));
        }

        return Result.Success(new Diccionario(
            codigo,
            tipo,
            nombre,
            definicion,
            string.IsNullOrWhiteSpace(sinonimos) ? null : sinonimos.Trim(),
            string.IsNullOrWhiteSpace(formula) ? null : formula.Trim(),
            string.IsNullOrWhiteSpace(unidad) ? null : unidad.Trim(),
            clasificacion));
    }
}
