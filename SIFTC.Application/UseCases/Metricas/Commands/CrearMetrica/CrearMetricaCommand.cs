using MediatR;
using SIFTC.Domain.Common;
using SIFTC.Domain.Enums;
using System.Text.Json.Serialization;

namespace SIFTC.Application.UseCases.Metricas.Commands.CrearMetrica;

public sealed record CrearMetricaCommand(
    string Codigo,
    string Nombre,
    string Formula,
    [property: JsonConverter(typeof(JsonStringEnumConverter))] Frecuencia Frecuencia,
    Guid IdDimension) : IRequest<Result<Guid>>;
