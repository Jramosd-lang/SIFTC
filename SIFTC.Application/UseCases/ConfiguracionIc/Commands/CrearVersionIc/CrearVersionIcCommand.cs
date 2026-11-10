using MediatR;
using SIFTC.Domain.Common;

namespace SIFTC.Application.UseCases.ConfiguracionIc.Commands.CrearVersionIc;

public sealed record CrearVersionIcCommand(
    Guid IdUsuario,
    string Motivo,
    string Frecuencia,
    IReadOnlyList<PesoIcDto> Pesos,
    IReadOnlyList<UmbralIcDto> Umbrales
) : IRequest<Result<Guid>>;
