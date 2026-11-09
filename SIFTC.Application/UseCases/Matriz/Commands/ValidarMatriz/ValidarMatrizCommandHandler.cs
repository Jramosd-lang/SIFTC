using MediatR;
using SIFTC.Application.Common.Interfaces.Integrations;
using SIFTC.Domain.Common;

namespace SIFTC.Application.UseCases.Matriz.Commands.ValidarMatriz;

public sealed class ValidarMatrizCommandHandler(IPythonBackendClient pythonBackendClient)
    : IRequestHandler<ValidarMatrizCommand, Result<ValidacionMatrizResponse>>
{
    public Task<Result<ValidacionMatrizResponse>> Handle(ValidarMatrizCommand request, CancellationToken cancellationToken) =>
        pythonBackendClient.ValidarMatrizAsync(
            request.Archivo,
            request.NombreArchivo,
            request.ContentType,
            request.Authorization,
            cancellationToken);
}
