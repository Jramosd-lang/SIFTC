using MediatR;
using SIFTC.Application.Common.Interfaces.Integrations;
using SIFTC.Domain.Common;

namespace SIFTC.Application.UseCases.Ranking.Queries.ObtenerRanking;

public sealed class ObtenerRankingQueryHandler(IPythonBackendClient pythonBackendClient)
    : IRequestHandler<ObtenerRankingQuery, Result<RankingResponseDto>>
{
    private const int LimiteMaximo = 200;
    private const int LimitePorDefecto = 50;
    private const int DesplazamientoPorDefecto = 0;

    private static readonly string[] Dimensiones = ["Rentabilidad", "Liquidez", "Solvencia", "Crecimiento"];
    private static readonly string[] Periodos = ["diario", "mensual", "trimestral"];
    private static readonly string[] Ordenes = ["posicion", "entidad", "sector", "ic", "variacionPct"];
    private static readonly string[] Direcciones = ["asc", "desc"];

    public async Task<Result<RankingResponseDto>> Handle(ObtenerRankingQuery request, CancellationToken cancellationToken)
    {
        string? dimension = null;
        if (!string.IsNullOrWhiteSpace(request.Dimension))
        {
            if (!TryNormalizar(request.Dimension, Dimensiones, Dimensiones[0], out dimension))
            {
                return Result.Failure<RankingResponseDto>(new Error("ranking.dimension.invalid", "El parámetro dimension admite: Rentabilidad, Liquidez, Solvencia o Crecimiento.", ErrorType.Failure));
            }
        }

        if (!TryNormalizar(request.Periodo, Periodos, "mensual", out var periodo))
        {
            return Result.Failure<RankingResponseDto>(new Error("ranking.periodo.invalid", "El parámetro periodo admite: diario, mensual o trimestral.", ErrorType.Failure));
        }

        if (!TryNormalizar(request.Orden, Ordenes, "ic", out var orden))
        {
            return Result.Failure<RankingResponseDto>(new Error("ranking.orden.invalid", "El parámetro orden admite: posicion, entidad, sector, ic o variacionPct.", ErrorType.Failure));
        }

        if (!TryNormalizar(request.Dir, Direcciones, "desc", out var dir))
        {
            return Result.Failure<RankingResponseDto>(new Error("ranking.dir.invalid", "El parámetro dir admite: asc o desc.", ErrorType.Failure));
        }

        var limit = request.Limit ?? LimitePorDefecto;
        if (limit is < 1 or > LimiteMaximo)
        {
            return Result.Failure<RankingResponseDto>(new Error("ranking.limit.invalid", $"El parámetro limit debe estar entre 1 y {LimiteMaximo}.", ErrorType.Failure));
        }

        var offset = request.Offset ?? DesplazamientoPorDefecto;
        if (offset < 0)
        {
            return Result.Failure<RankingResponseDto>(new Error("ranking.offset.invalid", "El parámetro offset debe ser mayor o igual a 0.", ErrorType.Failure));
        }

        var ranking = await pythonBackendClient.GetRankingAsync(
            new RankingQuery(
                request.Sector?.Trim(),
                dimension,
                periodo,
                orden,
                dir,
                limit,
                offset),
            request.Authorization,
            cancellationToken);

        return ranking.IsSuccess
            ? ranking
            : Result.Failure<RankingResponseDto>(ranking.Error);
    }

    private static bool TryNormalizar(string? valor, string[] permitidos, string porDefecto, out string resultado)
    {
        resultado = porDefecto;

        if (string.IsNullOrWhiteSpace(valor))
        {
            return true;
        }

        foreach (var candidato in permitidos)
        {
            if (string.Equals(candidato, valor.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                resultado = candidato;
                return true;
            }
        }

        return false;
    }
}
