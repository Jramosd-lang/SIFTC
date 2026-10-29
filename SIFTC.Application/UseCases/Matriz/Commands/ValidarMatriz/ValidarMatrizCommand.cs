using MediatR;
using SIFTC.Domain.Common;

namespace SIFTC.Application.UseCases.Matriz.Commands.ValidarMatriz;

public sealed record ValidarMatrizCommand(
    Stream Archivo,
    string NombreArchivo,
    string ContentType
) : IRequest<Result<ValidacionMatrizResponse>>;
