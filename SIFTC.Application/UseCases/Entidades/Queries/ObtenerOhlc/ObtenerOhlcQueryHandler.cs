using MediatR;
using SIFTC.Application.Common.Interfaces.Persistence;
using SIFTC.Domain.Common;
using SIFTC.Domain.Entities;
using SIFTC.Domain.Enums;

namespace SIFTC.Application.UseCases.Entidades.Queries.ObtenerOhlc;

public sealed class ObtenerOhlcQueryHandler(
    IEntidadRepository entidadRepository,
    ICotizacionQueryRepository cotizacionRepository)
    : IRequestHandler<ObtenerOhlcQuery, Result<OhlcResponseDto>>
{
    public async Task<Result<OhlcResponseDto>> Handle(ObtenerOhlcQuery request, CancellationToken cancellationToken)
    {
        if (!TryParseFrecuencia(request.Periodo, out var frecuencia))
        {
            return Result.Failure<OhlcResponseDto>(new Error(
                "ohlc.periodo.invalid",
                "El parámetro periodo admite: Diaria, Mensual o Trimestral.",
                ErrorType.Failure));
        }

        var entidad = await entidadRepository.GetByIdAsync(request.IdEntidad, cancellationToken);
        if (entidad is null)
        {
            return Result.Failure<OhlcResponseDto>(new Error(
                "entidad.not_found",
                "La entidad solicitada no existe.",
                ErrorType.NotFound));
        }

        var cotizaciones = await cotizacionRepository.GetByEntidadAsync(request.IdEntidad, cancellationToken);
        var serie = frecuencia == Frecuencia.Diaria
            ? ConstruirDiaria(cotizaciones)
            : ConstruirAgrupada(cotizaciones, frecuencia);

        return Result.Success(new OhlcResponseDto(
            entidad.IdEntidad,
            entidad.Nombre,
            frecuencia.ToString(),
            serie));
    }

    private static bool TryParseFrecuencia(string? valor, out Frecuencia frecuencia)
    {
        frecuencia = Frecuencia.Diaria;

        if (string.IsNullOrWhiteSpace(valor))
        {
            return true;
        }

        foreach (var candidato in Enum.GetValues<Frecuencia>())
        {
            if (string.Equals(candidato.ToString(), valor.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                frecuencia = candidato;
                return true;
            }
        }

        return false;
    }

    private static List<OhlcPuntoDto> ConstruirDiaria(IReadOnlyList<Cotizacion> cotizaciones)
    {
        var serie = new List<OhlcPuntoDto>(cotizaciones.Count);
        decimal? cierreAnterior = null;

        foreach (var cotizacion in cotizaciones.OrderBy(c => c.Fecha))
        {
            var variacion = cierreAnterior is null ? (decimal?)null : cotizacion.Cierre - cierreAnterior.Value;

            serie.Add(new OhlcPuntoDto(
                cotizacion.Fecha,
                cotizacion.Apertura,
                cotizacion.Maximo,
                cotizacion.Minimo,
                cotizacion.Cierre,
                variacion,
                cotizacion.VariacionPct));

            cierreAnterior = cotizacion.Cierre;
        }

        return serie;
    }

    private static List<OhlcPuntoDto> ConstruirAgrupada(IReadOnlyList<Cotizacion> cotizaciones, Frecuencia frecuencia)
    {
        var grupos = cotizaciones
            .GroupBy(c => frecuencia == Frecuencia.Mensual
                ? (Anio: c.Fecha.Year, Periodo: c.Fecha.Month)
                : (Anio: c.Fecha.Year, Periodo: ((c.Fecha.Month - 1) / 3) + 1))
            .OrderBy(g => g.Key.Anio)
            .ThenBy(g => g.Key.Periodo);

        var serie = new List<OhlcPuntoDto>();
        decimal? cierreAnterior = null;

        foreach (var grupo in grupos)
        {
            var ordenadas = grupo.OrderBy(c => c.Fecha).ToList();
            var primera = ordenadas[0];
            var ultima = ordenadas[^1];
            var variacion = cierreAnterior is null ? (decimal?)null : ultima.Cierre - cierreAnterior.Value;
            var variacionPct = cierreAnterior is null || cierreAnterior.Value == 0
                ? (decimal?)null
                : Math.Round((ultima.Cierre - cierreAnterior.Value) / cierreAnterior.Value * 100m, 4);

            serie.Add(new OhlcPuntoDto(
                ultima.Fecha,
                primera.Apertura,
                ordenadas.Max(c => c.Maximo),
                ordenadas.Min(c => c.Minimo),
                ultima.Cierre,
                variacion,
                variacionPct));

            cierreAnterior = ultima.Cierre;
        }

        return serie;
    }
}
