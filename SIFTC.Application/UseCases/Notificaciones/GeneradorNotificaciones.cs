using System.Globalization;
using SIFTC.Application.Common.Interfaces.Persistence;
using SIFTC.Domain.Entities;

namespace SIFTC.Application.UseCases.Notificaciones;

/// <summary>
/// CA2: evalúa las reglas activas contra cada evento y crea las filas de
/// notificaciones_envios (una por canal; 'ambos' crea dos).
/// </summary>
public sealed class GeneradorNotificaciones(INotificacionRepository repositorio)
{
    public async Task<int> EncolarPorEventoAsync(
        IReadOnlyList<EventoNotificacionDto> eventos,
        CancellationToken cancellationToken = default)
    {
        if (eventos.Count == 0)
        {
            return 0;
        }

        var reglas = await repositorio.ListarReglasActivasAsync(cancellationToken);
        if (reglas.Count == 0)
        {
            return 0;
        }

        var envios = new List<NotificacionEnvio>();

        foreach (var regla in reglas)
        {
            foreach (var evento in eventos)
            {
                if (!regla.AplicaA(evento.IdEntidad, evento.VariacionPct))
                {
                    continue;
                }

                var asunto = $"Cambio relevante en {evento.NombreEntidad}";
                var cuerpo =
                    $"La entidad {evento.NombreEntidad} presentó una variación de " +
                    $"{evento.VariacionPct.ToString("0.##", CultureInfo.InvariantCulture)} % " +
                    $"frente al dato anterior.";

                foreach (var canal in regla.Canales())
                {
                    var envio = NotificacionEnvio.Crear(
                        regla.IdUsuario,
                        regla.IdRegla,
                        canal,
                        asunto,
                        cuerpo);

                    if (envio.IsSuccess)
                    {
                        envios.Add(envio.Value);
                    }
                }
            }
        }

        if (envios.Count == 0)
        {
            return 0;
        }

        await repositorio.EncolarAsync(envios, cancellationToken);
        return envios.Count;
    }
}
