using MediatR;
using SIFTC.Application.Common.Interfaces.Persistence;
using SIFTC.Domain.Common;

namespace SIFTC.Application.UseCases.ConfiguracionIc.Commands.CrearVersionIc;

public sealed class CrearVersionIcCommandHandler(
    IConfiguracionIcRepository configuracionIcRepository)
    : IRequestHandler<CrearVersionIcCommand, Result<Guid>>
{
    public Task<Result<Guid>> Handle(
        CrearVersionIcCommand request,
        CancellationToken cancellationToken)
    {
        if (request.IdUsuario == Guid.Empty)
        {
            return Task.FromResult(Result.Failure<Guid>(new Error(
                "configuracionIc.usuario.required",
                "El usuario es obligatorio.",
                ErrorType.Validation)));
        }

        var validacion = ConfiguracionIcValidator.Validar(
            request.Motivo,
            request.Frecuencia,
            request.Pesos,
            request.Umbrales);

        if (validacion.IsFailure)
        {
            return Task.FromResult(Result.Failure<Guid>(validacion.Error));
        }

        return configuracionIcRepository.CrearVersionAsync(
            request.IdUsuario,
            request.Motivo.Trim(),
            request.Frecuencia,
            request.Pesos,
            request.Umbrales ?? [],
            cancellationToken);
    }
}
