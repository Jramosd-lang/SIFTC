using MediatR;

namespace SIFTC.Application.UseCases.Aprobaciones.Events;

// Se publica cuando analitica.publicar_lote terminó bien: hay nuevos valores del IC en las tablas analíticas.
public sealed record LotePublicadoEvent(Guid IdLote, Guid IdAprobador) : INotification;
