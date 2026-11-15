using MediatR;
using SIFTC.Domain.Common;

namespace SIFTC.Application.UseCases.TiempoReal.Commands.DifundirCotizaciones;

// Emisión manual para la prueba de carga (CA 3): envía el estado actual a todos los clientes
// con motivo "prueba". No modifica datos ni la secuencia.
public sealed record DifundirCotizacionesCommand : IRequest<Result<ActualizacionCotizacionesDto>>;
