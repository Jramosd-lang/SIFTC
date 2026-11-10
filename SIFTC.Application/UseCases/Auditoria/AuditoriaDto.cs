namespace SIFTC.Application.UseCases.Auditoria;

/// <summary>Evento de la bitácora de auditoría.</summary>
public sealed record AuditoriaDto(
    Guid IdBitacora,
    string Accion,
    string? Detalle,
    DateTimeOffset FechaEvento,
    Guid? IdUsuario);
