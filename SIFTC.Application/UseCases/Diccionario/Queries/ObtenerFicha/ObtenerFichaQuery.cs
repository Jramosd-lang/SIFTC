using MediatR;
using SIFTC.Domain.Common;

namespace SIFTC.Application.UseCases.Diccionario.Queries.ObtenerFicha;

/// <summary>
/// GET /api/v1/diccionario/{codigo} (HU-DD-08). Devuelve la ficha completa;
/// 404 si no existe o si el rol no puede verla (mismo 404 en ambos casos).
/// </summary>
public sealed record ObtenerFichaQuery(
    string? Codigo,
    string? Rol) : IRequest<Result<DiccionarioFichaDto>>;
