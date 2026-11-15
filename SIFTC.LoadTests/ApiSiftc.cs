using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace SIFTC.LoadTests;

// Llamadas REST que necesita la prueba: login y disparo de la emisión de prueba.
public sealed class ApiSiftc(Uri urlBase) : IDisposable
{
    private readonly HttpClient _http = new() { BaseAddress = urlBase, Timeout = TimeSpan.FromSeconds(100) };

    public async Task<string> IniciarSesionAsync(string usuario, string password, CancellationToken cancellationToken)
    {
        using var respuesta = await _http.PostAsJsonAsync(
            "api/v1/auth/login",
            new { usuario, password },
            cancellationToken);

        await AsegurarExitoAsync(respuesta, "iniciar sesión", cancellationToken);

        var cuerpo = await respuesta.Content.ReadFromJsonAsync<RespuestaLogin>(cancellationToken);
        return cuerpo?.Token ?? throw new InvalidOperationException("El login no devolvió token.");
    }

    public async Task EmitirPruebaAsync(string token, CancellationToken cancellationToken)
    {
        using var solicitud = new HttpRequestMessage(HttpMethod.Post, "api/v1/tiempo-real/emisiones-prueba");
        solicitud.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var respuesta = await _http.SendAsync(solicitud, cancellationToken);
        await AsegurarExitoAsync(respuesta, "emitir la prueba", cancellationToken);
    }

    private static async Task AsegurarExitoAsync(HttpResponseMessage respuesta, string accion, CancellationToken cancellationToken)
    {
        if (respuesta.IsSuccessStatusCode)
        {
            return;
        }

        var detalle = await respuesta.Content.ReadAsStringAsync(cancellationToken);
        throw new InvalidOperationException($"No se pudo {accion}: HTTP {(int)respuesta.StatusCode}. {detalle}");
    }

    public void Dispose() => _http.Dispose();

    private sealed record RespuestaLogin(string Token);
}
