using MediatR;
using SIFTC.Domain.Common;
using SIFTC.Domain.Enums;
using System.Text.Json.Serialization;

namespace SIFTC.Application.UseCases.Metricas.Commands.CrearMetrica;

/// <summary>Datos para crear una métrica en el catálogo.</summary>
/// <param name="Codigo">Código único, por ejemplo RNT-01.</param>
/// <param name="Nombre">Nombre visible de la métrica.</param>
/// <param name="Formula">Fórmula de cálculo.</param>
/// <param name="Frecuencia">Diaria, Mensual o Trimestral.</param>
/// <param name="IdDimension">UUID de la dimensión existente.</param>
public sealed record CrearMetricaCommand(
    string Codigo,
    string Nombre,
    string Formula,
    [property: JsonConverter(typeof(JsonStringEnumConverter))] Frecuencia Frecuencia,
    Guid IdDimension) : IRequest<Result<Guid>>;
