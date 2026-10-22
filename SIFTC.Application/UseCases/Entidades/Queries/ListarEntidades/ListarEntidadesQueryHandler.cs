using MediatR;
using SIFTC.Application.Common;
using SIFTC.Application.Common.Interfaces.Persistence;
using SIFTC.Domain.Common;

namespace SIFTC.Application.UseCases.Entidades.Queries.ListarEntidades;

public class ListarEntidadesQueryHandler(IEntidadRepository entidadRepository)
    : IRequestHandler<ListarEntidadesQuery, Result<Pagina<EntidadDto>>>
{
    private const int TamanoMaximoPagina = 100;

    public async Task<Result<Pagina<EntidadDto>>> Handle(ListarEntidadesQuery request, CancellationToken cancellationToken)
    {
        var page = request.Page ?? 1;
        var size = request.Size ?? TamanoMaximoPagina;

        // ErrorType.Failure se traduce a 400 (Solicitud incorrecta): son parámetros de consulta inválidos.
        if (page < 1)
        {
            return Result.Failure<Pagina<EntidadDto>>(new Error("entidades.page.invalid", "El parámetro page debe ser mayor o igual a 1.", ErrorType.Failure));
        }

        if (size is < 1 or > TamanoMaximoPagina)
        {
            return Result.Failure<Pagina<EntidadDto>>(new Error("entidades.size.invalid", $"El parámetro size debe estar entre 1 y {TamanoMaximoPagina}.", ErrorType.Failure));
        }

        if (!TryParseOpcion(request.Sort, CampoOrdenEntidad.Codigo, out var sort))
        {
            return Result.Failure<Pagina<EntidadDto>>(new Error("entidades.sort.invalid", "El parámetro sort admite: codigo, nombre, sector, ic o variacionPct.", ErrorType.Failure));
        }

        if (!TryParseOpcion(request.Dir, DireccionOrden.Asc, out var dir))
        {
            return Result.Failure<Pagina<EntidadDto>>(new Error("entidades.dir.invalid", "El parámetro dir admite: asc o desc.", ErrorType.Failure));
        }

        var pagina = await entidadRepository.GetPageAsync(request.Sector?.Trim(), page, size, sort, dir, cancellationToken);

        return Result.Success(new Pagina<EntidadDto>(
            pagina.Items.Select(EntidadDto.FromEntity).ToList(),
            pagina.Total));
    }

    // Solo acepta los nombres del enum (sin distinguir mayúsculas), nunca su valor numérico.
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
