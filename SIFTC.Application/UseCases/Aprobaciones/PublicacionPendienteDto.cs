namespace SIFTC.Application.UseCases.Aprobaciones;

/// <summary>Lote pendiente de aprobación visible en la bandeja.</summary>
/// <param name="IdLote">Identificador UUID del lote.</param>
/// <param name="NombreArchivo">Nombre del CSV cargado.</param>
/// <param name="FechaCarga">Fecha en que se registró el lote.</param>
/// <param name="RegistrosTotales">Filas leídas del CSV.</param>
/// <param name="RegistrosValidos">Filas procesadas por el ETL.</param>
/// <param name="EstadoCarga">Exitoso o Fallido.</param>
/// <param name="Estado">pendiente, publicado o rechazado.</param>
/// <param name="IdUsuarioEjecutor">Usuario que ejecutó la carga.</param>
/// <param name="IdUsuarioDecision">Usuario que aprobó o rechazó; null si está pendiente.</param>
/// <param name="FechaDecision">Fecha de la decisión; null si está pendiente.</param>
/// <param name="Comentario">Comentario de rechazo, si existe.</param>
public sealed record PublicacionPendienteDto(
    Guid IdLote,
    string NombreArchivo,
    DateTimeOffset FechaCarga,
    int RegistrosTotales,
    int RegistrosValidos,
    string EstadoCarga,
    string Estado,
    Guid IdUsuarioEjecutor,
    Guid? IdUsuarioDecision,
    DateTimeOffset? FechaDecision,
    string? Comentario
);
