namespace SIFTC.Application.Common.Csv;

/// <summary>
/// Lector mínimo de CSV con separador ';' (convención de SIFTC).
/// Respeta comillas dobles: un campo entrecomillado puede contener el
/// separador, comillas escapadas ("" ) y saltos de línea, como los archivos
/// que exporta Excel.
/// </summary>
public static class CsvLector
{
    public static IReadOnlyList<IReadOnlyList<string>> Leer(string texto)
    {
        var filas = new List<IReadOnlyList<string>>();
        var campos = new List<string>();
        var actual = new System.Text.StringBuilder();
        var entreComillas = false;
        var huboContenido = false;

        for (var i = 0; i < texto.Length; i++)
        {
            var c = texto[i];

            if (entreComillas)
            {
                if (c == '"')
                {
                    if (i + 1 < texto.Length && texto[i + 1] == '"')
                    {
                        actual.Append('"');
                        i++;
                    }
                    else
                    {
                        entreComillas = false;
                    }
                }
                else
                {
                    actual.Append(c);
                }

                continue;
            }

            switch (c)
            {
                case '"' when actual.Length == 0:
                    entreComillas = true;
                    huboContenido = true;
                    break;
                case ';':
                    campos.Add(actual.ToString());
                    actual.Clear();
                    huboContenido = true;
                    break;
                case '\r':
                    if (i + 1 < texto.Length && texto[i + 1] == '\n')
                    {
                        i++;
                    }

                    goto finalizarFila;
                case '\n':
                    goto finalizarFila;
                default:
                    actual.Append(c);
                    huboContenido = true;
                    break;
            }

            continue;

        finalizarFila:
            // Una línea vacía (o solo \r\n final) no es una fila.
            if (huboContenido || campos.Count > 0 || actual.Length > 0)
            {
                campos.Add(actual.ToString());
                filas.Add(campos);
                campos = new List<string>();
                actual.Clear();
                huboContenido = false;
            }
        }

        if (huboContenido || campos.Count > 0 || actual.Length > 0)
        {
            campos.Add(actual.ToString());
            filas.Add(campos);
        }

        return filas;
    }
}
