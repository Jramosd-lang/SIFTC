using MediatR;
using SIFTC.Domain.Common;

namespace SIFTC.Application.UseCases.Entidades.Queries.ObtenerEntidad;

public record ObtenerEntidadQuery(Guid IdEntidad) : IRequest<Result<EntidadDto>>;
