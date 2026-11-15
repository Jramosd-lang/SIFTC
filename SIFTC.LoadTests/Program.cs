using System.Diagnostics;
using SIFTC.LoadTests;

// HU-BE-04 CA 3: N clientes conectados al hub reciben una emisión en menos del umbral.
// 1. Abre N conexiones a /hubs/cotizaciones (por lotes para no saturar el arranque).
// 2. Verifica que cada una recibió una sola vez el estado inicial (CA 2).
// 3. Llama a POST /api/v1/tiempo-real/emisiones-prueba y mide, desde ANTES de esa llamada,
//    cuánto tarda cada cliente en recibir el mensaje "prueba". La medida es conservadora:
//    incluye la ida de la solicitud HTTP, así que la latencia real es igual o menor.

OpcionesPrueba opciones;
try
{
    opciones = OpcionesPrueba.Leer(args);
}
catch (ArgumentException exception)
{
    Console.Error.WriteLine(exception.Message);
    Console.Error.WriteLine(OpcionesPrueba.Ayuda);
    return 2;
}

using var cancelacion = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cancelacion.Cancel(); };
var ct = cancelacion.Token;

using var api = new ApiSiftc(opciones.UrlBase);
string token;
try
{
    token = opciones.Token ?? await api.IniciarSesionAsync(opciones.Usuario!, opciones.Password!, ct);
}
catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException)
{
    Console.Error.WriteLine(exception.Message);
    return 2;
}

Console.WriteLine($"Destino: {opciones.UrlBase}  Clientes: {opciones.Clientes}  Umbral: {opciones.UmbralMs} ms");

var clientes = new List<ClienteSimulado>(opciones.Clientes);
var fallosConexion = 0;
var inicioConexion = Stopwatch.StartNew();

try
{
    for (var creados = 0; creados < opciones.Clientes; creados += opciones.LoteConexion)
    {
        var lote = Enumerable.Range(0, Math.Min(opciones.LoteConexion, opciones.Clientes - creados))
            .Select(_ => new ClienteSimulado(opciones.UrlBase, token))
            .ToList();

        var resultados = await Task.WhenAll(lote.Select(async cliente =>
        {
            try
            {
                await cliente.ConectarAsync(ct);
                return true;
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                if (Interlocked.Increment(ref fallosConexion) <= 3)
                {
                    Console.Error.WriteLine($"Fallo de conexión: {exception.Message}");
                }

                await cliente.DisposeAsync();
                return false;
            }
        }));

        clientes.AddRange(lote.Where((_, i) => resultados[i]));
        Console.Write($"\rConectados: {clientes.Count}/{opciones.Clientes}");
    }

    Console.WriteLine($"\nConexión completa en {inicioConexion.Elapsed.TotalSeconds:F1} s. Fallos: {fallosConexion}");

    if (clientes.Count == 0)
    {
        Console.Error.WriteLine("Ningún cliente pudo conectarse: revise la URL, el token y que la API esté arriba.");
        Console.WriteLine("RESULTADO: NO APROBADO");
        return 1;
    }

    // Margen para que lleguen los estados iniciales de los últimos clientes.
    await Task.Delay(TimeSpan.FromSeconds(2), ct);

    var inicioEmision = Stopwatch.GetTimestamp();
    try
    {
        await api.EmitirPruebaAsync(token, ct);
    }
    catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException)
    {
        // 404: falta TiempoReal__EmisionPruebaHabilitada=true. 403: el token no es de un Administrador.
        Console.Error.WriteLine(exception.Message);
        Console.WriteLine("RESULTADO: NO APROBADO");
        return 1;
    }

    var todos = Task.WhenAll(clientes.Select(c => c.PruebaRecibida));
    await Task.WhenAny(todos, Task.Delay(TimeSpan.FromSeconds(opciones.EsperaSegundos), ct));
    // Espera corta extra para detectar mensajes duplicados.
    await Task.Delay(TimeSpan.FromMilliseconds(500), ct);

    return Reporte.Imprimir(opciones, clientes, fallosConexion, inicioEmision);
}
finally
{
    await Task.WhenAll(clientes.Select(c => c.DisposeAsync().AsTask()));
}
