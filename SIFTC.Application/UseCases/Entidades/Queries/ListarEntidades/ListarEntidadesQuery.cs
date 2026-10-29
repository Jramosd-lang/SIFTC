using MediatR;
using SIFTC.Application.Common;
using SIFTC.Domain.Common;

namespace SIFTC.Application.UseCases.Entidades.Queries.ListarEntidades;

// Los parámetros llegan tal cual desde la query string; el handler aplica valores por defecto y valida.
public record ListarEntidadesQuery(
    string? Sector,
    int? Page,
    int? Size,
    string? Sort,
    string? Dir
) : IRequest<Result<Pagina<EntidadDto>>>;
