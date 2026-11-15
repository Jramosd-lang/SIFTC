using SIFTC.Application.Common.Interfaces.Persistence;
using SIFTC.Application.UseCases.Entidades;

namespace SIFTC.Application.UseCases.TiempoReal;

// Arma el estado completo del ticker; lo comparten la consulta del hub, la difusión y la emisión de prueba.
public sealed class EstadoCotizacionesLector(
    IPublicacionRepository publicacionRepository,
    IEntidadRepository entidadRepository,
    TimeProvider timeProvider)
{
    public async Task<ActualizacionCotizacionesDto> LeerAsync(
        string motivo,
        CancellationToken cancellationToken = default)
    {
        // La secuencia se lee antes que las filas: si se publica un lote entre ambas lecturas,
        // el cliente recibe después una difusión con secuencia mayor y no pierde el cambio.
        var ultimaPublicacion = await publicacionRepository.ObtenerFechaUltimaPublicacionAsync(cancellationToken);
        var entidades = await entidadRepository.ListarTodasAsync(cancellationToken);

        return new ActualizacionCotizacionesDto(
            ultimaPublicacion?.ToUnixTimeMilliseconds() ?? 0,
            motivo,
            timeProvider.GetUtcNow(),
            entidades.Select(EntidadDto.FromEntity).ToList());
    }
}
