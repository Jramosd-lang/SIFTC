using MediatR;
using SIFTC.Application.UseCases.Notificaciones;
using SIFTC.Domain.Common;

namespace SIFTC.Application.UseCases.Notificaciones.Commands.RegistrarEvento;

/// <summary>
/// CA1/CA2: punto de entrada para un evento relevante (variación de una entidad).
/// El ETL/backend lo invoca tras publicar datos; el generador evalúa las reglas.
/// </summary>
public sealed record RegistrarEventoCommand(
    Guid IdEntidad,
    decimal VariacionPct,
    string? NombreEntidad) : IRequest<Result<RegistrarEventoResponseDto>>;
