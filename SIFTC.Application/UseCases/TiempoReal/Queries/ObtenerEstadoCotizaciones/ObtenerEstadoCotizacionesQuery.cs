using MediatR;
using SIFTC.Domain.Common;

namespace SIFTC.Application.UseCases.TiempoReal.Queries.ObtenerEstadoCotizaciones;

// La usa el hub al conectar o reconectar un cliente (CA 2: recibe el último estado).
public sealed record ObtenerEstadoCotizacionesQuery : IRequest<Result<ActualizacionCotizacionesDto>>;
