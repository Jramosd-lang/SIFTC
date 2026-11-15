using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;

namespace SIFTC.LoadTests;

// Un usuario del tablero conectado al hub. Registra cuándo recibe cada tipo de mensaje.
public sealed class ClienteSimulado : IAsyncDisposable
{
    public const string RutaHub = "hubs/cotizaciones";
    public const string Evento = "cotizaciones";

    private readonly HubConnection _conexion;
    private readonly TaskCompletionSource _pruebaRecibida = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int _estadosIniciales;
    private int _pruebas;

    public ClienteSimulado(Uri urlBase, string token)
    {
        _conexion = new HubConnectionBuilder()
            .WithUrl(new Uri(urlBase, RutaHub), opciones =>
            {
                opciones.AccessTokenProvider = () => Task.FromResult<string?>(token);
                // Igual que se recomienda al frontend: WebSocket directo, sin negotiate.
                opciones.Transports = HttpTransportType.WebSockets;
                opciones.SkipNegotiation = true;
            })
            .Build();

        _conexion.On<MensajeCotizaciones>(Evento, Recibir);
    }

    public int EstadosIniciales => Volatile.Read(ref _estadosIniciales);
    public int Pruebas => Volatile.Read(ref _pruebas);
    public long? RecibidoEnTicks { get; private set; }
    public Task PruebaRecibida => _pruebaRecibida.Task;

    public Task ConectarAsync(CancellationToken cancellationToken) => _conexion.StartAsync(cancellationToken);

    private void Recibir(MensajeCotizaciones mensaje)
    {
        switch (mensaje.Motivo)
        {
            case "estado-inicial":
                Interlocked.Increment(ref _estadosIniciales);
                break;
            case "prueba":
                if (Interlocked.Increment(ref _pruebas) == 1)
                {
                    RecibidoEnTicks = Stopwatch.GetTimestamp();
                    _pruebaRecibida.TrySetResult();
                }
                break;
        }
    }

    public ValueTask DisposeAsync() => _conexion.DisposeAsync();

    private sealed record MensajeCotizaciones(long Secuencia, string Motivo, DateTimeOffset GeneradoEn, JsonElement Entidades);
}
