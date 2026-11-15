using SIFTC.Application.UseCases.Entidades;

namespace SIFTC.Application.UseCases.TiempoReal;

/// <summary>Mensaje que el hub de cotizaciones envía a los clientes.</summary>
/// <param name="Secuencia">
/// Versión de los datos: milisegundos Unix de la última publicación aprobada (0 si no hay ninguna).
/// Solo crece cuando cambian los datos; el cliente descarta los mensajes con una secuencia menor o igual a la última aplicada.
/// </param>
/// <param name="Motivo">estado-inicial, lote-publicado o prueba (ver <see cref="MotivoActualizacion"/>).</param>
/// <param name="GeneradoEn">Momento UTC en que el servidor armó el mensaje.</param>
/// <param name="Entidades">Estado completo del ticker: mismas filas y campos que GET /api/v1/entidades.</param>
public sealed record ActualizacionCotizacionesDto(
    long Secuencia,
    string Motivo,
    DateTimeOffset GeneradoEn,
    IReadOnlyList<EntidadDto> Entidades);
