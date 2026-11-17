using SIFTC.Application.UseCases.Notificaciones;
using SIFTC.Domain.Entities;

namespace SIFTC.Application.Common.Interfaces.Persistence;

/// <summary>Acceso a reglas, tokens y cola de notificaciones (HU-AD-10 / HU-MO-05).</summary>
public interface INotificacionRepository
{
    // ---------------- CA2: reglas ----------------

    Task<IReadOnlyList<NotificacionRegla>> ListarReglasAsync(
        Guid idUsuario,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NotificacionRegla>> ListarReglasActivasAsync(
        CancellationToken cancellationToken = default);

    Task<NotificacionRegla?> ObtenerReglaAsync(
        Guid idRegla,
        CancellationToken cancellationToken = default);

    /// <summary>Regla con la misma clave única (usuario, canal, entidad), activa o no.</summary>
    Task<NotificacionRegla?> ObtenerReglaPorClaveAsync(
        Guid idUsuario,
        string canal,
        Guid? idEntidad,
        CancellationToken cancellationToken = default);

    Task CrearReglaAsync(
        NotificacionRegla regla,
        CancellationToken cancellationToken = default);

    Task ActualizarReglaAsync(
        NotificacionRegla regla,
        CancellationToken cancellationToken = default);

    /// <summary>Nombre de la entidad, o null si el id no existe.</summary>
    Task<string?> ObtenerNombreEntidadAsync(
        Guid idEntidad,
        CancellationToken cancellationToken = default);

    // ---------------- Tokens FCM ----------------

    /// <summary>Inserta el token o lo reasigna si ya existía (upsert por token_fcm).</summary>
    Task RegistrarTokenAsync(
        NotificacionToken token,
        CancellationToken cancellationToken = default);

    Task<bool> EliminarTokenAsync(
        Guid idUsuario,
        string tokenFcm,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NotificacionToken>> ObtenerTokensActivosAsync(
        Guid idUsuario,
        CancellationToken cancellationToken = default);

    Task DesactivarTokenAsync(
        string tokenFcm,
        CancellationToken cancellationToken = default);

    Task MarcarTokenUsoAsync(
        Guid idToken,
        CancellationToken cancellationToken = default);

    // ---------------- CA1 / CA3: cola de envíos ----------------

    Task<string?> ObtenerCorreoAsync(
        Guid idUsuario,
        CancellationToken cancellationToken = default);

    Task EncolarAsync(
        IReadOnlyList<NotificacionEnvio> envios,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Pendientes cuyo reintento ya venció (fecha_ultimo_intento nula o anterior
    /// al límite). Devuelve las entidades rastreadas para poder actualizarlas.
    /// </summary>
    Task<IReadOnlyList<NotificacionEnvio>> TomarPendientesAsync(
        int limite,
        DateTimeOffset reintentoVencidoAntesDe,
        CancellationToken cancellationToken = default);

    Task ActualizarEnvioAsync(
        NotificacionEnvio envio,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NotificacionEnvioDto>> ListarEnviosAsync(
        Guid? idUsuario,
        string? estado,
        int limite,
        CancellationToken cancellationToken = default);

    /// <summary>Variaciones de IC de las entidades del lote frente a su dato previo.</summary>
    Task<IReadOnlyList<EventoNotificacionDto>> ObtenerVariacionesLoteAsync(
        Guid idLote,
        CancellationToken cancellationToken = default);
}
