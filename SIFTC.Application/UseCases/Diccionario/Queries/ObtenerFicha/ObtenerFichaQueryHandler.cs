using MediatR;
using SIFTC.Application.Common.Interfaces.Persistence;
using SIFTC.Domain.Common;

namespace SIFTC.Application.UseCases.Diccionario.Queries.ObtenerFicha;

/// <summary>
/// CA3: ficha completa por código, o 404 si no existe.
/// CA4: si la clasificación no le corresponde al rol, responde EXACTAMENTE el
/// mismo 404 que un código inexistente, para que nadie pueda enumerar los
/// términos Confidenciales o Restringidos.
/// </summary>
public sealed class ObtenerFichaQueryHandler(IDiccionarioRepository repositorio)
    : IRequestHandler<ObtenerFichaQuery, Result<DiccionarioFichaDto>>
{
    public async Task<Result<DiccionarioFichaDto>> Handle(
        ObtenerFichaQuery request,
        CancellationToken cancellationToken)
    {
        // Código vacío: mismo 404 que uno inexistente.
        if (string.IsNullOrWhiteSpace(request.Codigo))
        {
            return NoEncontrado();
        }

        var ficha = await repositorio.ObtenerPorCodigoAsync(request.Codigo, cancellationToken);

        var visibles = ClasificacionesDiccionario.VisiblesPara(request.Rol);
        if (ficha is null || !visibles.Contains(ficha.Clasificacion, StringComparer.Ordinal))
        {
            return NoEncontrado();
        }

        return Result.Success(new DiccionarioFichaDto(
            ficha.Codigo,
            ficha.Tipo,
            ficha.Nombre,
            ficha.Definicion,
            ficha.Formula,
            ficha.Unidad,
            DividirSinonimos(ficha.Sinonimos),
            ficha.Clasificacion));
    }

    /// <summary>La HU pide el sinónimo como arreglo: "a, b" → ["a", "b"].</summary>
    private static IReadOnlyList<string> DividirSinonimos(string? sinonimos) =>
        string.IsNullOrWhiteSpace(sinonimos)
            ? Array.Empty<string>()
            : sinonimos
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static Result<DiccionarioFichaDto> NoEncontrado() =>
        Result.Failure<DiccionarioFichaDto>(new Error(
            "diccionario.no.encontrado",
            "Término no encontrado.",
            ErrorType.NotFound));
}
