using MediatR;
using SIFTC.Application.Common.Interfaces.Persistence;
using SIFTC.Domain.Common;

namespace SIFTC.Application.UseCases.Diccionario.Queries.BuscarDiccionario;

/// <summary>
/// CA2: búsqueda ILIKE sobre nombre, código y sinónimos, filtrada por las
/// clasificaciones que el rol puede ver (CA4) y limitada a 20 resultados.
/// </summary>
public sealed class BuscarDiccionarioQueryHandler(IDiccionarioRepository repositorio)
    : IRequestHandler<BuscarDiccionarioQuery, Result<IReadOnlyList<DiccionarioResultadoDto>>>
{
    public const int Limite = 20;

    public async Task<Result<IReadOnlyList<DiccionarioResultadoDto>>> Handle(
        BuscarDiccionarioQuery request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Buscar))
        {
            return Result.Failure<IReadOnlyList<DiccionarioResultadoDto>>(new Error(
                "diccionario.buscar.required",
                "El parámetro buscar es obligatorio.",
                ErrorType.Validation));
        }

        string? tipo = null;
        if (!string.IsNullOrWhiteSpace(request.Tipo))
        {
            tipo = request.Tipo.Trim().ToLowerInvariant();
            if (tipo is not ("dato" or "metrica" or "termino"))
            {
                return Result.Failure<IReadOnlyList<DiccionarioResultadoDto>>(new Error(
                    "diccionario.tipo.invalid",
                    "El parámetro tipo admite: dato, metrica o termino.",
                    ErrorType.Validation));
            }
        }

        var visibles = ClasificacionesDiccionario.VisiblesPara(request.Rol);

        var resultados = await repositorio.BuscarAsync(
            request.Buscar.Trim(),
            tipo,
            visibles,
            Limite,
            cancellationToken);

        return Result.Success<IReadOnlyList<DiccionarioResultadoDto>>(
            resultados
                .Select(d => new DiccionarioResultadoDto(d.Codigo, d.Tipo, d.Nombre, d.Definicion))
                .ToList());
    }
}
