using System.Text;
using MediatR;
using SIFTC.Application.Common.Csv;
using SIFTC.Application.Common.Interfaces.Persistence;
using SIFTC.Domain.Common;
using DiccionarioEntidad = SIFTC.Domain.Entities.Diccionario;

namespace SIFTC.Application.UseCases.Diccionario.Commands.CargarDiccionario;

/// <summary>
/// CA1: valida las 8 columnas ANTES de tocar la base; si faltan, devuelve 422
/// y la tabla queda como estaba. Solo si todo el archivo es válido se vacía la
/// tabla y se insertan las filas (el cargue reemplaza el contenido completo).
/// </summary>
public sealed class CargarDiccionarioCommandHandler(IDiccionarioRepository repositorio)
    : IRequestHandler<CargarDiccionarioCommand, Result<CargaDiccionarioResponseDto>>
{
    // Seccion 2.2 del documento de la HU.
    private static readonly string[] ColumnasEsperadas =
    [
        "codigo",
        "tipo",
        "nombre",
        "definicion",
        "sinonimos",
        "formula",
        "unidad",
        "clasificacion"
    ];

    public async Task<Result<CargaDiccionarioResponseDto>> Handle(
        CargarDiccionarioCommand request,
        CancellationToken cancellationToken)
    {
        string texto;
        try
        {
            using var lector = new StreamReader(
                request.Archivo,
                Encoding.UTF8,
                detectEncodingFromByteOrderMarks: true,
                bufferSize: 4096,
                leaveOpen: true);
            texto = await lector.ReadToEndAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return Result.Failure<CargaDiccionarioResponseDto>(new Error(
                "diccionario.csv.ilegible",
                "No fue posible leer el archivo como UTF-8.",
                ErrorType.Validation));
        }

        var filas = CsvLector.Leer(texto);
        if (filas.Count == 0)
        {
            return Result.Failure<CargaDiccionarioResponseDto>(new Error(
                "diccionario.csv.vacio",
                "El archivo está vacío: se esperaba la fila de encabezado.",
                ErrorType.Validation));
        }

        // Encabezado: deben estar las 8 columnas (el orden no importa).
        var encabezado = filas[0]
            .Select(c => c.Trim().ToLowerInvariant().Trim('\uFEFF'))
            .ToArray();

        var faltantes = ColumnasEsperadas
            .Where(c => !encabezado.Contains(c, StringComparer.Ordinal))
            .ToArray();

        if (faltantes.Length > 0)
        {
            return Result.Failure<CargaDiccionarioResponseDto>(new Error(
                "diccionario.columnas.faltan",
                $"Faltan columnas: [{string.Join(", ", faltantes)}].",
                ErrorType.Validation));
        }

        var indice = ColumnasEsperadas
            .Select((columna, i) => (columna, i))
            .ToDictionary(x => x.columna, x => x.i);

        var datos = new List<DiccionarioEntidad>();
        var codigos = new HashSet<string>(StringComparer.Ordinal);

        for (var n = 1; n < filas.Count; n++)
        {
            var fila = filas[n];

            // Línea en blanco al final del archivo.
            if (fila.All(c => string.IsNullOrWhiteSpace(c)))
            {
                continue;
            }

            string Obtener(string columna) =>
                fila.Count > indice[columna] ? fila[indice[columna]].Trim() : string.Empty;

            var creada = DiccionarioEntidad.Crear(
                Obtener("codigo"),
                Obtener("tipo"),
                Obtener("nombre"),
                Obtener("definicion"),
                Obtener("sinonimos"),
                Obtener("formula"),
                Obtener("unidad"),
                Obtener("clasificacion"));

            if (creada.IsFailure)
            {
                return Result.Failure<CargaDiccionarioResponseDto>(new Error(
                    "diccionario.fila.invalida",
                    $"Fila {n + 1}: {creada.Error.Message}",
                    ErrorType.Validation));
            }

            // La PK es codigo: un duplicado rompería la inserción entera.
            if (!codigos.Add(creada.Value.Codigo))
            {
                return Result.Failure<CargaDiccionarioResponseDto>(new Error(
                    "diccionario.codigo.duplicado",
                    $"Fila {n + 1}: el código '{creada.Value.Codigo}' está repetido.",
                    ErrorType.Validation));
            }

            datos.Add(creada.Value);
        }

        try
        {
            var registros = await repositorio.ReemplazarTodoAsync(datos, cancellationToken);
            return Result.Success(new CargaDiccionarioResponseDto(registros));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return Result.Failure<CargaDiccionarioResponseDto>(new Error(
                "diccionario.carga.error",
                "No fue posible reemplazar el contenido del diccionario.",
                ErrorType.Dependency));
        }
    }
}
