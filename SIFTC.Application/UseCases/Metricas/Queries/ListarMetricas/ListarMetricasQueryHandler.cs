using System.Globalization;
using MediatR;
using SIFTC.Application.Common;
using SIFTC.Application.Common.Interfaces.Persistence;
using SIFTC.Domain.Common;
using SIFTC.Domain.Enums;

namespace SIFTC.Application.UseCases.Metricas.Queries.ListarMetricas;

public sealed class ListarMetricasQueryHandler(IMetricaQueryRepository metricaQueryRepository)
    : IRequestHandler<ListarMetricasQuery, Result<Pagina<MetricaDto>>>
{
    private const int TamanoMaximoPagina = 100;

    public async Task<Result<Pagina<MetricaDto>>> Handle(ListarMetricasQuery request, CancellationToken cancellationToken)
    {
        var page = request.Page ?? 1;
        var size = request.Size ?? TamanoMaximoPagina;

        if (page < 1)
        {
            return Result.Failure<Pagina<MetricaDto>>(new Error("metricas.page.invalid", "El parámetro page debe ser mayor o igual a 1.", ErrorType.Failure));
        }

        if (size is < 1 or > TamanoMaximoPagina)
        {
            return Result.Failure<Pagina<MetricaDto>>(new Error("metricas.size.invalid", $"El parámetro size debe estar entre 1 y {TamanoMaximoPagina}.", ErrorType.Failure));
        }

        if (!TryParseOpcion(request.Sort, CampoOrdenMetrica.Fecha, out var sort))
        {
            return Result.Failure<Pagina<MetricaDto>>(new Error("metricas.sort.invalid", "El parámetro sort admite: fecha, codigo, nombre, sector, dimension, codigoMetrica, valor o valorNormalizado.", ErrorType.Failure));
        }

        if (!TryParseOpcion(request.Dir, DireccionOrden.Desc, out var dir))
        {
            return Result.Failure<Pagina<MetricaDto>>(new Error("metricas.dir.invalid", "El parámetro dir admite: asc o desc.", ErrorType.Failure));
        }

        Frecuencia? frecuencia = null;
        if (!string.IsNullOrWhiteSpace(request.Periodo))
        {
            frecuencia = request.Periodo.Trim().ToLowerInvariant() switch
            {
                "diario" or "diaria" => Frecuencia.Diaria,
                "mensual" => Frecuencia.Mensual,
                "trimestral" => Frecuencia.Trimestral,
                _ => null
            };

            if (frecuencia is null)
            {
                return Result.Failure<Pagina<MetricaDto>>(new Error("metricas.periodo.invalid", "El parámetro periodo admite: diario, mensual o trimestral.", ErrorType.Failure));
            }
        }

        DateOnly? fecha = null;
        if (!string.IsNullOrWhiteSpace(request.Fecha))
        {
            if (!DateOnly.TryParseExact(request.Fecha.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var fechaCorte))
            {
                return Result.Failure<Pagina<MetricaDto>>(new Error("metricas.fecha.invalid", "El parámetro fecha debe usar el formato ISO 8601 (yyyy-MM-dd).", ErrorType.Failure));
            }

            fecha = fechaCorte;
        }

        var pagina = await metricaQueryRepository.GetPageAsync(
            request.Sector?.Trim(),
            frecuencia,
            fecha,
            request.Dimension?.Trim(),
            page,
            size,
            sort,
            dir,
            cancellationToken);

        return Result.Success(pagina);
    }

    private static bool TryParseOpcion<TEnum>(string? valor, TEnum porDefecto, out TEnum opcion) where TEnum : struct, Enum
    {
        opcion = porDefecto;

        if (string.IsNullOrWhiteSpace(valor))
        {
            return true;
        }

        foreach (var candidato in Enum.GetValues<TEnum>())
        {
            if (string.Equals(candidato.ToString(), valor.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                opcion = candidato;
                return true;
            }
        }

        return false;
    }
}
