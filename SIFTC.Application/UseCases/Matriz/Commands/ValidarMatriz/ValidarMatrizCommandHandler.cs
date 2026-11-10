using MediatR;
using SIFTC.Application.Common.Interfaces.Integrations;
using SIFTC.Application.Common.Interfaces.Persistence;
using SIFTC.Domain.Common;

namespace SIFTC.Application.UseCases.Matriz.Commands.ValidarMatriz;

public sealed class ValidarMatrizCommandHandler(
    IPythonBackendClient pythonBackendClient,
    IAuditoriaRepository auditoriaRepository)
    : IRequestHandler<ValidarMatrizCommand, Result<ValidacionMatrizResponse>>
{
    public async Task<Result<ValidacionMatrizResponse>> Handle(
        ValidarMatrizCommand request,
        CancellationToken cancellationToken)
    {
        var resultado = await pythonBackendClient.ValidarMatrizAsync(
            request.Archivo,
            request.NombreArchivo,
            request.ContentType,
            request.Authorization,
            cancellationToken);

        // Solo se registra la carga que pasó la validación.
        if (resultado.IsSuccess && resultado.Value.EsValida)
        {
            await auditoriaRepository.RegistrarAsync(
                "matriz.validada",
                request.NombreArchivo,
                request.IdUsuario,
                cancellationToken);
        }

        return resultado;
    }
}
