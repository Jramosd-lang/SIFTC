using System.Globalization;
using System.Text;
using MediatR;
using SIFTC.Application.Common.Csv;
using SIFTC.Application.Common.Interfaces.Persistence;
using SIFTC.Application.UseCases.Entidades;
using SIFTC.Application.UseCases.Entidades.Queries.ObtenerOhlc;
using SIFTC.Application.UseCases.Ranking;
using SIFTC.Application.UseCases.Ranking.Queries.ObtenerRanking;
using SIFTC.Domain.Common;
using SIFTC.Domain.Entities;
using AuditoriaEntidad = SIFTC.Domain.Entities.Auditoria;

namespace SIFTC.Application.UseCases.Exportar.Queries.ExportarDatos;

/// <summary>
/// Exporta rankings o series a CSV.
/// CA1: los datos salen de los mismos casos de uso de la consulta (ObtenerRankingQuery /
/// ObtenerOhlcQuery), por lo que el archivo refleja exactamente los filtros aplicados.
/// CA2: los roles permitidos siguen la Matriz de permisos v1.1 y el filtro de rol de HU-BE-02.
/// CA3: toda exportación exitosa se registra en la bitácora; si el registro falla, la
/// exportación se bloquea con 503 y no se entrega el archivo.
/// </summary>
public sealed class ExportarQueryHandler(
    IMediator mediator,
    IAuditoriaRepository auditoriaRepository)
    : IRequestHandler<ExportarQuery, Result<ExportFileDto>>
{
    private const string TipoRanking = "ranking";
    private const string TipoSerie = "serie";
    private const string FormatoCsv = "csv";
    private const string RolGerente = "Gerente / Directivo";
    private const string AccionExportacion = "exportacion";
    private const string RutaExportar = "/api/v1/exportar";
    private const int LimiteDetalle = 250;

    // Matriz de permisos SIFTC v1.1:
    // - Ranking (Interna) + Posicion (Confidencial, Gerente = SA): los cinco roles de ranking.
    // - Series OHLC (Pública): los seis roles.
    // Los nombres coinciden con la semilla de seguridad.roles y con API/Common/RolesSistema.cs.
    private static readonly string[] RolesRanking =
    [
        "Administrador",
        "Data Steward",
        "Analista financiero",
        "Gerente / Directivo",
        "Auditor"
    ];

    private static readonly string[] RolesSerie =
    [
        "Administrador",
        "Data Steward",
        "Analista financiero",
        "Gerente / Directivo",
        "Auditor",
        "Usuario móvil"
    ];

    public async Task<Result<ExportFileDto>> Handle(ExportarQuery request, CancellationToken cancellationToken)
    {
        if (!TryNormalizarTipo(request.Tipo, out var tipo))
        {
            return Result.Failure<ExportFileDto>(new Error(
                "exportar.tipo.invalid",
                "El parámetro tipo admite: ranking o serie.",
                ErrorType.Failure));
        }

        if (!TryNormalizarFormato(request.Formato, out var formato))
        {
            return Result.Failure<ExportFileDto>(new Error(
                "exportar.formato.invalid",
                "El parámetro formato admite: csv.",
                ErrorType.Failure));
        }

        var rolesPermitidos = tipo == TipoRanking ? RolesRanking : RolesSerie;
        if (!rolesPermitidos.Contains(request.Rol ?? string.Empty, StringComparer.Ordinal))
        {
            return Result.Failure<ExportFileDto>(new Error(
                "exportar.rol.forbidden",
                "Su rol no tiene permiso para exportar este recurso.",
                ErrorType.Forbidden));
        }

        return tipo == TipoRanking
            ? await ExportarRanking(request, cancellationToken)
            : await ExportarSerie(request, cancellationToken);
    }

    private async Task<Result<ExportFileDto>> ExportarRanking(ExportarQuery request, CancellationToken cancellationToken)
    {
        // CA1: mismos filtros y misma validación que GET /api/v1/ranking.
        var resultado = await mediator.Send(
            new ObtenerRankingQuery(
                request.Sector,
                request.Dimension,
                request.Periodo,
                request.Orden,
                request.Dir,
                request.Limit,
                request.Offset,
                request.Authorization),
            cancellationToken);

        if (resultado.IsFailure)
        {
            return Result.Failure<ExportFileDto>(resultado.Error);
        }

        // HU-BE-02 / D1: el Gerente exporta el ranking sin posición ni cambio de posición.
        var items = request.Rol == RolGerente
            ? resultado.Value.Items
                .Select(item => item with { Posicion = null, CambioPosicion = null })
                .ToList()
            : resultado.Value.Items;

        var csv = ConstruirCsvRanking(items);
        var archivo = new ExportFileDto(
            CsvExportador.AUtf8Bom(csv),
            "text/csv; charset=utf-8",
            $"ranking_{MarcaTemporal()}.csv",
            items.Count);

        return await RegistrarExportacion(
            request,
            TipoRanking,
            ConstruirFiltrosRanking(request),
            archivo,
            cancellationToken);
    }

    private async Task<Result<ExportFileDto>> ExportarSerie(ExportarQuery request, CancellationToken cancellationToken)
    {
        if (request.IdEntidad is null)
        {
            return Result.Failure<ExportFileDto>(new Error(
                "exportar.entidad.required",
                "El parámetro idEntidad es obligatorio cuando tipo=serie.",
                ErrorType.Failure));
        }

        // CA1: mismos filtros y misma validación que GET /api/v1/entidades/{id}/ohlc.
        var resultado = await mediator.Send(
            new ObtenerOhlcQuery(request.IdEntidad.Value, request.Periodo),
            cancellationToken);

        if (resultado.IsFailure)
        {
            return Result.Failure<ExportFileDto>(resultado.Error);
        }

        var serie = resultado.Value;
        var csv = ConstruirCsvSerie(serie.Serie);
        var archivo = new ExportFileDto(
            CsvExportador.AUtf8Bom(csv),
            "text/csv; charset=utf-8",
            $"serie_{SanitizarNombre(serie.Entidad)}_{SanitizarNombre(serie.Periodo)}_{MarcaTemporal()}.csv",
            serie.Serie.Count);

        return await RegistrarExportacion(
            request,
            TipoSerie,
            $"idEntidad={serie.EntidadId};periodo={serie.Periodo}",
            archivo,
            cancellationToken);
    }

    /// <summary>CA3: registra la exportación en la bitácora antes de entregar el archivo.</summary>
    private async Task<Result<ExportFileDto>> RegistrarExportacion(
        ExportarQuery request,
        string tipo,
        string filtros,
        ExportFileDto archivo,
        CancellationToken cancellationToken)
    {
        var auditoria = AuditoriaEntidad.Create(
            AccionExportacion,
            ConstruirDetalle(request.Ip, tipo, archivo.TotalFilas, filtros),
            request.IdUsuario);

        if (auditoria.IsFailure)
        {
            return Result.Failure<ExportFileDto>(auditoria.Error);
        }

        try
        {
            await auditoriaRepository.RegistrarAsync(auditoria.Value, cancellationToken);
        }
        catch (Exception)
        {
            // Decisión adoptada: el registro es obligatorio (CA3). Si la bitácora no admite
            // el evento, la exportación se bloquea y no se entrega ningún archivo.
            return Result.Failure<ExportFileDto>(new Error(
                "exportar.bitacora.error",
                "No fue posible registrar la exportación en la bitácora.",
                ErrorType.Dependency));
        }

        return Result.Success(archivo);
    }

    private static string ConstruirCsvRanking(IReadOnlyList<RankingItemDto> items)
    {
        var csv = new StringBuilder();
        csv.AppendLine(CsvExportador.Fila(
            "posicion", "cambioPosicion", "entidadId", "entidad", "sector", "ic", "variacionPct"));

        foreach (var item in items)
        {
            csv.AppendLine(CsvExportador.Fila(
                CsvExportador.Numero(item.Posicion),
                CsvExportador.Numero(item.CambioPosicion),
                item.EntidadId.ToString(),
                item.Entidad,
                item.Sector,
                CsvExportador.Numero(item.Ic),
                CsvExportador.Numero(item.VariacionPct)));
        }

        return csv.ToString();
    }

    private static string ConstruirCsvSerie(IReadOnlyList<OhlcPuntoDto> puntos)
    {
        var csv = new StringBuilder();
        csv.AppendLine(CsvExportador.Fila(
            "fecha", "apertura", "maximo", "minimo", "cierre", "variacion", "variacionPct"));

        foreach (var punto in puntos)
        {
            csv.AppendLine(CsvExportador.Fila(
                punto.Fecha.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                CsvExportador.Numero(punto.Apertura),
                CsvExportador.Numero(punto.Maximo),
                CsvExportador.Numero(punto.Minimo),
                CsvExportador.Numero(punto.Cierre),
                CsvExportador.Numero(punto.Variacion),
                CsvExportador.Numero(punto.VariacionPct)));
        }

        return csv.ToString();
    }

    private static string ConstruirFiltrosRanking(ExportarQuery request)
    {
        var partes = new List<string>(7);

        if (!string.IsNullOrWhiteSpace(request.Sector))
        {
            partes.Add($"sector={request.Sector.Trim()}");
        }

        if (!string.IsNullOrWhiteSpace(request.Dimension))
        {
            partes.Add($"dimension={request.Dimension.Trim()}");
        }

        if (!string.IsNullOrWhiteSpace(request.Periodo))
        {
            partes.Add($"periodo={request.Periodo.Trim()}");
        }

        if (!string.IsNullOrWhiteSpace(request.Orden))
        {
            partes.Add($"orden={request.Orden.Trim()}");
        }

        if (!string.IsNullOrWhiteSpace(request.Dir))
        {
            partes.Add($"dir={request.Dir.Trim()}");
        }

        if (request.Limit is not null)
        {
            partes.Add($"limit={request.Limit}");
        }

        if (request.Offset is not null)
        {
            partes.Add($"offset={request.Offset}");
        }

        return string.Join(';', partes);
    }

    /// <summary>
    /// Detalle con la convención key=value del backend Python (backendFastApi):
    /// recurso=, ip= y estado= son filtrables desde GET /api/v1/auditoria.
    /// Máximo 250 caracteres: si no entra, se recorta el final de los filtros.
    /// </summary>
    private static string ConstruirDetalle(string? ip, string tipo, int filas, string filtros)
    {
        var ipSegura = string.IsNullOrWhiteSpace(ip) ? "desconocida" : ip.Trim();
        if (ipSegura.Length > 45)
        {
            ipSegura = ipSegura[..45];
        }

        var fijo = $"recurso={RutaExportar}; tipo={tipo}; formato=csv; filas={filas}; ip={ipSegura}; estado=200";
        const string prefijoFiltros = "; filtros=";

        if (fijo.Length + prefijoFiltros.Length + filtros.Length <= LimiteDetalle)
        {
            return $"{fijo}{prefijoFiltros}{filtros}";
        }

        var disponible = LimiteDetalle - fijo.Length - prefijoFiltros.Length;
        if (disponible <= 0)
        {
            return fijo.Length <= LimiteDetalle ? fijo : fijo[..LimiteDetalle];
        }

        return $"{fijo}{prefijoFiltros}{filtros[..Math.Min(filtros.Length, disponible)]}";
    }

    private static bool TryNormalizarTipo(string? valor, out string tipo)
    {
        tipo = valor?.Trim().ToLowerInvariant() ?? string.Empty;

        if (tipo == TipoRanking)
        {
            return true;
        }

        if (tipo is TipoSerie or "series")
        {
            tipo = TipoSerie;
            return true;
        }

        return false;
    }

    private static bool TryNormalizarFormato(string? valor, out string formato)
    {
        formato = string.IsNullOrWhiteSpace(valor) ? FormatoCsv : valor.Trim().ToLowerInvariant();
        return formato == FormatoCsv;
    }

    private static string MarcaTemporal() =>
        DateTime.UtcNow.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);

    private static string SanitizarNombre(string valor)
    {
        var limpio = new string(valor
            .Select(c => char.IsAsciiLetterOrDigit(c) ? char.ToLowerInvariant(c) : '-')
            .ToArray());

        while (limpio.Contains("--", StringComparison.Ordinal))
        {
            limpio = limpio.Replace("--", "-", StringComparison.Ordinal);
        }

        limpio = limpio.Trim('-');
        return limpio.Length == 0 ? "serie" : limpio;
    }
}
