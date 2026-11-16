using MediatR;
using SIFTC.Domain.Common;

namespace SIFTC.Application.UseCases.Diccionario.Queries.BuscarDiccionario;

/// <summary>
/// GET /api/v1/diccionario?buscar=&amp;tipo= (HU-DD-08). Busca por nombre,
/// código o sinónimo sin distinguir mayúsculas; tipo es opcional; máximo 20.
/// </summary>
public sealed record BuscarDiccionarioQuery(
    string? Buscar,
    string? Tipo,
    string? Rol) : IRequest<Result<IReadOnlyList<DiccionarioResultadoDto>>>;
