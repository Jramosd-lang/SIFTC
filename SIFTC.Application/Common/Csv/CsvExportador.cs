using System.Globalization;
using System.Text;

namespace SIFTC.Application.Common.Csv;

/// <summary>
/// Generación de CSV con la convención de SIFTC: separador ';' y decimal con coma,
/// igual que el CSV de entrada que acepta <c>POST /api/v1/cargas</c>.
/// Así Excel en español abre la descarga en columnas correctas.
/// </summary>
public static class CsvExportador
{
    /// <summary>Escapa un campo según RFC 4180 (comillas dobles) cuando contiene separador, comilla o salto de línea.</summary>
    public static string Escapar(string? campo)
    {
        if (string.IsNullOrEmpty(campo))
        {
            return string.Empty;
        }

        if (campo.Contains(';') || campo.Contains('"') || campo.Contains('\r') || campo.Contains('\n'))
        {
            return $"\"{campo.Replace("\"", "\"\"")}\"";
        }

        return campo;
    }

    /// <summary>Une los campos de una fila aplicando el escape a cada uno.</summary>
    public static string Fila(params string?[] campos) => string.Join(';', campos.Select(Escapar));

    /// <summary>Formatea un decimal con punto invariante y lo convierte a coma decimal.</summary>
    public static string Numero(decimal valor) =>
        valor.ToString(CultureInfo.InvariantCulture).Replace('.', ',');

    public static string Numero(decimal? valor) => valor is null ? string.Empty : Numero(valor.Value);

    public static string Numero(int? valor) =>
        valor?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;

    /// <summary>Convierte el CSV a bytes UTF-8 con BOM (para que Excel detecte acentos).</summary>
    public static byte[] AUtf8Bom(string contenido) => Encoding.UTF8.GetBytes($"\uFEFF{contenido}");
}
