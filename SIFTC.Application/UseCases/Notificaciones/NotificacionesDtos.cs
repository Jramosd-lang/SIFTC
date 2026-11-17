namespace SIFTC.Application.UseCases.Notificaciones;

/// <summary>Regla de aviso propia del usuario (CA2).</summary>
public sealed record NotificacionReglaDto(
    Guid IdRegla,
    string Canal,
    Guid? IdEntidad,
    decimal? Umbral,
    bool Activa,
    DateTimeOffset FechaCreacion);

/// <summary>Token FCM registrado (no se devuelve el valor del token).</summary>
public sealed record NotificacionTokenDto(
    Guid IdToken,
    string Plataforma,
    bool Activo,
    DateTimeOffset FechaRegistro);

/// <summary>Fila de la cola/log de envíos (CA1 y CA3).</summary>
public sealed record NotificacionEnvioDto(
    Guid IdEnvio,
    Guid? IdRegla,
    string Canal,
    string Asunto,
    string Estado,
    int Intentos,
    int MaxIntentos,
    string? UltimoError,
    DateTimeOffset FechaCreacion,
    DateTimeOffset? FechaUltimoIntento,
    DateTimeOffset? FechaEnvio);

/// <summary>Evento relevante que dispara la evaluación de reglas (CA1/CA2).</summary>
public sealed record EventoNotificacionDto(
    Guid IdEntidad,
    string NombreEntidad,
    decimal VariacionPct);

/// <summary>Respuesta del ingreso de eventos: cuántos envíos quedaron en cola.</summary>
public sealed record RegistrarEventoResponseDto(int Encolados);
